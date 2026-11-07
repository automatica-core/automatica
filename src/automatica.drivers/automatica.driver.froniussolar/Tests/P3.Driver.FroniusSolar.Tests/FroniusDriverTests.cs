using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using FroniusSolarClient;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.FroniusSolarFactory;
using P3.Driver.FroniusSolarFactory.Categories;
using Xunit;

namespace P3.Driver.FroniusSolar.Tests
{
    /// <summary>
    /// The Fronius category nodes with a real SolarClient against a loopback Fronius Solar API
    /// </summary>
    public sealed class FroniusDriverTests : IDisposable
    {
        private static readonly Guid DeviceTemplate = new("b41759b5-99b7-4844-85aa-176dfee82682");
        private static readonly Guid DeviceState = new("61be4df1-b5ca-4961-b833-99a2a96c044c");

        private static readonly Guid AcPower = new("c4c8ffee-7633-49fe-8a54-d91c62bf7f77");
        private static readonly Guid AcVoltage = new("c29a29a4-f258-4bb9-88a7-7a20d9485673");
        private static readonly Guid AcFrequency = new("463902e9-ffac-4842-9d8a-d9ae7e8e07f3");
        private static readonly Guid DcVoltage = new("d7120013-b520-4c92-b70a-2940f1495c8b");
        private static readonly Guid DayEnergy = new("8d367af6-04e3-4fd0-bc2a-a565d62a6266");
        private static readonly Guid TotalEnergy = new("18dcb8aa-7d2c-4875-9689-04049c150c01");
        private static readonly Guid DeviceStatus = new("71be07e9-8d6e-44a0-8b8d-46fe8cb90fee");
        private static readonly Guid ErrorCode = new("9c64ca64-168b-4d6e-b114-567ec2f505f0");

        private static readonly Guid L1AcCurrent = new("b31f4edc-c867-4768-93da-848d40d7b151");
        private static readonly Guid L3AcVoltage = new("74e4603d-9641-46ec-baf7-5e725a01d333");
        private static readonly Guid Ambient = new("f0195b05-2e64-4aaa-9788-561325765ab0");
        private static readonly Guid FanFrontLeft = new("eb48c2be-4459-4344-989e-6e12108d99a7");
        private static readonly Guid FanFrontRight = new("12f8d880-c53d-497a-aa34-91cd2a89cc24");
        private static readonly Guid FanBackLeft = new("7001f45f-89b5-4e2e-8cbf-a9661e6dcb32");
        private static readonly Guid FanBackRight = new("21ca6a18-5ca7-4c40-857d-b947c053d70c");

        private static readonly Guid FlowDeviceType = new("54cf9d87-2c6e-4ae3-bfb3-f55655cd2dd6");
        private static readonly Guid FlowCurrentPower = new("1a867ef3-4676-457c-9d1b-aafd256dd15c");
        private static readonly Guid FlowEnergyToday = new("5740baf8-15ca-44db-9b30-1ceb472c5739");
        private static readonly Guid FlowEnergyTotal = new("8c4b1a53-6de0-4a94-adf1-14a625613ecd");

        private readonly NodeTemplateFactoryMock _factory = new();
        private readonly P3.Driver.FroniusSolarFactory.FroniusSolarFactory _driverFactory = new();
        private readonly CapturingDispatcher _dispatcher = new();
        private LoopbackHttpServer? _server;

        public FroniusDriverTests()
        {
            _driverFactory.InitNodeTemplates(_factory);
        }

        public void Dispose()
        {
            _server?.Dispose();
        }

        private LoopbackHttpServer Api(Func<LoopbackHttpServer.Request, LoopbackHttpServer.Response> handler)
        {
            _server = new LoopbackHttpServer(handler);
            return _server;
        }

        private static LoopbackHttpServer.Response Ok(string json) => new(200, json);

        private SolarClient ClientFor(LoopbackHttpServer server) => new($"127.0.0.1:{server.Port}", 1, NullLogger.Instance);

        private NodeInstance Node(Guid template, params NodeInstance[] children)
        {
            var node = _factory.CreateNodeInstance(template);
            node.ObjId = Guid.NewGuid();
            foreach (var child in children)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }
            return node;
        }

