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
    public class WakeOnLanWriteTests : DriverFactoryTestBase<WakeOnLanDriverFactory>
    {
        private async Task<WakeOnLan> CreateNode(string mac)
        {
            var driver = await CreateDriver<WakeOnLanDriver>(WakeOnLanDriverFactory.BusId, WakeOnLanDriverFactory.ValueId);
            var prop = driver.Children[0].DriverContext.NodeInstance.PropertyInstance.Single(a => a.This2PropertyTemplate == WakeOnLanDriverFactory.MacId);
            prop.Value = mac;
            await driver.Init();
            return (WakeOnLan)driver.Children[0];
        }

        private async Task<object> WriteAndRead(WakeOnLan node, object value)
        {
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, new DispatchValue(node.Id, DispatchableType.NodeInstance, value, DateTime.Now, DispatchValueSource.Read));

            // the write queue is processed asynchronously: poll with a deadline
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline)
            {
                var v = Dispatcher.GetValue(DispatchableType.NodeInstance, node.Id);
                if (v != null)
                {
                    return v.Value;
                }

                await Task.Delay(10);
            }

            return null;
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData(true, true)]
        public async Task Write_ConvertibleValue_DispatchesTheBool(object value, bool expected)
        {
            var node = await CreateNode("AABBCCDDEEFF");

            Assert.Equal(expected, await WriteAndRead(node, value));
        }

        [Fact]
        public async Task Write_NotABool_DispatchesNothing()
        {
            var node = await CreateNode("AABBCCDDEEFF");

            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, new DispatchValue(node.Id, DispatchableType.NodeInstance, "wake please", DateTime.Now, DispatchValueSource.Read));
            await Task.Delay(150);

            Assert.Null(Dispatcher.GetValue(DispatchableType.NodeInstance, node.Id));
        }

        [Fact]
        public async Task Write_WithoutMac_DispatchesNothing()
        {
            var node = await CreateNode("");

            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, new DispatchValue(node.Id, DispatchableType.NodeInstance, true, DateTime.Now, DispatchValueSource.Read));
            await Task.Delay(150);

            Assert.Null(Dispatcher.GetValue(DispatchableType.NodeInstance, node.Id));
        }

        [Theory]
        [InlineData("aabbccddeeff")]
        [InlineData("aa:bb:cc:dd:ee:ff")]
        [InlineData("aA-bB-cC-dD-eE-fF")]
        public void BuildWakeUpPacket_IsCaseInsensitive(string mac)
        {
            var packet = WakeOnLan.BuildWakeUpPacket(mac);

            Assert.Equal(WakeOnLan.BuildWakeUpPacket("AABBCCDDEEFF"), packet);
        }

        [Fact]
        public void BuildWakeUpPacket_IsSyncStreamPlusSixteenMacs()
        {
            var packet = WakeOnLan.BuildWakeUpPacket("01:23:45:67:89:AB");

            Assert.Equal(102, packet.Length);
            Assert.All(packet.Take(6), b => Assert.Equal(0xFF, b));
            for (var i = 1; i <= 16; i++)
            {
                Assert.Equal(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB }, packet.Skip(i * 6).Take(6).ToArray());
            }
        }

        [Fact]
        public void WakeOnLanPort_IsTheDiscardPort()
        {
            Assert.Equal(9, WakeOnLan.WakeOnLanPort);
        }

        [Theory]
        [InlineData(2.5, true)]
        [InlineData(0.0, false)]
        [InlineData(-1, true)]
        [InlineData("TRUE", true)]
        [InlineData("-3", true)]
        public void TryConvertToBool_MoreInputs(object input, bool expected)
        {
            Assert.True(WakeOnLan.TryConvertToBool(input, out var result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("yes")]
        [InlineData("")]
        public void TryConvertToBool_Unconvertible_ReturnsFalse(string input)
        {
            Assert.False(WakeOnLan.TryConvertToBool(input, out var result));
            Assert.False(result);
        }
    }
}
