using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    /// <summary>
    /// A poll dispatches every value once. All groups share one name map, and the poll asked every group to dispatch the
    /// range it read, so each value was dispatched once per group (7 times per poll on a full device): logics behind the
    /// alarms wrote their output dozens of times per poll ("WriteQueue has 27 elements").
    /// </summary>
    public class SolarmanPollTests : DriverFactoryTestBase<SG04LP3DriverFactory>, IAsyncLifetime
    {
        // alarm-2 (register 0x022B) in the alert group
        private static readonly Guid AlarmTemplate = new("0c9778b8-4e05-49b8-b5b4-79267e393fb7");
        private const int AlarmRangeStart = 0x0202;

        private FakeSolarmanLogger _logger;

        public Task InitializeAsync()
        {
            _logger = new FakeSolarmanLogger();
            _logger.Registers[0x022B] = 0x0004;
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _logger.DisposeAsync();
        }

        private NodeInstance Child(NodeInstance parent, Guid template)
        {
            var node = CreateNodeInstance(template);
            parent.InverseThis2ParentNodeInstanceNavigation.Add(node);
            return node;
        }

        private int AlarmRangeReads()
        {
            lock (_logger.Requests)
            {
                return _logger.Requests.Count(a => a.function == 0x03 && a.start == AlarmRangeStart);
            }
        }

        [Fact]
        public async Task Poll_DispatchesEachValueOnce()
        {
            var device = CreateNodeInstance(DriverFactory.DriverGuid);
            device.GetProperty("solarman-ip").ValueString = "127.0.0.1";
            device.GetProperty("solarman-port").ValueInt = _logger.Port;
            device.GetProperty("solarman-serial").ValueString = FakeSolarmanLogger.Serial.ToString();
            device.GetProperty("solarman-poll-interval").ValueInt = 600000;
            device.GetProperty("solarman-device-id").ValueInt = 1;

            var alerts = Child(device, DriverFactory.AlertTypeGuid);
            var alarm = Child(alerts, AlarmTemplate);
            // more groups, every one of them dispatched the alarm before
            Child(device, DriverFactory.GridTypeGuid);
            Child(device, DriverFactory.SolarTypeGuid);
            Child(device, DriverFactory.BatteryTypeGuid);

            var dispatched = 0;
            object lastValue = null;
            await Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, alarm.ObjId, (_, value) =>
            {
                Interlocked.Increment(ref dispatched);
                lastValue = value.Value;
            });

            // CreateDriver only configures the children, the runtime calls Init of the driver itself
            var driver = await CreateDriver(device);
            Assert.True(await driver.Init());
            await driver.Start();
            try
            {
                var watch = Stopwatch.StartNew();
                while ((AlarmRangeReads() == 0 || Volatile.Read(ref dispatched) == 0) && watch.Elapsed < TimeSpan.FromSeconds(10))
                {
                    await Task.Delay(50);
                }
                // give extra dispatches of the same poll time to arrive
                await Task.Delay(500);

                Assert.True(AlarmRangeReads() > 0, "the alarm range was never read");
                Assert.Equal(AlarmRangeReads(), Volatile.Read(ref dispatched));
                Assert.Equal(4d, Convert.ToDouble(lastValue));
            }
            finally
            {
                await driver.Stop();
                await Dispatcher.UnRegisterDispatch(DispatchableType.NodeInstance, alarm.ObjId);
            }
        }
    }
}
