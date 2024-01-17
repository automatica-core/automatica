using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Xunit;

namespace P3.Driver.WakeOnLan.Tests
{
    public class WakeOnLanTests : DriverFactoryTestBase<WakeOnLanDriverFactory>
    {
        private readonly byte[] _wolMagicPacket =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE,
            0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD,
            0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC,
            0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB,
            0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA,
            0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
        };
        [Fact]
        public async void Test_WakeOnLanValue()
        {
            var driver = await CreateDriver<WakeOnLanDriver>(WakeOnLanDriverFactory.BusId, WakeOnLanDriverFactory.ValueId);

            var prop = driver.Children[0].DriverContext.NodeInstance.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplate == WakeOnLanDriverFactory.MacId);
            Assert.NotNull(prop);
            prop.Value = "AABBCCDDEEFF";


            Assert.True(driver.Children.Count == 1);
            Assert.IsType<WakeOnLan>(driver.Children[0]);

            await driver.Init();
            var con = driver.Children[0] as WakeOnLan;

            
            Assert.NotNull(con);
            await ((DriverBase)con).WriteValue(DispatchableMock.Instance, new DispatchValue(con.Id, DispatchableType.NodeInstance, 100, DateTime.Now,DispatchValueSource.Read));

            await Task.Delay(200);

            var value = Dispatcher.GetValue(DispatchableType.NodeInstance, con.Id);
            Assert.Equal("AABBCCDDEEFF", value.Value);

        }

        [Fact]
        public void Test_WakeOnLanPacket1()
        {
            var packet = WakeOnLan.BuildWakeUpPacket("AABBCCDDEEFF");

            Assert.Equal(_wolMagicPacket, packet);
        }

        [Fact]
        public void Test_WakeOnLanPacket2()
        {
            var packet = WakeOnLan.BuildWakeUpPacket("AA:BB:CC:DD:EE:FF");

            Assert.Equal(_wolMagicPacket, packet);
        }

        [Fact]
        public void Test_WakeOnLanPacket3()
        {
            var packet = WakeOnLan.BuildWakeUpPacket("AA-BB-CC-DD-EE-FF");

            Assert.Equal(_wolMagicPacket, packet);
        }
    }
}
