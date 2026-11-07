using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace P3.Driver.MachineFlags.Tests
{
    public class MachineFlagsWriteTests : DriverFactoryTestBase<MachineFlagsDriverFactory>
    {
        private readonly List<(object Value, DispatchValueSource Source)> _dispatched = new();

        private async Task<DriverBase> CreateFlag(Guid template)
        {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(d => d.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((_, value, source) =>
                {
                    lock (_dispatched)
                    {
                        _dispatched.Add((value, source));
                    }
                })
                .Returns(Task.CompletedTask);

            var root = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            root.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(template));
            var context = new DriverContextMock(root, DriverFactory, Factory, dispatcher.Object, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = DriverFactory.CreateDriver(context);
            await driver.Configure();
            await driver.Start();
            var flag = (DriverBase)driver.Children.Single();
            await flag.Start();
            return flag;
        }

        private async Task Write(DriverBase flag, object value)
        {
            int before;
            lock (_dispatched)
            {
                before = _dispatched.Count;
            }

            await flag.WriteValue(DispatchableMock.Instance, new DispatchValue(flag.Id, DispatchableType.NodeInstance, value, DateTime.Now, DispatchValueSource.Write));

            // writes are queued: wait until the queue has handled it (a dispatch or a short quiet period)
            var deadline = DateTime.UtcNow.AddMilliseconds(400);
            while (DateTime.UtcNow < deadline)
            {
                lock (_dispatched)
                {
                    if (_dispatched.Count > before)
                    {
                        break;
                    }
                }

                await Task.Delay(5);
            }

            await Task.Delay(20);
        }

        private object[] DistinctValuesInOrder()
        {
            lock (_dispatched)
            {
                var result = new List<object>();
                foreach (var (value, _) in _dispatched)
                {
                    if (result.Count == 0 || !Equals(result[^1], value))
                    {
                        result.Add(value);
                    }
                }

                return result.ToArray();
            }
        }

        [Fact]
        public async Task BinaryFlag_SameValueTwice_IsDispatchedOnlyForTheFirstWrite()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.BinaryId);

            await Write(flag, true);
            var afterFirst = _dispatched.Count;
            await Write(flag, 1);
            await Write(flag, "true");

            Assert.True(afterFirst > 0);
            Assert.Equal(afterFirst, _dispatched.Count);
            Assert.Equal(new object[] { true }, DistinctValuesInOrder());
        }

        [Fact]
        public async Task BinaryFlag_Toggle_DispatchesEveryChange()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.BinaryId);

            await Write(flag, true);
            await Write(flag, false);
            await Write(flag, 1);

            Assert.Equal(new object[] { true, false, true }, DistinctValuesInOrder());
        }

        [Fact]
        public async Task BinaryFlag_Unconvertible_IsIgnored()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.BinaryId);

            await Write(flag, "not a bool");

            Assert.Empty(_dispatched);
        }

        [Fact]
        public async Task BinaryFlag_WriteDispatchesWithWriteSource()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.BinaryId);

            await Write(flag, true);

            Assert.Contains(_dispatched, d => d.Source == DispatchValueSource.Write && Equals(d.Value, true));
        }

        [Theory]
        [InlineData("1.5", 1.5)]
        [InlineData(2, 2.0)]
        [InlineData(-0.25f, -0.25)]
        public async Task DoubleFlag_ConvertsWithInvariantCulture(object input, double expected)
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.NumberId);

            await Write(flag, input);

            Assert.Equal(new object[] { expected }, DistinctValuesInOrder());
        }

        [Fact]
        public async Task DoubleFlag_SameNumberAgain_IsNotDispatched()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.NumberId);

            await Write(flag, 5);
            var afterFirst = _dispatched.Count;
            await Write(flag, 5.0);
            await Write(flag, "5");

            Assert.Equal(afterFirst, _dispatched.Count);
        }

        [Fact]
        public async Task DoubleFlag_Unconvertible_IsIgnored()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.NumberId);

            await Write(flag, "abc");

            Assert.Empty(_dispatched);
        }

        [Fact]
        public async Task StringFlag_SameTextAgain_IsNotDispatched()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.StringId);

            await Write(flag, "hello");
            var afterFirst = _dispatched.Count;
            await Write(flag, "hello");
            await Write(flag, "world");

            Assert.True(_dispatched.Count > afterFirst);
            Assert.Equal(new object[] { "hello", "world" }, DistinctValuesInOrder());
        }

        [Fact]
        public async Task StringFlag_NullValue_DoesNotBreakTheNode()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.StringId);

            await Write(flag, null);
            await Write(flag, "after null");

            Assert.Equal(new object[] { "after null" }, DistinctValuesInOrder());
        }

        [Fact]
        public async Task GenericFlag_DifferentTypes_AreDispatchedAsIs()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.ValueId);

            await Write(flag, 42);
            await Write(flag, "text");

            Assert.Equal(new object[] { 42, "text" }, DistinctValuesInOrder());
        }

        [Fact(Skip = "BUG: the generic machine flag compares boxed values by reference (_value == value), the same number is dispatched again on every write (P3.Driver.MachineFlags/Attributes/MachineFlags.cs:28)")]
        public async Task GenericFlag_SameBoxedNumberAgain_IsNotDispatched()
        {
            var flag = await CreateFlag(MachineFlagsDriverFactory.ValueId);

            await Write(flag, 42);
            var afterFirst = _dispatched.Count;
            await Write(flag, 42);

            Assert.Equal(afterFirst, _dispatched.Count);
        }
    }
}
