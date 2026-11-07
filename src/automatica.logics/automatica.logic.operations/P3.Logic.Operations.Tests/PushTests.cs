using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Push;
using Xunit;
using static P3.Logic.Operations.Tests.OperationsTestHelper;

namespace P3.Logic.Operations.Tests
{
    public class PushTests : LogicTest<PushLogicFactory>
    {
        private int Duration => (int)typeof(PushLogic)
            .GetField("_duration", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Logic)!;

        private void SetDuration(object value)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(PushLogicFactory.Duration), Dispatchable, value);
        }

        [Fact]
        public void Factory_DurationParameter_Defaults300()
        {
            var param = GetLogicInterfaceByTemplate(PushLogicFactory.Duration);

            Assert.Equal(300L, param.ValueInteger);
            Assert.Equal(3, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void Start_ReadsDurationParameter()
        {
            Assert.Equal(300, Duration);
        }

        [Theory]
        [InlineData(20, 20)]
        [InlineData(1, 1)]
        [InlineData(0, 1)]
        [InlineData(-50, 1)]
        [InlineData("75", 75)]
        [InlineData(12.4, 12)]
        public void DurationParameter_IsClampedToAtLeastOneMs(object value, int expected)
        {
            SetDuration(value);

            Assert.Equal(expected, Duration);
        }

        [Fact]
        public void DurationParameter_ChangeReturnsNoOutputs()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(PushLogicFactory.Duration), Dispatchable, 10));
        }

        [Fact]
        public async Task Input_ReturnsNoSynchronousOutputs_AndPulsesTrueThenFalse()
        {
            await Context.Dispatcher.ClearValues();
            SetDuration(150);

            var sw = Stopwatch.StartNew();
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(PushLogicFactory.RuleInput), Dispatchable, true);
            Assert.Empty(outputs);

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, PushLogicFactory.RuleOutput, true, 2000), "pulse did not start");
            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, PushLogicFactory.RuleOutput, false, 3000), "pulse did not end");

            // the off telegram comes after the configured pulse length
            Assert.True(sw.ElapsedMilliseconds >= 140, $"pulse ended after {sw.ElapsedMilliseconds}ms");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData(null)]
        public async Task Input_AnyValue_TriggersPulse(object value)
        {
            await Context.Dispatcher.ClearValues();
            SetDuration(1);

            Logic.ValueChanged(GetLogicInterfaceByTemplate(PushLogicFactory.RuleInput), Dispatchable, value);

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, PushLogicFactory.RuleOutput, false, 3000));
        }

        [Fact]
        public async Task OutputInterface_AsInput_DoesNotPulse()
        {
            await Context.Dispatcher.ClearValues();
            SetDuration(1);

            Logic.ValueChanged(GetLogicInterfaceByTemplate(PushLogicFactory.RuleOutput), Dispatchable, true);
            await Task.Delay(50);

            Assert.Null(DispatchedValue(Context.Dispatcher, PushLogicFactory.RuleOutput));
        }
    }
}
