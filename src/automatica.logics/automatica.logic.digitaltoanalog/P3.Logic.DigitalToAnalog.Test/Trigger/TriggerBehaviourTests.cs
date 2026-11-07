using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.DigitalToAnalog.Tests.TestSupport;
using P3.Logic.DigitalToAnalog.Trigger;
using Xunit;

namespace P3.Logic.DigitalToAnalog.Tests.Trigger
{
    public class TriggerBehaviourTests : LogicTest<TriggerLogicFactory>
    {
        private readonly ValueRecorder _output;

        // the logic under test dispatches into its own recording dispatcher, not into the shared one of the base class
        private new ILogic Logic { get; }

        public TriggerBehaviourTests()
        {
            var dispatcher = new RecordingDispatcher();
            Logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, dispatcher));
            Logic.Start().GetAwaiter().GetResult();
            _output = new ValueRecorder(dispatcher, TriggerLogicFactory.RuleOutput);
        }

        private TriggerRule Rule => (TriggerRule)Logic;

        private IList<ILogicOutputChanged> Trigger(object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, value);
        }

        private IList<ILogicOutputChanged> SetValueInput(object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleValueInput), Dispatchable, value);
        }

        private void SetParameter(Guid template, object value)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);
        }

        public static IEnumerable<object[]> TriggerTruthTable => new List<object[]>
        {
            new object[] { true, true },
            new object[] { "true", true },
            new object[] { "True", true },
            new object[] { "TRUE", true },
            new object[] { false, false },
            new object[] { "false", false },
            new object[] { 1, false },
            new object[] { 0, false },
            new object[] { "1", false },
            new object[] { "yes", false },
            new object[] { 1.0d, false },
            new object[] { null, false },
        };

        [Theory]
        [MemberData(nameof(TriggerTruthTable))]
        public void Trigger_OnlyBooleanTrueEmitsTheValue(object triggerValue, bool emits)
        {
            SetValueInput(10);

            var result = Trigger(triggerValue);

            if (emits)
            {
                Assert.Single(result);
                Assert.Equal(10, result.First().Value);
                Assert.Equal(TriggerLogicFactory.RuleOutput, result.First().Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            }
            else
            {
                Assert.Empty(result);
            }
        }

        [Fact]
        public void Trigger_DefaultEmptyParameterAndNoValueInput_IsIgnored()
        {
            // the value parameter defaults to "" - nothing to emit
            Assert.Empty(Trigger(true));
        }

        [Fact]
        public void ValueInput_ReturnsNoOutput()
        {
            Assert.Empty(SetValueInput(42));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValueInput_EmptyOrNull_TriggerIsIgnored(object value)
        {
            SetValueInput(5);
            SetValueInput(value);

            Assert.Empty(Trigger(true));
        }

        [Fact]
        public void ValueInput_StringIsEmittedAsIs()
        {
            // only the text parameter is parsed as number, the V input value is passed through untouched
            SetValueInput("50");

            var result = Trigger(true);

            Assert.Equal("50", Assert.Single(result).Value);
        }

        [Fact]
        public void ValueInput_LastValueWins()
        {
            SetValueInput(1);
            SetValueInput(2);
            SetValueInput(true);

            Assert.Equal(true, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public void Trigger_Repeated_EmitsEveryTime()
        {
            SetValueInput(3);

            Assert.Single(Trigger(true));
            Assert.Single(Trigger(true));
            Assert.Empty(Trigger(false));
            Assert.Single(Trigger(true));
        }

        public static IEnumerable<object[]> ParameterParsingData => new List<object[]>
        {
            new object[] { "50.5", 50.5d },
            new object[] { "-3", -3d },
            new object[] { "1e3", 1000d },
            new object[] { " 7 ", 7d },
            new object[] { "0", 0d },
            new object[] { "abc", "abc" },
            new object[] { "50,5", "50,5" },
        };

        [Theory]
        [MemberData(nameof(ParameterParsingData))]
        public async Task ValueParameter_NumericTextIsParsedInvariant(string parameter, object expected)
        {
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = parameter;
            await Logic.Start();

            Assert.Equal(expected, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public async Task Restart_WithUnchangedParameter_KeepsTheValueFromTheValueInput()
        {
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = "5";
            await Logic.Start();

            SetValueInput(9);
            await Logic.Start();

            Assert.Equal(9, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public async Task Restart_WithChangedParameter_OverridesTheValueFromTheValueInput()
        {
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = "5";
            await Logic.Start();
            SetValueInput(9);

            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = "6";
            await Logic.Start();

            Assert.Equal(6d, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public void ValueParameter_ChangedAtRuntime_OverridesTheValueInput()
        {
            SetValueInput(9);
            SetParameter(TriggerLogicFactory.ValueParameter, "8");

            Assert.Equal(8d, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public void ValueParameter_SameValueAgain_DoesNotOverrideTheValueInput()
        {
            SetParameter(TriggerLogicFactory.ValueParameter, "8");
            SetValueInput(9);
            SetParameter(TriggerLogicFactory.ValueParameter, "8");

            Assert.Equal(9, Assert.Single(Trigger(true)).Value);
        }

        [Theory]
        [InlineData(250, 250)]
        [InlineData("250", 250)]
        [InlineData("abc", 0)]
        [InlineData(null, 0)]
        [InlineData(-1, -1)]
        public void DelayParameter_IsConvertedOrFallsBackToZero(object value, int expected)
        {
            SetParameter(TriggerLogicFactory.DelayParameter, value);

            Assert.Equal(expected, Rule.Delay);
        }

        [Fact]
        public void Delay_Default_IsZero()
        {
            Assert.Equal(0, Rule.Delay);
        }

        [Fact]
        public void Trigger_NegativeDelay_EmitsDirectly()
        {
            SetParameter(TriggerLogicFactory.DelayParameter, -1);
            SetValueInput(4);

            Assert.Equal(4, Assert.Single(Trigger(true)).Value);
        }

        [Fact]
        public async Task Trigger_WithDelay_DispatchesTheValueLater()
        {
            SetParameter(TriggerLogicFactory.DelayParameter, 150);
            SetValueInput(11);

            Assert.Empty(Trigger(true));
            Assert.Empty(_output.Values);

            var values = await _output.WaitForCountAsync(1);
            Assert.Equal(11, values.Single());
        }

        [Fact]
        public async Task Trigger_WithDelay_UsesTheValueAtTriggerTime()
        {
            SetParameter(TriggerLogicFactory.DelayParameter, 100);
            SetValueInput(11);

            Trigger(true);
            SetValueInput(12);

            var values = await _output.WaitForCountAsync(1);
            Assert.Equal(11, values.Single());
        }

        [Fact]
        public async Task Trigger_WithDelayAndNoValue_DispatchesNothing()
        {
            SetParameter(TriggerLogicFactory.DelayParameter, 20);

            Assert.Empty(Trigger(true));

            await Task.Delay(100);
            Assert.Empty(_output.Values);
        }
    }
}
