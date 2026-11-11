using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// A5-20-01 battery valve actuators: learn mode puts the device directly below "Learned", the valve asks for a
    /// bidirectional 4BS teach-in and expects an answer (controller to actuator) to every status telegram.
    /// </summary>
    public class EepValveReplyTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private const string Valve = "0183DB5B";
        private static readonly byte[] ValveId = { 0x01, 0x83, 0xDB, 0x5B };
        private static readonly byte[] IdBase = { 0xFF, 0x80, 0x00, 0x00 };

        /// <summary>
        /// A USB stick that answers CO_RD_IDBASE with <see cref="IdBase"/> and records every frame written.
        /// </summary>
        private sealed class StickStream : BaseStream
        {
            public List<byte[]> Written { get; } = new();

            public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

            public override Task<bool> Open() => Task.FromResult(true);

            public override Task<bool> Close() => Task.FromResult(true);

            public override Task<EnOceanPacket> WriteFrame(EnOceanPacket frame)
            {
                Written.Add(frame.RawData.ToArray());
                var data = frame.RawData.Span[4] == (byte)EnOcean.PacketType.CommonCommand
                    ? new byte[] { 0x00 }.Concat(IdBase).ToArray()
                    : new byte[] { 0x00 };
                var header = new byte[] { 0x55, 0x00, (byte)data.Length, 0x00, (byte)EnOcean.PacketType.Response };
                var answer = EnOceanPacket.CreateNew(header, data, Span<byte>.Empty);
                return Task.FromResult(EnOceanPacket.Parse(answer.RawData.ToArray()));
            }

            public override void Pause()
            {
            }

            public override void Continue()
            {
            }

            /// <summary>
            /// The radio frames sent: ERP1 data (RORG, user data, sender id, status) and optional data
            /// (sub telegrams, destination id, dBm, security level).
            /// </summary>
            public List<(byte[] Data, byte[] Optional)> Radio() => Written
                .Where(f => f[4] == (byte)EnOcean.PacketType.RadioErp1)
                .Select(f =>
                {
                    var length = (f[1] << 8) | f[2];
                    return (f.AsSpan(6, length).ToArray(), f.AsSpan(6 + length, f[3]).ToArray());
                })
                .ToList();
        }

        private sealed class WriteContextMock : IWriteContext
        {
            public Task DispatchValue(object value, CancellationToken token = default) => Task.CompletedTask;
        }

        private sealed class Setup
        {
            public EnOceanDriver Driver;
            public StickStream Stick;
            public Dictionary<Guid, NodeInstance> Nodes = new();

            public IEnumerable<IDriverNode> All() => All(Driver);

            public EnOceanTypeNode Type(Guid typeTemplate) =>
                All().OfType<EnOceanTypeNode>().Single(n => n.Id == Nodes[typeTemplate].ObjId);

            public EnOceanDataNode Data(Guid dataField) =>
                All().OfType<EnOceanDataNode>().Single(n => n.Id == Nodes[dataField].ObjId);

            private static IEnumerable<IDriverNode> All(IDriverNode node)
            {
                yield return node;
                foreach (var child in ((DriverBase)node).Children.SelectMany(All))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Learned / status case (and optionally the command case), both with the serial number of the valve, the
        /// way learn mode creates them: no RORG and function nodes in between.
        /// </summary>
        private async Task<Setup> Create(bool withCommandNode)
        {
            await Dispatcher.ClearValues();
            var setup = new Setup();

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);

            void AddType(Guid typeTemplate, params Guid[] dataFields)
            {
                var type = CreateNodeInstance(typeTemplate);
                type.SetProperty("enocean-serialnumber", Valve);
                learned.InverseThis2ParentNodeInstanceNavigation.Add(type);
                setup.Nodes[typeTemplate] = type;
                foreach (var dataField in dataFields)
                {
                    var node = CreateNodeInstance(dataField);
                    type.InverseThis2ParentNodeInstanceNavigation.Add(node);
                    setup.Nodes[dataField] = node;
                }
            }

            AddType(EnOceanRorgA5Data.TypeA5_20_01_1Guid, EnOceanRorgA5Data.DataFieldA5_20_01_1_LRNB_Guid,
                EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid, EnOceanRorgA5Data.DataFieldA5_20_01_1_TMP_Guid);
            if (withCommandNode)
            {
                AddType(EnOceanRorgA5Data.TypeA5_20_01_2Guid, EnOceanRorgA5Data.DataFieldA5_20_01_2_SP_Guid,
                    EnOceanRorgA5Data.DataFieldA5_20_01_2_SPS_Guid);
            }

            setup.Driver = await CreateDriver<EnOceanDriver>(driverNode);
            setup.Stick = new StickStream();
            var transport = new P3.Driver.EnOcean.Driver(setup.Stick);
            Assert.True(await transport.Open());
            setup.Driver.SetTransport(transport);
            return setup;
        }

        private static RadioErp1Packet FromValve(string userData) =>
            EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString(userData), Valve);

        /// <summary>
        /// Checks RORG, sender id (id base + offset of the valve), status and destination (the valve), returns the user data.
        /// </summary>
        private static string Addressed((byte[] Data, byte[] Optional) frame)
        {
            var erp1 = frame.Data;
            Assert.Equal((byte)Rorg.FourBs, erp1[0]);
            var offset = EnOceanTypeNode.DerivedSenderOffset(Valve);
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, offset }, erp1.AsSpan(erp1.Length - 5, 4).ToArray());
            Assert.Equal(0x00, erp1[^1]);
            Assert.Equal(ValveId, frame.Optional.AsSpan(1, 4).ToArray());
            return Convert.ToHexString(erp1.AsSpan(1, erp1.Length - 6));
        }

        [Fact]
        public async Task DeviceDirectlyBelowLearned_DecodesStatusTelegram()
        {
            var setup = await Create(false);

            // DB3 current value 50 %, DB2 flags, DB1 temperature raw 128, DB0 LRN bit = data telegram
            setup.Driver.TelegramReceived(FromValve("32008008"));

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);
            Assert.Equal(50d, Convert.ToDouble(values[setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid].ObjId].Value));
            Assert.True(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_1_TMP_Guid].ObjId));
        }

        [Fact]
        public async Task TeachInQuery_GetsAddressedResponse()
        {
            var setup = await Create(false);

            // what the LOX-FSA sent: 4BS teach-in with EEP A5-20-01, manufacturer 0x00A, DB0 0x80 = query
            setup.Driver.TelegramReceived(FromValve("80080A80"));
            Assert.True(await setup.Type(EnOceanRorgA5Data.TypeA5_20_01_1Guid).LastReply);

            var frame = Assert.Single(setup.Stick.Radio());
            // FUNC 0x20, TYPE 0x01, manufacturer 0x7FF, DB0 0xF0 = with EEP, EEP supported, successful, response
            Assert.Equal("800FFFF0", Addressed(frame));
        }

        [Fact]
        public async Task TeachInOfASensor_IsNotAnswered()
        {
            Assert.False(EnOceanTypeNode.IsBidirectionalTeachInQuery(EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString("10080A80"), Valve)));
            // a response (DB0.4 = 1) is never answered
            Assert.False(EnOceanTypeNode.IsBidirectionalTeachInQuery(FromValve("80080AF0")));
            Assert.True(EnOceanTypeNode.IsBidirectionalTeachInQuery(FromValve("80080A80")));
            await Task.CompletedTask;
        }

        [Fact]
        public async Task StatusTelegram_IsAnsweredWithTheWrittenSetpoint()
        {
            var setup = await Create(true);

            // the valve sleeps: writing only stores the value
            Assert.True(await setup.Data(EnOceanRorgA5Data.DataFieldA5_20_01_2_SP_Guid).SendValue(40, new WriteContextMock()));
            Assert.Empty(setup.Stick.Radio());

            setup.Driver.TelegramReceived(FromValve("32008008"));
            Assert.True(await setup.Type(EnOceanRorgA5Data.TypeA5_20_01_1Guid).LastReply);

            var frame = Assert.Single(setup.Stick.Radio());
            // DB3 valve position 40 %, DB1 SPS = 0 (valve position), DB0 LRN bit = data telegram
            Assert.Equal("28000008", Addressed(frame));

            // the next status telegram gets the same answer
            setup.Driver.TelegramReceived(FromValve("28008008"));
            Assert.True(await setup.Type(EnOceanRorgA5Data.TypeA5_20_01_1Guid).LastReply);
            Assert.Equal("28000008", Addressed(setup.Stick.Radio().Last()));
        }

        [Fact]
        public async Task StatusTelegram_WithoutWrittenValue_IsNotAnswered()
        {
            var setup = await Create(true);

            setup.Driver.TelegramReceived(FromValve("32008008"));

            Assert.False(await setup.Type(EnOceanRorgA5Data.TypeA5_20_01_1Guid).LastReply);
            Assert.Empty(setup.Stick.Radio());
        }

        [Fact]
        public async Task StatusTelegram_WithoutCommandNode_IsNotAnswered()
        {
            var setup = await Create(false);

            setup.Driver.TelegramReceived(FromValve("32008008"));

            Assert.False(await setup.Type(EnOceanRorgA5Data.TypeA5_20_01_1Guid).LastReply);
            Assert.Empty(setup.Stick.Radio());
        }

        [Fact]
        public async Task CommandNode_IgnoresTelegramsOfTheValve()
        {
            var setup = await Create(true);

            setup.Driver.TelegramReceived(FromValve("32008008"));

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);
            Assert.False(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_2_SP_Guid].ObjId));
            Assert.True(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_1_CV_Guid].ObjId));
        }
    }
}
