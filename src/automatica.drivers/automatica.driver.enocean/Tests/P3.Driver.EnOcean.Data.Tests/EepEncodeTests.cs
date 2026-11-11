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
    /// Actuator commands (D2-01 actuator set output, D2-05 go to position / stop, A5-38-08 central command): the
    /// telegram bytes follow the EEP 2.6.x specification and the driver decodes them back to the written values.
    /// </summary>
    public class EepEncodeTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private const string Sender = "0581A2B3";
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
                // RESPONSE RET_OK, with the id base for CO_RD_IDBASE
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
            /// ERP1 data (RORG, user data, sender id, status) of the radio telegrams sent.
            /// </summary>
            public List<byte[]> Radio() => Written
                .Where(f => f[4] == (byte)EnOcean.PacketType.RadioErp1)
                .Select(f => f.AsSpan(6, (f[1] << 8) | f[2]).ToArray())
                .ToList();
        }

        private sealed class WriteContextMock : IWriteContext
        {
            public List<object> Dispatched { get; } = new();

            public Task DispatchValue(object value, CancellationToken token = default)
            {
                Dispatched.Add(value);
                return Task.CompletedTask;
            }
        }

        private sealed class Actuator
        {
            public EnOceanDriver Driver;
            public StickStream Stick;
            public Dictionary<Guid, NodeInstance> Nodes;

            public EnOceanDataNode Node(Guid dataField) => All(Driver).OfType<EnOceanDataNode>().Single(n => n.Id == Nodes[dataField].ObjId);

            private static IEnumerable<IDriverNode> All(IDriverNode node)
            {
                yield return node;
                foreach (var child in ((DriverBase)node).Children.SelectMany(All))
                {
                    yield return child;
                }
            }
        }

        private async Task<Actuator> Create(Rorg rorg, Guid typeGuid, int? senderOffset, params Guid[] dataFields)
        {
            await Dispatcher.ClearValues();

            var r = ((byte)rorg).ToString("X2");
            var rorgClass = typeof(EnOceanDriverFactory).Assembly.GetType($"P3.Driver.EnOcean.DriverFactory.EnOceanRorg{r}Data")!;
            var func = EnOceanEepMetadata.GetOutgoing(typeGuid)?.Func ?? (byte)0;
            var rorgGuid = (Guid)rorgClass.GetField($"Rorg{r}Guid")!.GetValue(null)!;
            var funcGuid = (Guid)rorgClass.GetField($"Function{r}_{func:X2}Guid")!.GetValue(null)!;

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);
            var rorgNode = CreateNodeInstance(rorgGuid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorgNode);
            var funcNode = CreateNodeInstance(funcGuid);
            rorgNode.InverseThis2ParentNodeInstanceNavigation.Add(funcNode);
            var type = CreateNodeInstance(typeGuid);
            type.SetProperty("enocean-serialnumber", Sender);
            if (senderOffset.HasValue)
            {
                type.SetProperty("enocean-sender-offset", senderOffset.Value);
            }
            funcNode.InverseThis2ParentNodeInstanceNavigation.Add(type);

            var nodes = new Dictionary<Guid, NodeInstance> { [Guid.Empty] = driverNode };
            foreach (var dataField in dataFields)
            {
                var node = CreateNodeInstance(dataField);
                type.InverseThis2ParentNodeInstanceNavigation.Add(node);
                nodes[dataField] = node;
            }

            var driver = await CreateDriver<EnOceanDriver>(driverNode);
            var stick = new StickStream();
            var transport = new P3.Driver.EnOcean.Driver(stick);
            Assert.True(await transport.Open());
            driver.SetTransport(transport);

            return new Actuator { Driver = driver, Stick = stick, Nodes = nodes };
        }

        private static async Task<byte[]> Write(Actuator actuator, Guid dataField, object value)
        {
            var context = new WriteContextMock();
            var sent = actuator.Stick.Radio().Count;

            Assert.True(await actuator.Node(dataField).SendValue(value, context));

            Assert.Equal(new[] { value }, context.Dispatched);
            var radio = actuator.Stick.Radio();
            Assert.Equal(sent + 1, radio.Count);
            return radio.Last();
        }

        /// <summary>
        /// The user data of a sent ERP1 telegram after checking RORG, sender id (id base + offset) and status.
        /// </summary>
        private static string UserData(byte[] erp1, Rorg rorg, byte senderOffset)
        {
            Assert.Equal((byte)rorg, erp1[0]);
            var sender = erp1.AsSpan(erp1.Length - 5, 4).ToArray();
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, senderOffset }, sender);
            Assert.Equal(0x00, erp1[^1]);
            return Convert.ToHexString(erp1.AsSpan(1, erp1.Length - 6));
        }

        /// <summary>
        /// Feeds <paramref name="data"/> to a device of the same case, as if the actuator sent it, and returns the
        /// decoded values.
        /// </summary>
        private async Task<Dictionary<Guid, object>> Decode(Rorg rorg, Guid typeGuid, string data, params Guid[] dataFields)
        {
            var device = await Create(rorg, typeGuid, null, dataFields);
            await Dispatcher.ClearValues();
            device.Driver.TelegramReceived(EepTelegrams.Radio(rorg, Convert.FromHexString(data), Sender));
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            return dataFields.ToDictionary(f => f, f => values.TryGetValue(device.Nodes[f].ObjId, out var v) ? v.Value : null);
        }

        [Fact]
        public async Task D2_01_00_SetOutput_AllChannelsOn_AndBackOff()
        {
            var type = EnOceanRorgD2Data.TypeD2_01_00_1Guid;
            var fields = new[] { EnOceanRorgD2Data.DataFieldD2_01_00_1_CMD_Guid, EnOceanRorgD2Data.DataFieldD2_01_00_1_DV_Guid,
                EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid };
            var actuator = await Create(Rorg.Vld, type, 5, fields);

            // DB2: CMD 0x1, DB1: dim value 0 (switch to new output value), I/O channel 0x1E (all), DB0: output value 100 %
            var on = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid, 100), Rorg.Vld, 5);
            Assert.Equal("011E64", on);

            // the channel is kept from the write before, output value 0 = off
            var channel = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid, 2), Rorg.Vld, 5);
            Assert.Equal("010264", channel);
            var off = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid, 0), Rorg.Vld, 5);
            Assert.Equal("010200", off);

            var decoded = await Decode(Rorg.Vld, type, on, fields);
            Assert.Equal(1L, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_01_00_1_CMD_Guid]));
            Assert.Equal(0x1EL, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid]));
            Assert.Equal(100d, Convert.ToDouble(decoded[EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid]));

            decoded = await Decode(Rorg.Vld, type, off, fields);
            Assert.Equal(2L, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid]));
            Assert.Equal(0d, Convert.ToDouble(decoded[EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid]));
        }

        [Fact]
        public async Task D2_01_00_CommandConstant_CannotBeOverwritten()
        {
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_01_00_1Guid, 5,
                EnOceanRorgD2Data.DataFieldD2_01_00_1_CMD_Guid, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid);

            var data = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_01_00_1_CMD_Guid, 4), Rorg.Vld, 5);

            Assert.Equal("011E00", data);
        }

        [Fact]
        public async Task D2_05_00_GoToPositionAndAngle_KeepsTheOtherValue()
        {
            var type = EnOceanRorgD2Data.TypeD2_05_00_1Guid;
            var fields = new[] { EnOceanRorgD2Data.DataFieldD2_05_00_1_POS_Guid, EnOceanRorgD2Data.DataFieldD2_05_00_1_ANG_Guid,
                EnOceanRorgD2Data.DataFieldD2_05_00_1_REPO_Guid, EnOceanRorgD2Data.DataFieldD2_05_00_1_LOCK_Guid,
                EnOceanRorgD2Data.DataFieldD2_05_00_1_CHN_Guid, EnOceanRorgD2Data.DataFieldD2_05_00_1_CMD_Guid };
            var actuator = await Create(Rorg.Vld, type, 7, fields);

            // DB3: position 50 %, DB2: angle 127 = do not change, DB1: repositioning 0 / lock 0, DB0: channel 0xF (all) / CMD 0x1
            var position = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_05_00_1_POS_Guid, 50), Rorg.Vld, 7);
            Assert.Equal("327F00F1", position);

            var angle = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_05_00_1_ANG_Guid, 20), Rorg.Vld, 7);
            Assert.Equal("321400F1", angle);

            var decoded = await Decode(Rorg.Vld, type, angle, fields);
            Assert.Equal(50d, Convert.ToDouble(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_1_POS_Guid]));
            Assert.Equal(20d, Convert.ToDouble(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_1_ANG_Guid]));
            Assert.Equal(0xFL, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_1_CHN_Guid]));
            Assert.Equal(1L, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_1_CMD_Guid]));
        }

        [Fact]
        public async Task D2_05_00_Stop()
        {
            var type = EnOceanRorgD2Data.TypeD2_05_00_2Guid;
            var fields = new[] { EnOceanRorgD2Data.DataFieldD2_05_00_2_CHN_Guid, EnOceanRorgD2Data.DataFieldD2_05_00_2_CMD_Guid };
            var actuator = await Create(Rorg.Vld, type, 7, fields);

            // channel 0xF (all), CMD 0x2 stop - whatever is written to CMD
            var stop = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_05_00_2_CMD_Guid, true), Rorg.Vld, 7);
            Assert.Equal("F2", stop);

            var decoded = await Decode(Rorg.Vld, type, stop, fields);
            Assert.Equal(0xFL, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_2_CHN_Guid]));
            Assert.Equal(2L, Convert.ToInt64(decoded[EnOceanRorgD2Data.DataFieldD2_05_00_2_CMD_Guid]));
        }

        [Theory]
        [InlineData(true, "01000009")]  // COM 0x1, time 0, LRN bit 1 = data, SW 1 = on
        [InlineData(false, "01000008")]
        public async Task A5_38_08_Switching(bool on, string expected)
        {
            var type = EnOceanRorgA5Data.TypeA5_38_08_1Guid;
            var fields = new[] { EnOceanRorgA5Data.DataFieldA5_38_08_1_COM_Guid, EnOceanRorgA5Data.DataFieldA5_38_08_1_SW_Guid };
            var actuator = await Create(Rorg.FourBs, type, 3, fields);

            var data = UserData(await Write(actuator, EnOceanRorgA5Data.DataFieldA5_38_08_1_SW_Guid, on), Rorg.FourBs, 3);
            Assert.Equal(expected, data);

            var decoded = await Decode(Rorg.FourBs, type, data, fields);
            Assert.Equal(1L, Convert.ToInt64(decoded[EnOceanRorgA5Data.DataFieldA5_38_08_1_COM_Guid]));
            Assert.Equal(on, decoded[EnOceanRorgA5Data.DataFieldA5_38_08_1_SW_Guid]);
        }

        [Fact]
        public async Task A5_38_08_Dimming()
        {
            var type = EnOceanRorgA5Data.TypeA5_38_08_2Guid;
            var fields = new[] { EnOceanRorgA5Data.DataFieldA5_38_08_2_COM_Guid, EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid,
                EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid, EnOceanRorgA5Data.DataFieldA5_38_08_2_SW_Guid };
            var actuator = await Create(Rorg.FourBs, type, 3, fields);

            // COM 0x2, dimming value 50 % = 0x80 of 0..255 (EDIMR 0 = absolute), ramp 0, LRN bit 1, STR 0, SW 0
            Assert.Equal("02800008", UserData(await Write(actuator, EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid, 50.0), Rorg.FourBs, 3));
            // ramp 5 s and on, the dimming value stays
            Assert.Equal("02800508", UserData(await Write(actuator, EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid, 5), Rorg.FourBs, 3));
            var data = UserData(await Write(actuator, EnOceanRorgA5Data.DataFieldA5_38_08_2_SW_Guid, true), Rorg.FourBs, 3);
            Assert.Equal("02800509", data);

            var decoded = await Decode(Rorg.FourBs, type, data, fields);
            // 128 / 255 * 100 %, the 8 bit resolution
            Assert.Equal(50d, Convert.ToDouble(decoded[EnOceanRorgA5Data.DataFieldA5_38_08_2_EDIM_Guid]), 0.5);
            Assert.Equal(5d, Convert.ToDouble(decoded[EnOceanRorgA5Data.DataFieldA5_38_08_2_RMP_Guid]));
            Assert.Equal(true, decoded[EnOceanRorgA5Data.DataFieldA5_38_08_2_SW_Guid]);
        }

        [Fact]
        public async Task A5_38_08_TeachIn_Is4BsTeachInWithEep()
        {
            var actuator = await Create(Rorg.FourBs, EnOceanRorgA5Data.TypeA5_38_08_1Guid, 3,
                EnOceanRorgA5Data.DataFieldA5_38_08_1_SW_Guid, EnOceanRorgA5Data.DataFieldA5_38_08_1_TEACHIN_Guid);

            var data = UserData(await Write(actuator, EnOceanRorgA5Data.DataFieldA5_38_08_1_TEACHIN_Guid, true), Rorg.FourBs, 3);

            // FUNC 0x38, TYPE 0x08, manufacturer 0x7FF, LRN type 1 = with EEP, LRN bit 0 = teach-in
            Assert.Equal("E047FF80", data);
            Assert.True(RadioErp1Packet.IsLrnTeachIn(EepTelegrams.Radio(Rorg.FourBs, Convert.FromHexString(data), Sender)));
        }

        [Fact]
        public async Task D2_05_00_TeachIn_IsUteQuery()
        {
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_05_00_1Guid, 7,
                EnOceanRorgD2Data.DataFieldD2_05_00_1_POS_Guid, EnOceanRorgD2Data.DataFieldD2_05_00_1_TEACHIN_Guid);

            var data = UserData(await Write(actuator, EnOceanRorgD2Data.DataFieldD2_05_00_1_TEACHIN_Guid, true), Rorg.Ute, 7);

            // bidirectional, response expected, teach-in request, all channels, manufacturer 0x7FF, TYPE 0x00, FUNC 0x05, RORG D2
            Assert.Equal("80FFFF070005D2", data);
        }

        [Fact]
        public async Task TeachIn_False_SendsNothing()
        {
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_01_00_1Guid, 5, EnOceanRorgD2Data.DataFieldD2_01_00_1_TEACHIN_Guid);

            Assert.False(await actuator.Node(EnOceanRorgD2Data.DataFieldD2_01_00_1_TEACHIN_Guid).SendValue(false, new WriteContextMock()));
            Assert.Empty(actuator.Stick.Radio());
        }

        [Fact]
        public async Task SenderOffset_IsDerivedFromTheDeviceId_WhenNotSet()
        {
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_01_00_1Guid, null, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid);

            var erp1 = await Write(actuator, EnOceanRorgD2Data.DataFieldD2_01_00_1_OV_Guid, 100);

            var offset = EnOceanTypeNode.DerivedSenderOffset(Sender);
            Assert.InRange(offset, 1, 127);
            Assert.Equal("011E64", UserData(erp1, Rorg.Vld, offset));
        }

        [Fact]
        public async Task ReceivedTelegramCase_CannotBeWritten()
        {
            // D2-01-00 CMD 0x4 actuator status response is sent by the actuator
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_01_00_4Guid, 5, EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid);

            Assert.False(await actuator.Node(EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid).SendValue(50, new WriteContextMock()));
            Assert.Empty(actuator.Stick.Radio());
        }

        [Fact]
        public async Task ValueOutOfTheField_IsNotSent()
        {
            var actuator = await Create(Rorg.Vld, EnOceanRorgD2Data.TypeD2_01_00_1Guid, 5, EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid);

            Assert.False(await actuator.Node(EnOceanRorgD2Data.DataFieldD2_01_00_1_IO_Guid).SendValue(64, new WriteContextMock()));
            Assert.Empty(actuator.Stick.Radio());
        }

        [Fact]
        public void SenderId_IsIdBasePlusOffset()
        {
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, 0x7F }, RadioErp1Packet.SenderIdFor(IdBase, 0x7F));
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x01, 0x00 }, RadioErp1Packet.SenderIdFor(new byte[] { 0xFF, 0x80, 0x00, 0x80 }, 0x80));
        }
    }
}
