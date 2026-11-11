using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Knx.Falcon;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using Xunit;

namespace P3.Driver.Knx.Tests.Attributes
{
    /// <summary>
    /// Telegram round trips through the group address nodes: a payload received from the bus is dispatched,
    /// writing the dispatched value back produces the same payload again.
    /// </summary>
    public class KnxDptRoundTripTests : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        private sealed class WriteContextStub : IWriteContext
        {
            public Task DispatchValue(object value, CancellationToken token = default) => Task.CompletedTask;
        }

        private static readonly MethodInfo WriteMethod = typeof(DriverBase).GetMethod("Write", BindingFlags.Instance | BindingFlags.NonPublic,
            new[] { typeof(object), typeof(IWriteContext), typeof(CancellationToken) })!;

        private static Task Write(IDriverNode node, object value) =>
            (Task)WriteMethod.Invoke(node, new[] { value, new WriteContextStub(), CancellationToken.None })!;

        private NodeInstance CreateByKey(string key, int address, int subType)
        {
            var node = Factory.CreateNodeInstanceByKey(key);
            Assert.True(node != null, key);
            node.ObjId = Guid.NewGuid();
            node.IsReadable = true;
            node.IsWriteable = true;
            node.WriteOnlyIfChanged = false; // the same value is written back
            node.SetProperty("knx-address", address);
            if (subType > 0)
            {
                node.SetProperty("knx-dpt-sub", subType);
            }
            return node;
        }

        private async Task<(KnxAttributeTests.RecordingKnxDriver Driver, IDriverNode Node)> CreateDriver(NodeInstance groupAddress, int main = 1, int middle = 2)
        {
            var gateway = CreateNodeInstance(KnxIpDriverFactory.KnxGateway);
            gateway.SetProperty("knx-ip", "127.0.0.1");
            var threeLevel = CreateNodeInstance(KnxIpDriverFactory.KnxIpGateway3LevelInterface);
            var mainNode = CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMainAddress);
            mainNode.SetProperty("knx-address", main);
            var middleNode = CreateNodeInstance(KnxIpDriverFactory.KnxIp3LevelMiddleAddress);
            middleNode.SetProperty("knx-address", middle);

            gateway.InverseThis2ParentNodeInstanceNavigation.Add(threeLevel);
            threeLevel.InverseThis2ParentNodeInstanceNavigation.Add(mainNode);
            mainNode.InverseThis2ParentNodeInstanceNavigation.Add(middleNode);
            middleNode.InverseThis2ParentNodeInstanceNavigation.Add(groupAddress);

            var context = new DriverContextMock(gateway, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = new KnxAttributeTests.RecordingKnxDriver(context);
            Assert.True(await driver.Configure());

            var node = driver.Children.Single().Children.Single().Children.Single().Children.Single();
            return (driver, node);
        }

