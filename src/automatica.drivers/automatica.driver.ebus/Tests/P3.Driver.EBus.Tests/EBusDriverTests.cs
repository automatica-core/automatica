using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.EBusFactory;
using P3.Driver.EBusFactory.Wolf;
using Xunit;
using static P3.Driver.EBus.Tests.FakeBus;

namespace P3.Driver.EBus.Tests
{
    public class EBusDriverTests : DriverFactoryTestBase<EBusFactory.EBusFactory>
    {
        private readonly FakeBus _bus = new();

        public EBusDriverTests()
        {
            // values read from a Wolf BWL-1
            _bus.Registers[2] = 363;
            _bus.Registers[12] = unchecked((ushort)-18);
            _bus.Registers[13] = 3225;
            _bus.Registers[14] = 504;
            _bus.Registers[15] = 268;
            _bus.Registers[19] = 530;
            _bus.Registers[22] = 2997;
            _bus.Registers[385] = 650;
            _bus.Registers[550] = 35543;
            _bus.Registers[551] = 1;
            _bus.Registers[554] = 19598;
        }

        private (NodeInstance Bus, Dictionary<string, NodeInstance> Values) CreateTree(params (int Register, double Scale)[] customRegisters)
        {
            var bus = CreateNodeInstance(DriverFactory.DriverGuid);
            bus.SetProperty(EBusFactory.EBusFactory.IpKey, "192.0.2.1");
            bus.SetProperty(EBusFactory.EBusFactory.PortKey, 5000);
            bus.SetProperty(EBusFactory.EBusFactory.MasterAddressKey, 255);
            bus.SetProperty(EBusFactory.EBusFactory.PollIntervalKey, 3600);

            var device = CreateNodeInstance(WolfBwl1.Template);
            device.SetProperty(EBusFactory.EBusFactory.DeviceAddressKey, 8);
            bus.InverseThis2ParentNodeInstanceNavigation.Add(device);

            var values = new Dictionary<string, NodeInstance>();
            foreach (var dp in WolfBwl1.DataPoints)
            {
                var child = CreateNodeInstance(dp.Template);
                device.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[dp.Key] = child;
            }

            foreach (var (register, scale) in customRegisters)
            {
                var child = CreateNodeInstance(WolfBwl1.CustomRegisterTemplate);
                child.SetProperty(EBusFactory.EBusFactory.RegisterKey, register);
                child.SetProperty(EBusFactory.EBusFactory.ScaleKey, scale);
                child.SetProperty(EBusFactory.EBusFactory.SignedKey, false);
                device.InverseThis2ParentNodeInstanceNavigation.Add(child);
                values[$"register-{register}"] = child;
            }

            return (bus, values);
        }

        private async Task<EBusDriver> StartAsync(NodeInstance bus)
        {
            var driver = (EBusDriver)await CreateDriver(bus);
            driver.TransportFactory = (host, port) =>
            {
                Assert.Equal("192.0.2.1", host);
                Assert.Equal(5000, port);
                return _bus;
            };
            await driver.Init();
            await driver.Start();
            await driver.Connected.WaitAsync(TimeSpan.FromSeconds(20));
            return driver;
        }

        private object Value(NodeInstance node) => Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;

        private static async Task Eventually(Func<bool> condition, string what)
        {
            for (var i = 0; i < 200 && !condition(); i++)
            {
                await Task.Delay(20);
            }

            Assert.True(condition(), what);
        }

