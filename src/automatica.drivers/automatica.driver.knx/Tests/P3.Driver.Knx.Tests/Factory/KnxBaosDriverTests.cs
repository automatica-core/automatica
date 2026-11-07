using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Drivers;
using Knx.Falcon;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Knx.DriverFactory.Factories.Baos;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using Xunit;

namespace P3.Driver.Knx.Tests.Factory
{
    /// <summary>
    /// The BAOS driver without a reachable serial interface (the module is not plugged in or the port does not exist).
    /// </summary>
    public class KnxBaosDriverTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        private KnxBaosDriver CreateDriver()
        {
            var baos = new BaosKnxFactory();
            var driverNode = CreateNodeInstance(KnxIpDriverFactory.KnxBaos);
            var context = new DriverContextMock(driverNode, baos, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            return (KnxBaosDriver)baos.CreateDriver(context);
        }

        [Fact]
        public async Task Start_InterfaceNotReachable_KeepsTheDriverRunning()
        {
            var driver = CreateDriver();
            Assert.True(await driver.Init());
            Assert.True(await driver.Configure());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                Assert.True(await driver.Start(cts.Token)); // retried in the background
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Stop_WithoutStart_DoesNotThrow()
        {
            var driver = CreateDriver();
            Assert.True(await driver.Init());

            await driver.Stop();
        }

        [Fact]
        public async Task Write_InterfaceNotOpen_ReturnsFalse()
        {
            var driver = CreateDriver();
            Assert.True(await driver.Init());

            Assert.False(await driver.Write(null, "5", new GroupValue(true), CancellationToken.None));
        }

        [Fact]
        public async Task Read_InterfaceNotOpen_ReturnsFalse()
        {
            var driver = CreateDriver();
            Assert.True(await driver.Init());

            Assert.False(await driver.Read("5"));
        }

        [Fact]
        public async Task DataPointInd_UnknownDatapoint_IsIgnored()
        {
            var driver = CreateDriver();
            Assert.True(await driver.Init());
            Assert.True(await driver.Configure());

            await driver.DataPointInd(new[] { new P3.Knx.Core.Baos.Driver.Data.DatapointValue { DatapointId = 999, Data = new byte[] { 0x01 } } });
        }
    }
}