        private async Task<object> WaitForDispatch(NodeInstance node)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                var value = Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId);
                if (value != null)
                {
                    return value.Value;
                }

                await Task.Delay(10);
            }

            throw new TimeoutException($"{node.Name} did not dispatch a value");
        }

        private static GroupValue Payload(string hex, int bits) =>
            bits is > 0 and <= 6 ? new GroupValue(Convert.FromHexString(hex)[0], bits) : new GroupValue(Convert.FromHexString(hex));

        [Theory]
        // DPT 1 boolean
        [InlineData("knx-dpt1", 0, "01", 1, true)]
        [InlineData("knx-dpt1", 0, "00", 1, false)]
        // DPT 5 unsigned 8 bit (5.001 scaling, 5.004 raw percent, 5.010 counter)
        [InlineData("knx-dpt5", 0, "FF", 0, 100)]
        [InlineData("knx-dpt5", 0, "00", 0, 0)]
        [InlineData("knx-dpt5", 4, "C8", 0, 200)]
        [InlineData("knx-dpt5", 10, "FF", 0, 255)]
        [InlineData("knx-dpt5", 10, "01", 0, 1)]
        // DPT 6 signed 8 bit
        [InlineData("knx-dpt6", 0, "80", 0, -128)]
        [InlineData("knx-dpt6", 0, "7F", 0, 127)]
        // DPT 7 unsigned 16 bit
        [InlineData("knx-dpt7", 0, "FFFF", 0, 65535)]
        [InlineData("knx-dpt7", 0, "0000", 0, 0)]
        // DPT 8 signed 16 bit
        [InlineData("knx-dpt8", 0, "8000", 0, -32768)]
        [InlineData("knx-dpt8", 0, "7FFF", 0, 32767)]
        [InlineData("knx-dpt8", 0, "FFFF", 0, -1)]
        // DPT 9 2 byte float
        [InlineData("knx-dpt9", 0, "0C1A", 0, 21.0)]
        [InlineData("knx-dpt9", 0, "8A24", 0, -30.0)]
        [InlineData("knx-dpt9", 0, "0000", 0, 0.0)]
        // DPT 13 signed 32 bit
        [InlineData("knx-dpt13", 0, "80000000", 0, int.MinValue)]
        [InlineData("knx-dpt13", 0, "7FFFFFFF", 0, int.MaxValue)]
        // catalogue codecs
        [InlineData("knx-dpt12", 0, "FFFFFFFF", 0, 4294967295L)]
        [InlineData("knx-dpt14", 0, "41A80000", 0, 21.0)]
        [InlineData("knx-dpt14", 0, "C2C80000", 0, -100.0)]
        [InlineData("knx-dpt17", 0, "3F", 0, 63)]
        [InlineData("knx-dpt20", 0, "02", 0, 2)]
        [InlineData("knx-dpt29", 0, "FFFFFFFFFFFFFFFF", 0, -1L)]
        [InlineData("knx-dpt29", 0, "001FFFFFFFFFFFFF", 0, 9007199254740991L)] // 2^53 - 1, dispatched as double
        public async Task BusPayload_DispatchedAndWrittenBack_IsTheSamePayload(string key, int subType, string hex, int bits, object expected)
        {
            var node = CreateByKey(key, 30, subType);
            var (driver, ga) = await CreateDriver(node);

            driver.GroupValueReceived("1/2/30", Payload(hex, bits));
            var value = await WaitForDispatch(node);

            if (expected is double d)
            {
                Assert.Equal(d, Convert.ToDouble(value), 3);
            }
            else if (expected is bool b)
            {
                Assert.Equal(b, value);
            }
            else
            {
                Assert.Equal(Convert.ToInt64(expected), Convert.ToInt64(value));
            }

            await Write(ga, value);

            var written = driver.Writes.Single();
            Assert.Equal("1/2/30", written.Address);
            Assert.Equal(hex, Convert.ToHexString(written.Value.Value));
        }

        [Theory]
        [InlineData("0C1E05", 12, 30, 5)]
        [InlineData("173B3B", 23, 59, 59)]
        [InlineData("000000", 0, 0, 0)]
        [InlineData("2C1E05", 12, 30, 5)] // Monday, the node is a time of day node
        public async Task Dpt10_BusTime_IsDispatchedAsTimeOfDay(string hex, int hours, int minutes, int seconds)
        {
            var node = CreateByKey("knx-dpt10", 31, 0);
            var (driver, _) = await CreateDriver(node);

            driver.GroupValueReceived("1/2/31", Payload(hex, 0));

            Assert.Equal(new TimeSpan(hours, minutes, seconds), await WaitForDispatch(node));
        }

        [Fact]
        public async Task Dpt10_DateTimeWrite_SendsTheTimeOfDay()
        {
            var node = CreateByKey("knx-dpt10", 34, 0);
            var (driver, ga) = await CreateDriver(node);

            await Write(ga, new DateTime(2026, 3, 1, 12, 30, 5));

            Assert.Equal("0C1E05", Convert.ToHexString(driver.Writes.Single().Value.Value)); // no day of week
        }

        [Theory(Skip = "BUG: KnxDpt10Attribute writes TimeSpan/TimeOnly/string values with new KnxTime(t, 0), 0 is DayOfWeek.Sunday, so every time is sent with day 7 instead of no day")]
        [InlineData("0C1E05", 12, 30, 5)]
        [InlineData("173B3B", 23, 59, 59)]
        [InlineData("000000", 0, 0, 0)]
        public async Task Dpt10_TimeOfDay_RoundTrips(string hex, int hours, int minutes, int seconds)
        {
            var node = CreateByKey("knx-dpt10", 31, 0);
            var (driver, ga) = await CreateDriver(node);

            driver.GroupValueReceived("1/2/31", Payload(hex, 0));
            var value = await WaitForDispatch(node);
            Assert.Equal(new TimeSpan(hours, minutes, seconds), value);

            await Write(ga, value);
            Assert.Equal(hex, Convert.ToHexString(driver.Writes.Single().Value.Value));
        }

        [Fact]
        public async Task Dpt16_AsciiString_RoundTrips()
        {
            var node = CreateByKey("knx-dpt16", 32, 0);
            var (driver, ga) = await CreateDriver(node);
            var payload = new byte[14];
            "Hello KNX"u8.ToArray().CopyTo(payload, 0);

            driver.GroupValueReceived("1/2/32", new GroupValue(payload));
            var value = await WaitForDispatch(node);
            Assert.Equal("Hello KNX", value);

            await Write(ga, value);
            Assert.Equal(payload, driver.Writes.Single().Value.Value);
        }

        [Fact]
        public async Task Dpt16_FullLengthString_RoundTrips()
        {
            var node = CreateByKey("knx-dpt16", 33, 0);
            var (driver, ga) = await CreateDriver(node);
            var payload = "ABCDEFGHIJKLMN"u8.ToArray();

            driver.GroupValueReceived("1/2/33", new GroupValue(payload));
            Assert.Equal("ABCDEFGHIJKLMN", await WaitForDispatch(node));

            await Write(ga, "ABCDEFGHIJKLMN");
            Assert.Equal(payload, driver.Writes.Single().Value.Value);
        }

        /// <summary>
        /// A number (the bitcoin price from the blockchain ticker) was dropped with "Could not convert value correctly"
        /// </summary>
        [Fact]
        public async Task Dpt16_Number_IsWrittenAsInvariantText()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
            try
            {
                var node = CreateByKey("knx-dpt16", 44, 0);
                var (driver, ga) = await CreateDriver(node);

                await Write(ga, 73831.5d);

                var expected = new byte[14];
                "73831.5"u8.ToArray().CopyTo(expected, 0);
                Assert.Equal(expected, driver.Writes.Single().Value.Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public async Task Dpt16_LongText_IsCutToFourteenCharacters()
        {
            var node = CreateByKey("knx-dpt16", 45, 0);
            var (driver, ga) = await CreateDriver(node);

            await Write(ga, "73831.67727413896");

            Assert.Equal("73831.67727413"u8.ToArray(), driver.Writes.Single().Value.Value);
        }

        [Theory]
        [InlineData(0, 0, 0, "0/0/0")]
        [InlineData(31, 7, 255, "31/7/255")]
        [InlineData(15, 0, 1, "15/0/1")]
        public async Task GroupAddress_IsComposedOfMainMiddleAndSubGroup(int main, int middle, int sub, string expected)
        {
            var node = CreateByKey("knx-dpt1", sub, 0);
            var (driver, ga) = await CreateDriver(node, main, middle);

            Assert.Equal(expected, ((KnxGroupAddress)ga).GroupAddress);

            driver.GroupValueReceived(expected, new GroupValue(true));
            Assert.Equal(true, await WaitForDispatch(node));

            await Write(ga, false);
            Assert.Equal(expected, driver.Writes.Single().Address);
        }

        [Fact]
        public async Task GroupValueReceived_OtherAddress_IsNotDispatched()
        {
            var node = CreateByKey("knx-dpt5", 40, 10);
            var (driver, _) = await CreateDriver(node);

            driver.GroupValueReceived("1/2/41", new GroupValue(new byte[] { 0x05 }));
            driver.GroupValueReceived("2/2/40", new GroupValue(new byte[] { 0x06 }));
            driver.GroupValueReceived("1/2/40", new GroupValue(new byte[] { 0x07 }));

            Assert.Equal(7, Convert.ToInt32(await WaitForDispatch(node)));
        }

        [Fact(Skip = "BUG: KnxDpt9Attribute sends 0x7FFF (KNX invalid data) for values above 670760.96, the catalogue float codec clamps to the largest valid value")]
        public async Task Dpt9_OutOfRangeWrite_IsNotSentAsInvalidData()
        {
            var node = CreateByKey("knx-dpt9", 42, 0);
            var (driver, ga) = await CreateDriver(node);

            // 2 byte float ends at 670760.96
            await Record.ExceptionAsync(() => Write(ga, 1e9));

            Assert.All(driver.Writes, a => Assert.NotEqual("7FFF", Convert.ToHexString(a.Value.Value)));
        }

        [Fact]
        public async Task Read_SendsAReadRequestForTheGroupAddress()
        {
            var node = CreateByKey("knx-dpt9", 43, 0);
            var (driver, ga) = await CreateDriver(node);

            await ga.Read();

            Assert.Equal("1/2/43", driver.Reads.Single());
        }
    }
}