        [Fact]
        public async Task Bwl1_RegistersArePolled()
        {
            var (bus, values) = CreateTree((385, 0.1));
            var driver = await StartAsync(bus);
            try
            {
                Assert.Equal(32.25, Value(values["flow-temperature"]));
                Assert.Equal(29.97, Value(values["return-temperature"]));
                Assert.Equal(36.3, Value(values["flow-setpoint"]));
                Assert.Equal(50.4, Value(values["hot-water-temperature"]));
                Assert.Equal(53.0, Value(values["hot-water-setpoint"]));
                Assert.Equal(-1.8, Value(values["outside-temperature"]));
                Assert.Equal(26.8, Value(values["circuit-2-flow-temperature"]));
                Assert.Equal(19598L, Convert.ToInt64(Value(values["compressor-hours"])));
                Assert.Equal(101079L, Convert.ToInt64(Value(values["compressor-starts"])));
                Assert.Equal(65.0, Value(values["register-385"]));

                // only reads were sent, all to the heat pump
                Assert.All(_bus.Requests, r => Assert.Equal(Hex("ff 08 50 22"), r.Take(4).ToArray()));
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Bwl1_BroadcastsSetTheStatus()
        {
            var (bus, values) = CreateTree();
            var driver = await StartAsync(bus);
            try
            {
                _bus.Inject(Hex("03 fe 05 03 08 01 00 48 ff 44 1e 34 02 d5"));
                _bus.Inject(Hex("03 fe fe 01 0a 45 30 30 30 20 30 36 3a 32 39 ed"));

                // the fault code comes last, the status is set by then
                await Eventually(() => Value(values["fault-code"]) is { } code && Convert.ToInt64(code) == 0, "fault code from FE 01");
                Assert.Equal(true, Value(values["running"]));
                Assert.Equal(true, Value(values["circulation-pump"]));

                // the same broadcast from another master is not this heat pump
                _bus.Inject(Hex("f1 fe 05 03 08 01 01 00 ff 44 ff 34 02 " + Crc(Hex("f1 fe 05 03 08 01 01 00 ff 44 ff 34 02"))));
                await Task.Delay(200);
                Assert.Equal(true, Value(values["running"]));
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task ClosedAdapter_Reconnects()
        {
            EBusDriver.ReconnectDelay = TimeSpan.FromMilliseconds(50);
            var (bus, values) = CreateTree();
            var connects = 0;
            var buses = new List<FakeBus>();
            var driver = (EBusDriver)await CreateDriver(bus);
            driver.TransportFactory = (_, _) =>
            {
                connects++;
                var fake = new FakeBus();
                fake.Registers[13] = (ushort)(3000 + connects);
                buses.Add(fake);
                return fake;
            };
            await driver.Init();
            await driver.Start();
            try
            {
                await driver.Connected.WaitAsync(TimeSpan.FromSeconds(20));
                Assert.Equal(30.01, Value(values["flow-temperature"]));

                buses[0].Close();

                await Eventually(() => Equals(Value(values["flow-temperature"]), 30.02), "reconnected and polled again");
                Assert.True(buses[0].Disposed);
            }
            finally
            {
                await driver.Stop();
            }

            Assert.True(buses.Last().Disposed);
        }

        private static string Crc(byte[] telegram) => EBusProtocol.Checksum(telegram).ToString("x2");

        [Fact]
        public void TemplateGuids_AreUnique_AndTextsExist()
        {
            var keys = new List<string>();
            var guids = new List<Guid>();
            var proxy = DispatchProxy.Create<INodeTemplateFactory, Recorder>();
            ((Recorder)(object)proxy).OnCall = (name, args) =>
            {
                if (name is nameof(INodeTemplateFactory.CreateNodeTemplate) or nameof(INodeTemplateFactory.CreatePropertyTemplate))
                {
                    if ((Guid)args[0] != DriverFactory.DriverGuid)
                    {
                        guids.Add((Guid)args[0]);
                    }

                    keys.Add((string)args[1]);
                    keys.Add((string)args[2]);
                }
            };
            DriverFactory.InitNodeTemplates(proxy);

            Assert.Equal(guids.Count, guids.Distinct().Count());
            foreach (var language in new[] { "de", "en" })
            {
                using var stream = typeof(EBusFactory.EBusFactory).Assembly.GetManifestResourceStream($"P3.Driver.EBusFactory.Resources.EBus-{language}.json");
                using var json = JsonDocument.Parse(stream!);
                foreach (var key in keys.Where(k => k.StartsWith("EBUS.")))
                {
                    var element = json.RootElement;
                    foreach (var part in key.Split('.'))
                    {
                        Assert.True(element.TryGetProperty(part, out element), $"{key} is missing in {language}");
                    }
                }
            }
        }

        [Fact]
        public void Manifest_ShipsTheProtocolLibrary()
        {
            // the plugin package only contains the output and the listed dependencies, without the protocol library the server cannot create the templates
            var factory = typeof(EBusFactory.EBusFactory).Assembly;
            using var stream = factory.GetManifestResourceStream("P3.Driver.EBusFactory.automatica-manifest.json");
            using var json = JsonDocument.Parse(stream!);
            var dependencies = json.RootElement.GetProperty("Automatica").GetProperty("Dependencies").EnumerateArray().Select(a => a.GetString()).ToList();

            var ownLibraries = factory.GetReferencedAssemblies().Where(a => a.Name!.StartsWith("P3.Driver.EBus")).Select(a => $"{a.Name}.dll").ToList();

            Assert.Contains("P3.Driver.EBus.dll", ownLibraries);
            Assert.All(ownLibraries, library => Assert.Contains(library, dependencies));
        }

        public class Recorder : DispatchProxy
        {
            public Action<string, object[]> OnCall { get; set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                OnCall?.Invoke(targetMethod.Name, args);
                return targetMethod.ReturnType == typeof(void) ? null : Activator.CreateInstance(targetMethod.ReturnType);
            }
        }
    }
}
