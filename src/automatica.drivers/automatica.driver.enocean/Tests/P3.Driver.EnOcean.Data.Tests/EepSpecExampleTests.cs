using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.DriverFactory;
using P3.Driver.EnOcean.DriverFactory.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Simulated;
using Xunit;

namespace P3.Driver.EnOcean.Data.Tests
{
    /// <summary>
    /// Telegrams with hand computed values from the EEP 2.6.x specification, the case selection of multi-message
    /// profiles and the telegrams the driver sends.
    /// </summary>
    public class EepSpecExampleTests : DriverFactoryTestBase<EnOceanDriverFactory>
    {
        private const string Sender = "0581A2B3";

        private async Task<Dictionary<Guid, NodeInstance>> Device(Guid rorgGuid, Guid funcGuid, Guid typeGuid, params Guid[] dataFields)
        {
            await Dispatcher.ClearValues();

            var driverNode = CreateNodeInstance(EnOceanDriverFactory.DriverGuidId);
            var learned = CreateNodeInstance(EnOceanDriverFactory.LearnedGuid);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(learned);
            var rorg = CreateNodeInstance(rorgGuid);
            learned.InverseThis2ParentNodeInstanceNavigation.Add(rorg);
            var func = CreateNodeInstance(funcGuid);
            rorg.InverseThis2ParentNodeInstanceNavigation.Add(func);
            var type = CreateNodeInstance(typeGuid);
            type.SetProperty("enocean-serialnumber", Sender);
            func.InverseThis2ParentNodeInstanceNavigation.Add(type);

            var nodes = new Dictionary<Guid, NodeInstance> { [Guid.Empty] = driverNode };
            foreach (var dataField in dataFields)
            {
                var node = CreateNodeInstance(dataField);
                type.InverseThis2ParentNodeInstanceNavigation.Add(node);
                nodes[dataField] = node;
            }

            return nodes;
        }

        private async Task<IDictionary<Guid, DispatchValue>> Receive(Dictionary<Guid, NodeInstance> device, Rorg rorg, string data, byte status = 0x00)
        {
            var driver = await CreateDriver<EnOceanDriver>(device[Guid.Empty]);
            await Dispatcher.ClearValues();
            driver.TelegramReceived(EepTelegrams.Radio(rorg, Convert.FromHexString(data), Sender, status));
            return Dispatcher.GetValues(DispatchableType.NodeInstance);
        }

        private static object Value(IDictionary<Guid, DispatchValue> values, Dictionary<Guid, NodeInstance> device, Guid field) =>
            values.TryGetValue(device[field].ObjId, out var value) ? value.Value : null;

