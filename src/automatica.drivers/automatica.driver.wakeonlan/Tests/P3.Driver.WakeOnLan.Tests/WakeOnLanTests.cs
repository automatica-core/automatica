using System.Linq;
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
        public void Test_WakeOnLanValue()
        {
            var root = CreateNodeInstance(WakeOnLanDriverFactory.BusId);
            var node = CreateNodeInstance(WakeOnLanDriverFactory.ValueId);

            root.InverseThis2ParentNodeInstanceNavigation.Add(node);

            var prop = node.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplate == WakeOnLanDriverFactory.MacId);
            Assert.NotNull(prop);
            prop.Value = "AABBCCDDEEFF";

            var driver = CreateDriver(root);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<WakeOnLan>(driver.Children[0]);

            var con = driver.Children[0] as WakeOnLan;

            
            Assert.NotNull(con);
            con.WriteValue(DispatchableMock.Instance, 100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal("AABBCCDDEEFF", value);

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