        private static void SetProperty(NodeInstance node, string key, object value)
        {
            node.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == key).Value = value;
        }

        private (FroniusDeviceAttribute Device, DriverContextMock Context) Device(string ip = "127.0.0.1", int deviceId = 1, bool disabled = false)
        {
            var node = Node(DeviceTemplate);
            SetProperty(node, "fronius-ip", ip);
            SetProperty(node, "fronius-device-id", deviceId);
            SetProperty(node, "fronius-poll-interval", 30000);
            SetProperty(node, "fronius-disabled", disabled);

            var context = new DriverContextMock(node, _driverFactory, _factory, _dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            return (new FroniusDeviceAttribute(context), context);
        }

        private static async Task<T> Category<T>(T category) where T : FroniusCategoryAttribute
        {
            Assert.True(await category.Configure());
            return category;
        }

        private static double Num(object? value) => Convert.ToDouble(value);

        // ---------- templates ----------

        [Fact]
        public void InitNodeTemplates_CreatesDeviceCategoriesAndValues()
        {
            Assert.Equal("fronius-solar-device", _factory.GetNodeTemplateById(DeviceTemplate).Key);
            Assert.Equal("fronius-solar-common-data", _factory.GetNodeTemplateById(_driverFactory.CommonInverterData).Key);
            Assert.Equal("fronius-solar-p3-inverter-data", _factory.GetNodeTemplateById(_driverFactory.P3InverterData).Key);
            Assert.Equal("fronius-solar-power-flow-realtime-data", _factory.GetNodeTemplateById(_driverFactory.PowerFlowRealtimeData).Key);
            Assert.Equal("fan-front-right-speed", _factory.GetNodeTemplateById(FanFrontRight).Key);
            Assert.Equal("fan-back-right-speed", _factory.GetNodeTemplateById(FanBackRight).Key);
        }

        // ---------- P3 inverter data ----------

        [Fact]
        public async Task P3InverterData_EachFan_ReadsItsOwnField()
        {
            // regression: fan-front-right-speed dispatched the back-right fan value
            var server = Api(_ => Ok(FroniusJson.P3InverterData()));
            var (device, context) = Device();
            Assert.True(await device.Init());

            var fl = Node(FanFrontLeft);
            var fr = Node(FanFrontRight);
            var bl = Node(FanBackLeft);
            var br = Node(FanBackRight);
            var categoryNode = Node(_driverFactory.P3InverterData, fl, fr, bl, br);
            var category = await Category(new P3InverterDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Equal(1100d, Num(_dispatcher.LastValueOf(fl.ObjId)));
            Assert.Equal(1200d, Num(_dispatcher.LastValueOf(fr.ObjId)));
            Assert.Equal(1300d, Num(_dispatcher.LastValueOf(bl.ObjId)));
            Assert.Equal(1400d, Num(_dispatcher.LastValueOf(br.ObjId)));
        }

        [Fact]
        public async Task P3InverterData_PhaseValues_AreDispatchedAndRequestIsDeviceScoped()
        {
            var server = Api(_ => Ok(FroniusJson.P3InverterData()));
            var (device, context) = Device(deviceId: 3);
            Assert.True(await device.Init());

            var l1 = Node(L1AcCurrent);
            var l3 = Node(L3AcVoltage);
            var ambient = Node(Ambient);
            var categoryNode = Node(_driverFactory.P3InverterData, l1, l3, ambient);
            var category = await Category(new P3InverterDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Equal(1.1, Num(_dispatcher.LastValueOf(l1.ObjId)), 6);
            Assert.Equal(230.3, Num(_dispatcher.LastValueOf(l3.ObjId)), 6);
            Assert.Equal(41d, Num(_dispatcher.LastValueOf(ambient.ObjId)));

            var request = Assert.Single(server.Requests);
            Assert.Equal("GET", request.Method);
            var uri = new Uri("http://localhost" + request.Path);
            Assert.Equal("/solar_api/v1/GetInverterRealtimeData.cgi", uri.AbsolutePath);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal("Device", query["Scope"]);
            Assert.Equal("3", query["DeviceId"]);
            Assert.Equal("3PInverterData", query["DataCollection"]);
        }

        // ---------- common inverter data ----------

        [Fact]
        public async Task CommonInverterData_Values_AreDispatchedAndDeviceStateIsOnline()
        {
            var server = Api(_ => Ok(FroniusJson.CommonInverterData()));
            var (device, context) = Device();
            Assert.True(await device.Init());
            var stateNode = Node(DeviceState);
            Assert.NotNull(device.CreateDriverNode(context.Copy(stateNode, NullLogger.Instance)));

            var power = Node(AcPower);
            var voltage = Node(AcVoltage);
            var frequency = Node(AcFrequency);
            var dc = Node(DcVoltage);
            var day = Node(DayEnergy);
            var total = Node(TotalEnergy);
            var status = Node(DeviceStatus);
            var error = Node(ErrorCode);
            var categoryNode = Node(_driverFactory.CommonInverterData, power, voltage, frequency, dc, day, total, status, error);
            var category = await Category(new CommonInverterDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Equal(true, _dispatcher.LastValueOf(stateNode.ObjId));
            Assert.Equal(580d, Num(_dispatcher.LastValueOf(power.ObjId)));
            Assert.Equal(231.4, Num(_dispatcher.LastValueOf(voltage.ObjId)), 6);
            Assert.Equal(49.98, Num(_dispatcher.LastValueOf(frequency.ObjId)), 6);
            Assert.Equal(401.7, Num(_dispatcher.LastValueOf(dc.ObjId)), 6);
            Assert.Equal(8123.5, Num(_dispatcher.LastValueOf(day.ObjId)), 6);
            Assert.Equal(1000000d, Num(_dispatcher.LastValueOf(total.ObjId)));
            Assert.Equal(7, Convert.ToInt32(_dispatcher.LastValueOf(status.ObjId)));
            Assert.Equal(307, Convert.ToInt32(_dispatcher.LastValueOf(error.ObjId)));

            Assert.All(server.Requests, r => Assert.Contains("DataCollection=CommonInverterData", r.Path));
        }

        [Fact]
        public async Task CommonInverterData_ResponseWithoutBody_ReportsDeviceStateOffline()
        {
            var server = Api(_ => Ok(FroniusJson.NoBody()));
            var (device, context) = Device();
            Assert.True(await device.Init());
            var stateNode = Node(DeviceState);
            device.CreateDriverNode(context.Copy(stateNode, NullLogger.Instance));

            var power = Node(AcPower);
            var categoryNode = Node(_driverFactory.CommonInverterData, power);
            var category = await Category(new CommonInverterDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Equal(false, _dispatcher.LastValueOf(stateNode.ObjId));
            Assert.Equal(0d, Num(_dispatcher.LastValueOf(power.ObjId)));
        }

        // ---------- power flow ----------

        [Fact]
        public async Task PowerFlow_MatchingInverter_ValuesAreDispatched()
        {
            var server = Api(_ => Ok(FroniusJson.PowerFlow("1")));
            var (device, context) = Device(deviceId: 1);
            Assert.True(await device.Init());

            var type = Node(FlowDeviceType);
            var power = Node(FlowCurrentPower);
            var today = Node(FlowEnergyToday);
            var total = Node(FlowEnergyTotal);
            var categoryNode = Node(_driverFactory.PowerFlowRealtimeData, type, power, today, total);
            var category = await Category(new PowerFlowRealtimeDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Equal(102, Convert.ToInt32(_dispatcher.LastValueOf(type.ObjId)));
            Assert.Equal(580, Convert.ToInt32(_dispatcher.LastValueOf(power.ObjId)));
            Assert.Equal(8123.5, Num(_dispatcher.LastValueOf(today.ObjId)), 6);
            Assert.Equal(1000000d, Num(_dispatcher.LastValueOf(total.ObjId)));
            Assert.Contains("/solar_api/v1/GetPowerFlowRealtimeData.fcgi", Assert.Single(server.Requests).Path);
        }

        [Fact]
        public async Task PowerFlow_OtherInverterId_DispatchesNothing()
        {
            var server = Api(_ => Ok(FroniusJson.PowerFlow("2")));
            var (device, context) = Device(deviceId: 1);
            Assert.True(await device.Init());

            var power = Node(FlowCurrentPower);
            var categoryNode = Node(_driverFactory.PowerFlowRealtimeData, power);
            var category = await Category(new PowerFlowRealtimeDataAttribute(context.Copy(categoryNode, NullLogger.Instance), ClientFor(server), device));

            await category.PollAttributes();

            Assert.Empty(_dispatcher.ValuesOf(power.ObjId));
        }

        // ---------- device lifecycle ----------

        [Theory]
        [InlineData("not-an-ip")]
        [InlineData("")]
        [InlineData("300.1.1.1")]
        public async Task Init_InvalidIp_ReturnsFalse(string ip)
        {
            var (device, _) = Device(ip: ip);

            Assert.False(await device.Init());
        }

        [Fact]
        public async Task Init_ReadsDeviceIdAndPollInterval()
        {
            var (device, _) = Device(deviceId: 7);

            Assert.True(await device.Init());

            Assert.Equal((byte)7, device.DeviceId);
            Assert.Equal(30000, device.PollInterval);
            Assert.False(device.IsDisabled);
        }

        [Fact]
        public async Task Start_WithoutInit_Throws()
        {
            var (device, _) = Device();

            await Assert.ThrowsAsync<ArgumentException>(() => device.Start());
        }

        private static System.Timers.Timer PollTimer(FroniusDeviceAttribute device)
        {
            var field = typeof(FroniusDeviceAttribute).GetField("_pollTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (System.Timers.Timer)field!.GetValue(device)!;
        }

        private static int ElapsedHandlerCount(System.Timers.Timer timer)
        {
            var field = typeof(System.Timers.Timer).GetField("_onIntervalElapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (field!.GetValue(timer) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        [Fact]
        public async Task Stop_StopsThePollTimer()
        {
            var (device, _) = Device();
            Assert.True(await device.Init());

            await device.Start();
            Assert.True(PollTimer(device).Enabled);

            await device.Stop();

            Assert.False(PollTimer(device).Enabled);
            Assert.Equal(0, ElapsedHandlerCount(PollTimer(device)));
        }

        [Fact]
        public async Task Start_RegistersExactlyOneElapsedHandler()
        {
            // Stop removes the handler again (see Stop_StopsThePollTimer), so a restart cannot poll twice per tick
            var (device, _) = Device();
            Assert.True(await device.Init());

            await device.Start();
            try
            {
                Assert.Equal(1, ElapsedHandlerCount(PollTimer(device)));
                Assert.Equal(30000d, PollTimer(device).Interval);
            }
            finally
            {
                await device.Stop();
            }
        }

        [Fact]
        public async Task Start_Disabled_DoesNotPoll()
        {
            var (device, _) = Device(disabled: true);
            Assert.True(await device.Init());

            await device.Start();

            Assert.True(device.IsDisabled);
            Assert.False(PollTimer(device).Enabled);
            await device.Stop();
        }
    }
}
