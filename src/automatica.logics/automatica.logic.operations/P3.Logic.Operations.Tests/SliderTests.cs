using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Slider;
using Xunit;
using static P3.Logic.Operations.Tests.OperationsTestHelper;

namespace P3.Logic.Operations.Tests
{
    public class SliderTests : LogicTest<SliderLogicFactory>
    {
        private IList<ILogicOutputChanged> Value(object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValue), Dispatchable, value);
        }

        private IList<ILogicOutputChanged> Reset(object value = null)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputReset), Dispatchable, value ?? true);
        }

        private object EmittedValue(IList<ILogicOutputChanged> outputs) => Output(outputs, SliderLogicFactory.RuleOutput);

        // ---------- factory / defaults ----------

        [Fact]
        public void Factory_ParameterDefaults_AreMin0Max100()
        {
            Assert.Equal(0d, GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMinParam).ValueDouble);
            Assert.Equal(100d, GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMaxParam).ValueDouble);
            Assert.Equal(7, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void GetDataForVisu_Initially_HasBoundsButNoLastValue()
        {
            var data = Assert.IsType<SliderData>(Logic.GetDataForVisu());

            Assert.Null(data.LastValue);
            Assert.Equal(0d, data.MinValue);
            Assert.Equal(100d, data.MaxValue);
        }

        // ---------- value input (fix ab70dd2d: the clamped double is emitted, not the raw input) ----------

        [Theory]
        [InlineData(50, 50d)]
        [InlineData(0, 0d)]
        [InlineData(100, 100d)]
        [InlineData(150, 100d)]
        [InlineData(100.5, 100d)]
        [InlineData(-5, 0d)]
        [InlineData(-0.1, 0d)]
        [InlineData(33.3, 33.3d)]
        [InlineData(true, 1d)]
        [InlineData(false, 0d)]
        [InlineData("42", 42d)]
        [InlineData("1000", 100d)]
        public void ValueInput_EmitsClampedDouble(object input, double expected)
        {
            var emitted = EmittedValue(Value(input));

            Assert.IsType<double>(emitted);
            Assert.Equal(expected, (double)emitted, 6);
        }

        [Fact]
        public void ValueInput_Int_IsEmittedAsDouble()
        {
            var emitted = EmittedValue(Value(42));

            Assert.Equal(42d, Assert.IsType<double>(emitted));
        }

        [Fact]
        public void ValueInput_Null_IsTreatedAsZero()
        {
            Assert.Equal(0d, EmittedValue(Value(null)));
        }

        [Fact]
        public void ValueInput_Unparsable_EmitsNothing()
        {
            Assert.Empty(Value("abc"));
            Assert.Null(((SliderData)Logic.GetDataForVisu()).LastValue);
        }

        [Fact(Skip = "BUG: NaN passes both clamp checks and is emitted (and re-emitted every time, NaN != NaN) (SliderLogic.cs:68-86)")]
        public void ValueInput_NaN_IsNotEmitted()
        {
            Assert.Empty(Value(double.NaN));
        }

        [Fact]
        public void ValueInput_SameValueTwice_SecondIsSuppressed()
        {
            Assert.Equal(40d, EmittedValue(Value(40)));
            Assert.Empty(Value(40));
            Assert.Empty(Value(40d));
            Assert.Equal(41d, EmittedValue(Value(41)));
        }

        [Fact]
        public void ValueInput_ClampedValueThenTheClampedValueItself_IsSuppressed()
        {
            Assert.Equal(100d, EmittedValue(Value(150)));
            // last value is the clamped 100
            Assert.Empty(Value(100));
        }

        [Fact]
        public void ValueInput_StoresClampedLastValueForVisu()
        {
            Value(150);

            var data = (SliderData)Logic.GetDataForVisu();
            Assert.Equal(100d, data.LastValue);
        }

        // ---------- reset (fix ab70dd2d: reset clears the last value) ----------

        [Fact]
        public void Reset_EmitsZero()
        {
            Value(70);

            var emitted = EmittedValue(Reset());

            Assert.Equal(0d, Convert.ToDouble(emitted));
        }

        [Fact]
        public void Reset_ThenSameValueAsBefore_IsEmittedAgain()
        {
            Assert.Equal(70d, EmittedValue(Value(70)));
            Reset();

            Assert.Equal(70d, EmittedValue(Value(70)));
        }

        [Fact]
        public void Reset_SetsLastValueToZero()
        {
            Value(70);
            Reset();

            Assert.Equal(0d, ((SliderData)Logic.GetDataForVisu()).LastValue);
            // zero is now the last value, the same value is suppressed
            Assert.Empty(Value(0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(0)]
        public void Reset_AnyValue_Resets(object resetValue)
        {
            Value(70);

            Assert.Equal(0d, Convert.ToDouble(EmittedValue(Reset(resetValue))));
        }

        // ---------- min / max inputs ----------

        [Fact]
        public void MinInput_ChangesLowerBound_WithoutOutput()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMin), Dispatchable, 20));

            Assert.Equal(20d, EmittedValue(Value(10)));
            Assert.Equal(20d, ((SliderData)Logic.GetDataForVisu()).MinValue);
        }

        [Fact]
        public void MaxInput_ChangesUpperBound_WithoutOutput()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMax), Dispatchable, "50"));

            Assert.Equal(50d, EmittedValue(Value(80)));
            Assert.Equal(50d, ((SliderData)Logic.GetDataForVisu()).MaxValue);
        }

        [Fact]
        public void MinAndMaxInput_ValueInsideRange_IsNotChanged()
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMin), Dispatchable, -10);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMax), Dispatchable, 10);

            Assert.Equal(-5d, EmittedValue(Value(-5)));
            Assert.Equal(10d, EmittedValue(Value(11)));
            Assert.Equal(-10d, EmittedValue(Value(-11)));
        }

        [Fact]
        public async Task Parameters_AreReadOnStart()
        {
            GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMinParam).Value = 10d;
            GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValueMaxParam).Value = 20d;
            await Logic.Start();

            Assert.Equal(10d, EmittedValue(Value(0)));
            Assert.Equal(20d, EmittedValue(Value(30)));
            Assert.Equal(15d, EmittedValue(Value(15)));
        }

        [Fact]
        public async Task LogicInputChanged_DispatchesClampedValue()
        {
            await Context.Dispatcher.ClearValues();

            LogicInputChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValue), 250);

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, SliderLogicFactory.RuleOutput, 100d));
        }
    }
}
