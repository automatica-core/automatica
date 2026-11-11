using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// Learn mode: profile detection from teach-in telegrams, the automatic teach-in response for bidirectional devices
    /// and the two-way template (status and command case of an A5-20 device in one node).
    /// </summary>
    public class EepLearnModeTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private const string Valve = "0183DB5B";
        private const string Rocker = "FEF1A2B3";
        private const string Switch = "05112233";
        private static readonly byte[] ValveId = { 0x01, 0x83, 0xDB, 0x5B };
        private static readonly byte[] IdBase = { 0xFF, 0x80, 0x00, 0x00 };

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
            /// The radio frames sent: ERP1 data and optional data.
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
            public List<(EnOceanLearnNotification Notification, IList<Guid> Templates)> Notified = new();

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

        /// <param name="twoWayValve">true: the valve is set up with the two-way template</param>
        private async Task<Setup> Create(bool twoWayValve)
        {
            await Dispatcher.ClearValues();
            var setup = new Setup();

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);

            if (twoWayValve)
            {
                var type = CreateNodeInstance(EnOceanRorgA5Data.TwoWayA5_20_01Guid);
                type.Name = "Valve office";
                type.SetProperty("enocean-serialnumber", Valve);
                learned.InverseThis2ParentNodeInstanceNavigation.Add(type);
                setup.Nodes[EnOceanRorgA5Data.TwoWayA5_20_01Guid] = type;
                foreach (var dataField in new[]
                         {
                             EnOceanRorgA5Data.DataFieldA5_20_01_IN_LRNB_Guid, EnOceanRorgA5Data.DataFieldA5_20_01_IN_CV_Guid,
                             EnOceanRorgA5Data.DataFieldA5_20_01_IN_TMP_Guid, EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SP_Guid,
                             EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SPS_Guid, EnOceanRorgA5Data.DataFieldA5_20_01_OUT_TMP_Guid
                         })
                {
                    var node = CreateNodeInstance(dataField);
                    type.InverseThis2ParentNodeInstanceNavigation.Add(node);
                    setup.Nodes[dataField] = node;
                }
            }

            setup.Driver = await CreateDriver<EnOceanDriver>(driverNode);
            setup.Stick = new StickStream();
            var transport = new P3.Driver.EnOcean.Driver(setup.Stick);
            Assert.True(await transport.Open());
            setup.Driver.SetTransport(transport);
            setup.Driver.LearnNotified += (notification, templates) => setup.Notified.Add((notification, templates));
            return setup;
        }

        private static RadioErp1Packet FromValve(string userData) =>
            EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString(userData), Valve);

        // --- teach-in telegrams ---------------------------------------------------------------------------------

        [Fact]
        public void FourBsTeachInWithEep_TellsProfileAndManufacturer()
        {
            // what the valve sends: A5-20-01, manufacturer 0x00A, DB0 0x80 = with EEP, query
            var teachIn = TeachIn.Parse(FromValve("80080A80"));

            Assert.Equal(TeachInKind.FourBsWithEep, teachIn.Kind);
            Assert.Equal("A5-20-01", teachIn.Eep);
            Assert.Equal(0x00A, teachIn.Manufacturer);
            Assert.True(teachIn.ResponseExpected);
        }

        [Fact]
        public void FourBsTeachInOfASensor_ExpectsNoResponse()
        {
            // A5-02-05 temperature sensor: FUNC 0x02, TYPE 0x05
            var teachIn = TeachIn.Parse(EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString("08280080"), Switch));

            Assert.Equal("A5-02-05", teachIn.Eep);
            Assert.False(teachIn.ResponseExpected);
        }

        [Fact]
        public void FourBsTeachInWithoutEep_HasNoProfile()
        {
            var teachIn = TeachIn.Parse(EepTelegrams.Radio(Rorg.FourBs, new byte[] { 0, 0, 0, 0 }, Switch));

            Assert.Equal(TeachInKind.FourBs, teachIn.Kind);
            Assert.False(teachIn.HasProfile);
            Assert.Null(teachIn.Eep);
        }

        [Fact]
        public void FourBsDataTelegram_IsNoTeachIn()
        {
            Assert.Null(TeachIn.Parse(FromValve("32008008")));
        }

        [Fact]
        public void UteQuery_TellsTheVldProfile()
        {
            // DB6 0x80 bidirectional, response expected, teach-in or deletion; 1 channel; manufacturer 0x046; D2-01-12
            var teachIn = TeachIn.Parse(EepTelegrams.Radio(Rorg.Ute, Convert.FromHexString("80014600" + "1201D2"), Switch));

            Assert.Equal(TeachInKind.Ute, teachIn.Kind);
            Assert.Equal(Rorg.Vld, teachIn.Rorg);
            Assert.Equal("D2-01-12", teachIn.Eep);
            Assert.Equal(0x046, teachIn.Manufacturer);
            Assert.True(teachIn.ResponseExpected);
            Assert.False(teachIn.IsDeletion);
            Assert.True(RadioErp1Packet.IsTechIn(EepTelegrams.Radio(Rorg.Ute, Convert.FromHexString("800146001201D2"), Switch)));
        }

        [Fact]
        public void UteDeletionAndUteResponse_AreNoTeachInQuery()
        {
            Assert.True(TeachIn.Parse(EepTelegrams.Radio(Rorg.Ute, Convert.FromHexString("A00146001201D2"), Switch)).IsDeletion);
            // command 1 = a response, not a query
            Assert.Null(TeachIn.Parse(EepTelegrams.Radio(Rorg.Ute, Convert.FromHexString("910146001201D2"), Switch)));
        }

        [Fact]
        public void UteResponse_AcceptsTheQueriedProfile()
        {
            Assert.Equal("910146001201D2", Convert.ToHexString(TeachIn.UteResponse(Convert.FromHexString("800146001201D2"))));
        }

        [Fact]
        public void Rssi_ComesFromTheOptionalData()
        {
            // EepTelegrams sends dBm 0x46
            Assert.Equal(-70, FromValve("32008008").Rssi);
        }

        // --- learn mode -----------------------------------------------------------------------------------------

        [Fact]
        public async Task LearnMode_UnknownValve_IsAnsweredAndOfferedAsTwoWayDevice()
        {
            var setup = await Create(false);
            await setup.Driver.EnableLearnMode();

            setup.Driver.LearnTelegramReceived(FromValve("80080A80"));
            Assert.True(await setup.Driver.LastLearnResponse);

            // teach-in response: FUNC 0x20, TYPE 0x01, manufacturer 0x7FF, DB0 0xF0, addressed to the valve
            var frame = Assert.Single(setup.Stick.Radio());
            Assert.Equal((byte)Rorg.FourBs, frame.Data[0]);
            Assert.Equal("800FFFF0", Convert.ToHexString(frame.Data.AsSpan(1, 4)));
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, EnOceanTypeNode.DerivedSenderOffset(Valve) }, frame.Data.AsSpan(5, 4).ToArray());
            Assert.Equal(ValveId, frame.Optional.AsSpan(1, 4).ToArray());

            var (notification, templates) = Assert.Single(setup.Notified);
            Assert.Equal(Valve, notification.Serial);
            Assert.Equal(EnOceanLearnNotification.StatusNew, notification.Status);
            Assert.Equal("A5-20-01", notification.Eep);
            Assert.Equal(-70, notification.Rssi);
            Assert.True(notification.Paired);
            // one template: the two-way device, the two case templates are not offered
            Assert.Equal(new[] { EnOceanRorgA5Data.TwoWayA5_20_01Guid }, templates);
            Assert.Contains(notification.Properties, p => p.This2PropertyTemplateNavigation.Key == "enocean-serialnumber" && (string)p.Value == Valve);
        }

        [Fact]
        public async Task LearnMode_RepeatedTeachIn_IsReportedOnce()
        {
            var setup = await Create(false);
            await setup.Driver.EnableLearnMode();

            setup.Driver.LearnTelegramReceived(EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString("08280080"), Switch));
            setup.Driver.LearnTelegramReceived(EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString("08280080"), Switch));

            var (notification, templates) = Assert.Single(setup.Notified);
            Assert.Equal("A5-02-05", notification.Eep);
            Assert.False(notification.Paired);
            Assert.Equal(new[] { EnOceanRorgA5Data.TypeA5_02_05_5Guid }, templates);
            // a sensor never gets a teach-in response
            Assert.Empty(setup.Stick.Radio());
        }

        [Fact]
        public async Task LearnMode_Rocker_OffersTheRpsProfilesCommonFirst()
        {
            var setup = await Create(false);
            await setup.Driver.EnableLearnMode();

            setup.Driver.LearnTelegramReceived(EepTelegrams.Radio(Rorg.Rps, new byte[] { 0x30 }, Rocker, 0x30));

            var (notification, templates) = Assert.Single(setup.Notified);
            Assert.Null(notification.Eep);
            Assert.Equal("RPS", notification.Telegram);
            Assert.True(templates.Count > 3);
            Assert.Equal("F6-02-01", Eep(templates[0]));
            Assert.All(templates, t => Assert.Equal(0xF6, t.ToByteArray()[10]));
        }

        [Fact]
        public async Task LearnMode_KnownValve_IsReportedAsKnown_AndAnsweredByItsNode()
        {
            var setup = await Create(true);
            await setup.Driver.EnableLearnMode();

            setup.Driver.LearnTelegramReceived(FromValve("80080A80"));
            setup.Driver.TelegramReceived(FromValve("80080A80"));
            Assert.True(await setup.Type(EnOceanRorgA5Data.TwoWayA5_20_01Guid).LastReply);

            var (notification, _) = Assert.Single(setup.Notified);
            Assert.Equal(EnOceanLearnNotification.StatusKnown, notification.Status);
            Assert.Equal("Valve office", notification.Known);
            Assert.False(notification.Paired);
            // only the node answered
            Assert.Single(setup.Stick.Radio());
        }

        [Fact]
        public async Task LearnMode_FirstDataTelegramOfAKnownDevice_IsReportedOnce()
        {
            var setup = await Create(true);
            await setup.Driver.EnableLearnMode();

            setup.Driver.LearnDataReceived(FromValve("32008008"));
            setup.Driver.LearnDataReceived(FromValve("32008008"));

            var (notification, templates) = Assert.Single(setup.Notified);
            Assert.Equal(EnOceanLearnNotification.StatusReceiving, notification.Status);
            Assert.Empty(templates);
        }

        [Fact]
        public async Task LearnModeOff_NothingIsReportedOrAnswered()
        {
            var setup = await Create(false);

            setup.Driver.LearnTelegramReceived(FromValve("80080A80"));
            await setup.Driver.EnableLearnMode();
            await setup.Driver.DisableLearnMode();
            setup.Driver.LearnTelegramReceived(FromValve("80080A80"));

            Assert.Empty(setup.Notified);
            Assert.Empty(setup.Stick.Radio());
        }

        // --- two-way device -------------------------------------------------------------------------------------

        [Fact]
        public async Task TwoWay_DecodesTheStatusFields_NotTheCommandFields()
        {
            var setup = await Create(true);

            // DB3 current value 50 %, DB1 temperature raw 128, DB0 data telegram
            setup.Driver.TelegramReceived(FromValve("32008008"));

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);
            Assert.Equal(50d, Convert.ToDouble(values[setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_IN_CV_Guid].ObjId].Value));
            Assert.True(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_IN_TMP_Guid].ObjId));
            Assert.False(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SP_Guid].ObjId));
            Assert.False(values.ContainsKey(setup.Nodes[EnOceanRorgA5Data.DataFieldA5_20_01_OUT_TMP_Guid].ObjId));
        }

        [Fact]
        public async Task TwoWay_StatusTelegram_IsAnsweredWithTheWrittenSetpoint()
        {
            var setup = await Create(true);

            // the valve sleeps: writing only stores the value
            Assert.True(await setup.Data(EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SP_Guid).SendValue(40, new WriteContextMock()));
            Assert.Empty(setup.Stick.Radio());

            setup.Driver.TelegramReceived(FromValve("32008008"));
            Assert.True(await setup.Type(EnOceanRorgA5Data.TwoWayA5_20_01Guid).LastReply);

            var frame = Assert.Single(setup.Stick.Radio());
            // DB3 valve position 40 %, DB1 SPS = 0, DB0 LRN bit = data telegram, addressed to the valve
            Assert.Equal("28000008", Convert.ToHexString(frame.Data.AsSpan(1, 4)));
            Assert.Equal(ValveId, frame.Optional.AsSpan(1, 4).ToArray());
        }

        [Fact]
        public async Task TwoWay_WithoutWrittenValue_IsNotAnswered()
        {
            var setup = await Create(true);

            setup.Driver.TelegramReceived(FromValve("32008008"));

            Assert.False(await setup.Type(EnOceanRorgA5Data.TwoWayA5_20_01Guid).LastReply);
            Assert.Empty(setup.Stick.Radio());
        }

        [Fact]
        public void TwoWayTemplates_ExistForA520_01To06()
        {
            foreach (var template in new[]
                     {
                         EnOceanRorgA5Data.TwoWayA5_20_01Guid, EnOceanRorgA5Data.TwoWayA5_20_02Guid, EnOceanRorgA5Data.TwoWayA5_20_03Guid,
                         EnOceanRorgA5Data.TwoWayA5_20_04Guid, EnOceanRorgA5Data.TwoWayA5_20_05Guid, EnOceanRorgA5Data.TwoWayA5_20_06Guid
                     })
            {
                var node = CreateNodeInstance(template);
                Assert.NotNull(node);
                Assert.Equal("enocean-type-20", node.This2NodeTemplateNavigation.Key);
                Assert.True(EnOceanEepMetadata.IsTwoWay(template));
                Assert.NotNull(EnOceanEepMetadata.GetOutgoing(template));
            }

            // status values read only, command values writable
            Assert.False(CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_IN_CV_Guid).IsWriteable);
            Assert.True(CreateNodeInstance(EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SP_Guid).IsWriteable);
            Assert.True(EnOceanEepMetadata.IsCommandField(EnOceanRorgA5Data.DataFieldA5_20_01_OUT_SP_Guid));
            Assert.False(EnOceanEepMetadata.IsCommandField(EnOceanRorgA5Data.DataFieldA5_20_01_IN_CV_Guid));
        }

        private static string Eep(Guid template)
        {
            var bytes = template.ToByteArray();
            return $"{bytes[10]:X2}-{bytes[11]:X2}-{bytes[12]:X2}";
        }
    }
}