        [Theory]
        [InlineData("0000FF08", 0.0)]    // DB1 n = 255 => 0 °C
        [InlineData("00008008", 19.92156862745098)]
        [InlineData("00000008", 40.0)]   // n = 0 => +40 °C
        public async Task A5_02_05_Temperature_IsInverted(string data, double expected)
        {
            var device = await Device(EnOceanRorgA5Data.RorgA5Guid, EnOceanRorgA5Data.FunctionA5_02Guid, EnOceanRorgA5Data.TypeA5_02_05_5Guid,
                EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            var values = await Receive(device, Rorg.FourBs, data);

            Assert.Equal(expected, (double)Value(values, device, EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid), 6);
        }

        [Fact]
        public async Task A5_02_05_TeachInTelegram_IsNotDispatched()
        {
            var device = await Device(EnOceanRorgA5Data.RorgA5Guid, EnOceanRorgA5Data.FunctionA5_02Guid, EnOceanRorgA5Data.TypeA5_02_05_5Guid,
                EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid);

            // DB0.3 = 0: teach-in, DB3..DB1 carry the EEP and manufacturer, not a temperature
            var values = await Receive(device, Rorg.FourBs, "08280B80");

            Assert.Null(Value(values, device, EnOceanRorgA5Data.DataFieldA5_02_05_5_TMP_Guid));
        }

        [Fact]
        public async Task A5_07_03_Occupancy_SupplyVoltageIlluminationAndMotion()
        {
            var device = await Device(EnOceanRorgA5Data.RorgA5Guid, EnOceanRorgA5Data.FunctionA5_07Guid, EnOceanRorgA5Data.TypeA5_07_03_3Guid,
                EnOceanRorgA5Data.DataFieldA5_07_03_3_SVC_Guid, EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid,
                EnOceanRorgA5Data.DataFieldA5_07_03_3_PIRS_Guid);

            // DB3 = 200 => 4.0 V, DB2..DB1 bits 8..17 = 500 lx, DB0.7 = 1 motion, DB0.3 = 1 data telegram
            // 500 = 0b0111110100 => DB2 = 0b01111101, DB1 = 0b00xxxxxx
            var values = await Receive(device, Rorg.FourBs, "C87D0088");

            Assert.Equal(4.0, (double)Value(values, device, EnOceanRorgA5Data.DataFieldA5_07_03_3_SVC_Guid), 6);
            Assert.Equal(500.0, (double)Value(values, device, EnOceanRorgA5Data.DataFieldA5_07_03_3_ILL_Guid), 6);
            Assert.Equal(true, Value(values, device, EnOceanRorgA5Data.DataFieldA5_07_03_3_PIRS_Guid));

            values = await Receive(device, Rorg.FourBs, "C87D0008");
            Assert.Equal(false, Value(values, device, EnOceanRorgA5Data.DataFieldA5_07_03_3_PIRS_Guid));
        }

        [Theory]
        [InlineData("09", true)]  // CO = 1 contact closed, LRN = 1
        [InlineData("08", false)] // CO = 0 contact open
        public async Task D5_00_01_Contact(string data, bool expected)
        {
            var device = await Device(EnOceanRorgD5Data.RorgD5Guid, EnOceanRorgD5Data.FunctionD5_00Guid, EnOceanRorgD5Data.TypeD5_00_01_1Guid,
                EnOceanRorgD5Data.DataFieldD5_00_01_1_CO_Guid);

            var values = await Receive(device, Rorg.OneBs, data);

            Assert.Equal(expected, Value(values, device, EnOceanRorgD5Data.DataFieldD5_00_01_1_CO_Guid));
        }

        [Fact]
        public async Task F6_02_01_RockerPressAndRelease()
        {
            var device = await Device(EnOceanRorgF6Data.RorgF6Guid, EnOceanRorgF6Data.FunctionF6_02Guid, EnOceanRorgF6Data.TypeF6_02_01_1Guid,
                EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid, EnOceanRorgF6Data.DataFieldF6_02_01_1_EB_Guid);

            // PTM200: button BI (R1 = 3) pressed, T21 = 1, NU = 1
            var values = await Receive(device, Rorg.Rps, "70", 0x30);
            Assert.Equal(3, Value(values, device, EnOceanRorgF6Data.DataFieldF6_02_01_1_R1_Guid));
            Assert.Equal(true, Value(values, device, EnOceanRorgF6Data.DataFieldF6_02_01_1_EB_Guid));

            // released: data 0x00, NU = 0
            values = await Receive(device, Rorg.Rps, "00", 0x20);
            Assert.Equal(false, Value(values, device, EnOceanRorgF6Data.DataFieldF6_02_01_1_EB_Guid));
        }

        [Theory]
        [InlineData("F0", WindowState.Closed)] // 0b1111xxxx
        [InlineData("E0", WindowState.Open)]   // 0b1110xxxx, handle down from the side
        [InlineData("C0", WindowState.Open)]   // 0b1100xxxx
        [InlineData("D0", WindowState.Tilted)] // 0b1101xxxx
        public async Task F6_10_00_WindowHandle(string data, WindowState expected)
        {
            var device = await Device(EnOceanRorgF6Data.RorgF6Guid, EnOceanRorgF6Data.FunctionF6_10Guid, EnOceanRorgF6Data.TypeF6_10_00_1Guid,
                EnOceanRorgF6Data.DataFieldF6_10_00_1_WIN_Guid);

            var values = await Receive(device, Rorg.Rps, data, 0x20);

            Assert.Equal(expected, Value(values, device, EnOceanRorgF6Data.DataFieldF6_10_00_1_WIN_Guid));
        }

        [Fact]
        public async Task D2_01_00_StatusNodes_IgnoreMeasurementTelegrams()
        {
            // CMD 0x4 actuator status response: OV (output value) at offset 17, 7 bit
            var device = await Device(EnOceanRorgD2Data.RorgD2Guid, EnOceanRorgD2Data.FunctionD2_01Guid, EnOceanRorgD2Data.TypeD2_01_00_4Guid,
                EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid);

            // CMD 4, channel 0, output value 100 %
            var values = await Receive(device, Rorg.Vld, "040064");
            Assert.Equal(100, Value(values, device, EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid));

            // CMD 7 actuator measurement response (unit + 32 bit value): no status, nothing dispatched
            values = await Receive(device, Rorg.Vld, "076000000064");
            Assert.Null(Value(values, device, EnOceanRorgD2Data.DataFieldD2_01_00_4_OV_Guid));
        }

        [Theory]
        [InlineData(0x5A, 18.0)]  // 0..250 => 0..50 °C
        [InlineData(250, 50.0)]
        public async Task D2_14_30_Temperature_IsScaledWithinTheEnumSegment(int raw, double expected)
        {
            var device = await Device(EnOceanRorgD2Data.RorgD2Guid, EnOceanRorgD2Data.FunctionD2_14Guid, EnOceanRorgD2Data.TypeD2_14_30_27Guid,
                EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_RAW_Guid);

            var data = new byte[6];
            // TMP8: offset 23, 8 bit
            for (var i = 0; i < 8; i++)
            {
                if (((raw >> (7 - i)) & 1) == 1)
                {
                    var pos = 23 + i;
                    data[pos >> 3] |= (byte)(0x80 >> (pos & 7));
                }
            }

            var values = await Receive(device, Rorg.Vld, Convert.ToHexString(data));

            Assert.Equal(expected, (double)Value(values, device, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid), 6);
            Assert.Equal(raw, Value(values, device, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_RAW_Guid));
        }

        [Fact]
        public async Task D2_14_30_StatusCode_IsNotDispatchedAsTemperature()
        {
            var device = await Device(EnOceanRorgD2Data.RorgD2Guid, EnOceanRorgD2Data.FunctionD2_14Guid, EnOceanRorgD2Data.TypeD2_14_30_27Guid,
                EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_RAW_Guid);

            // TMP8 = 255 (offset 23): error code, not 0..50 °C
            var values = await Receive(device, Rorg.Vld, "000001FE0000");

            Assert.Null(Value(values, device, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_Guid));
            Assert.Equal(255, Value(values, device, EnOceanRorgD2Data.DataFieldD2_14_30_27_TMP8_RAW_Guid));
        }

        private sealed class RecordingStream : BaseStream
        {
            public List<byte[]> Written { get; } = new();

            public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

            public override Task<bool> Open() => Task.FromResult(true);

            public override Task<bool> Close() => Task.FromResult(true);

            public override Task<EnOceanPacket> WriteFrame(EnOceanPacket frame)
            {
                Written.Add(frame.RawData.ToArray());
                return Task.FromResult<EnOceanPacket>(null);
            }

            public override void Pause()
            {
            }

            public override void Continue()
            {
            }
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

        private sealed class TestRelay : EnOceanSimulatedRelay
        {
            public TestRelay(IDriverContext driverContext, P3.Driver.EnOcean.Driver driver) : base(driverContext, driver)
            {
            }

            public Task Send(object value, IWriteContext context) => Write(value, context);
        }

        /// <summary>
        /// ERP1 data of a sent RADIO_ERP1 frame: RORG, user data, sender id, status.
        /// </summary>
        private static byte[] Erp1(byte[] frame) => frame.AsSpan(6, (frame[1] << 8) | frame[2]).ToArray();

        [Theory]
        [InlineData(true, 0x10)]  // rocker AI (R1 = 0) pressed
        [InlineData(false, 0x30)] // rocker A0 (R1 = 1) pressed
        public async Task SimulatedRelay_SendsAPressAndAReleaseLikeAPtm200(bool on, byte pressed)
        {
            var stream = new RecordingStream();
            var driver = new P3.Driver.EnOcean.Driver(stream);
            var context = new DriverContextMock(CreateNodeInstance(EnOceanDriverFactory.DriverGuidId), DriverFactory, Factory, Dispatcher,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, TimeProvider.System);
            var relay = new TestRelay(context, driver);
            var writeContext = new WriteContextMock();

            await relay.Send(on, writeContext);

            Assert.Equal(2, stream.Written.Count);
            // press: data, empty id base, status T21 = 1 / NU = 1
            Assert.Equal(new byte[] { 0xF6, pressed, 0x30 }, Erp1(stream.Written[0]));
            // release: no button, status T21 = 1 / NU = 0
            Assert.Equal(new byte[] { 0xF6, 0x00, 0x20 }, Erp1(stream.Written[1]));
            Assert.Equal(new object[] { on }, writeContext.Dispatched);
        }
    }
}
