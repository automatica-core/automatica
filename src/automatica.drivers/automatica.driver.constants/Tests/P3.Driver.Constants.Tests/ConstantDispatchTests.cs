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

namespace P3.Driver.Constants.Tests
{
    public class ConstantDispatchTests : DriverFactoryTestBase<ConstantsDriverFactory>
    {
        private readonly List<(Guid Id, object Value, DispatchValueSource Source)> _dispatched = new();

        private async Task<(IDriver Driver, Constant Constant)> Create(Guid template, object propertyValue = null)
        {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(d => d.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((self, value, source) =>
                {
                    lock (_dispatched)
                    {
                        _dispatched.Add((self.Id, value, source));
                    }
                })
                .Returns(Task.CompletedTask);

            var root = CreateNodeInstance(ConstantsDriverFactory.BusId);
            var node = CreateNodeInstance(template);
            if (propertyValue != null)
            {
                node.PropertyInstance.Single(p => p.This2PropertyTemplateNavigation.Key == "const_value").Value = propertyValue;
            }

            root.InverseThis2ParentNodeInstanceNavigation.Add(node);
            var context = new DriverContextMock(root, DriverFactory, Factory, dispatcher.Object, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = DriverFactory.CreateDriver(context);
            await driver.Configure();
            var constant = (Constant)driver.Children.Single();
            await constant.Init();
            return (driver, constant);
        }

        private async Task WaitForDispatches(int count)
        {
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline)
            {
                lock (_dispatched)
                {
                    if (_dispatched.Count >= count)
                    {
                        return;
                    }
                }

                await Task.Delay(5);
            }
        }

        [Theory]
        [InlineData(-42, -42d)]
        [InlineData(0, 0d)]
        [InlineData(int.MaxValue, (double)int.MaxValue)]
        public async Task NumericConstant_IsADouble(int configured, double expected)
        {
            var (_, constant) = await Create(ConstantsDriverFactory.ValueId, configured);

            Assert.Equal(expected, constant.Value);
            Assert.IsType<double>(constant.Value);
        }

        [Fact]
        public async Task StringConstant_EmptyString()
        {
            var (_, constant) = await Create(ConstantsDriverFactory.StringValueId, "");

            Assert.Equal("", constant.Value);
        }

        [Fact]
        public async Task Start_DispatchesTheValueOnce()
        {
            var (_, constant) = await Create(ConstantsDriverFactory.PiId);

            await constant.Start();
            await WaitForDispatches(1);

            var (id, value, _) = Assert.Single(_dispatched);
            Assert.Equal(constant.Id, id);
            Assert.Equal(Math.PI, value);
            await constant.Stop();
        }

        [Fact]
        public async Task Write_IgnoresTheWrittenValueAndRedispatchesTheConstant()
        {
            var (_, constant) = await Create(ConstantsDriverFactory.TrueId);
            await constant.Start();
            await WaitForDispatches(1);

            await constant.WriteValue(DispatchableMock.Instance, new DispatchValue(constant.Id, DispatchableType.NodeInstance, false, DateTime.Now, DispatchValueSource.Write));
            await WaitForDispatches(2);

            Assert.Equal(2, _dispatched.Count);
            Assert.All(_dispatched, d => Assert.Equal(true, d.Value));
            Assert.Equal(true, constant.Value);
            await constant.Stop();
        }

        [Fact]
        public async Task Stop_ThenTimerTick_DoesNotDispatch()
        {
            var (_, constant) = await Create(ConstantsDriverFactory.FalseId);
            await constant.Start();
            await WaitForDispatches(1);

            Assert.True(await constant.Stop());
            var count = _dispatched.Count;
            await Task.Delay(50);

            Assert.Equal(count, _dispatched.Count);
        }

        [Fact]
        public async Task Read_DispatchesTheValue()
        {
            var (_, constant) = await Create(ConstantsDriverFactory.HalfPiId);

            await constant.Read();
            await WaitForDispatches(1);

            Assert.Equal(Math.PI / 2, Assert.Single(_dispatched).Value);
        }

        [Fact]
        public async Task EveryTemplate_CreatesAConstantNode()
        {
            foreach (var id in new[] { ConstantsDriverFactory.ValueId, ConstantsDriverFactory.PiId, ConstantsDriverFactory.HalfPiId, ConstantsDriverFactory.DoublePiId,
                         ConstantsDriverFactory.TrueId, ConstantsDriverFactory.FalseId, ConstantsDriverFactory.StringValueId })
            {
                var (_, constant) = await Create(id);
                Assert.IsType<Constant>(constant);
            }
        }
    }
}
