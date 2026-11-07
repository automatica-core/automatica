using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Ceiling;
using P3.Logic.Math.BasicOperations.Floor;
using P3.Logic.Math.BasicOperations.Modulo;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests
{
    public class CeilingEdgeCaseTests : LogicTest<CeilingLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(CeilingLogicFactory.RuleInput1), Dispatchable, value);

        [Theory]
        [InlineData(1.1, 2.0)]
        [InlineData(1.0, 1.0)]
        [InlineData(1.0000001, 2.0)]
        [InlineData(-1.1, -1.0)]
        [InlineData(-0.5, -0.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(1e300, 1e300)]
        public void InputValueChanged_Number_EmitsCeiling(double input, double expected)
        {
            var single = Assert.Single(Set(input));

            Assert.Equal(expected, (double)single.Value);
            Assert.Equal(CeilingLogicFactory.RuleOutput, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_IntegerBoolAndString_AreConverted()
        {
            Assert.Equal(3.0, (double)Set(3)[0].Value);
            Assert.Equal(1.0, (double)Set(true)[0].Value);
            using (new CultureScope("de-DE"))
            {
                Assert.Equal(3.0, (double)Set("2,1")[0].Value);
            }
        }

        [Fact]
        public void InputValueChanged_NullBeforeAnyValue_EmitsCeilingOfZero()
        {
            Assert.Equal(0.0, (double)Assert.Single(Set(null)).Value);
        }

        [Fact]
        public void InputValueChanged_NullAfterValue_KeepsLastValue()
        {
            Set(4.2);

            Assert.Equal(5.0, (double)Assert.Single(Set(null)).Value);
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsLastValue()
        {
            Set(4.2);

            Assert.Empty(Set("abc"));
            Assert.Equal(5.0, (double)Set(null)[0].Value);
        }

        [Fact]
        public async Task Start_NoInput_DispatchesZero()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(0.0, StartValueTestHelper.OutputValue(this, CeilingLogicFactory.RuleOutput));
        }

        [Fact(Skip = "BUG: Ceiling Stop resets the input to 0, so a reload (Stop + Start) dispatches 0 instead of the ceiling of the last input - Addition/Subtract/Multiply/Division/Modulo keep their inputs (CeilingRule.cs:25)")]
        public async Task StopStart_KeepsLastInput()
        {
            Set(1.5);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(2.0, StartValueTestHelper.OutputValue(this, CeilingLogicFactory.RuleOutput));
        }

        [Fact(Skip = "BUG: Ceiling Start dispatches the raw input instead of Math.Ceiling(input) - a Restart (no Stop) dispatches 1.5 instead of 2 (CeilingRule.cs:30)")]
        public async Task Restart_DispatchesCeilingOfLastInput()
        {
            Set(1.5);

            await Context.Dispatcher.ClearValues();
            await Logic.Restart(RuleInstance);

            Assert.Equal(2.0, StartValueTestHelper.OutputValue(this, CeilingLogicFactory.RuleOutput));
        }
    }

    public class FloorEdgeCaseTests : LogicTest<FloorLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(FloorLogicFactory.RuleInput1), Dispatchable, value);

        [Theory]
        [InlineData(1.9, 1.0)]
        [InlineData(1.0, 1.0)]
        [InlineData(-1.1, -2.0)]
        [InlineData(-0.0000001, -1.0)]
        [InlineData(0.5, 0.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        public void InputValueChanged_Number_EmitsFloor(double input, double expected)
        {
            var single = Assert.Single(Set(input));

            Assert.Equal(expected, (double)single.Value);
            Assert.Equal(FloorLogicFactory.RuleOutput, single.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_IntegerBoolAndString_AreConverted()
        {
            Assert.Equal(3.0, (double)Set(3)[0].Value);
            Assert.Equal(0.0, (double)Set(false)[0].Value);
            using (new CultureScope("en-US"))
            {
                Assert.Equal(2.0, (double)Set("2.9")[0].Value);
            }
        }

        [Fact]
        public void InputValueChanged_NullAfterValue_KeepsLastValue()
        {
            Set(4.8);

            Assert.Equal(4.0, (double)Assert.Single(Set(null)).Value);
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsLastValue()
        {
            Set(4.8);

            Assert.Empty(Set("abc"));
            Assert.Equal(4.0, (double)Set(null)[0].Value);
        }

        [Fact]
        public async Task Start_NoInput_DispatchesZero()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(0.0, StartValueTestHelper.OutputValue(this, FloorLogicFactory.RuleOutput));
        }

        [Fact(Skip = "BUG: Floor Stop resets the input to 0, so a reload (Stop + Start) dispatches 0 instead of the floor of the last input - Addition/Subtract/Multiply/Division/Modulo keep their inputs (FloorRule.cs:25)")]
        public async Task StopStart_KeepsLastInput()
        {
            Set(2.5);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(2.0, StartValueTestHelper.OutputValue(this, FloorLogicFactory.RuleOutput));
        }

        [Fact(Skip = "BUG: Floor Start dispatches the raw input instead of Math.Floor(input) - a Restart (no Stop) dispatches 2.5 instead of 2 (FloorRule.cs:30)")]
        public async Task Restart_DispatchesFloorOfLastInput()
        {
            Set(2.5);

            await Context.Dispatcher.ClearValues();
            await Logic.Restart(RuleInstance);

            Assert.Equal(2.0, StartValueTestHelper.OutputValue(this, FloorLogicFactory.RuleOutput));
        }
    }

    public class ModuloEdgeCaseTests : LogicTest<ModuloLogicFactory>
    {
        private IList<ILogicOutputChanged> Set(Guid input, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(input), Dispatchable, value);

        [Theory]
        [InlineData(10, 3, 1.0, 1)]
        [InlineData(-10, 3, -1.0, -1)]        // sign follows the dividend
        [InlineData(10, -3, 1.0, 1)]
        [InlineData(0, 5, 0.0, 0)]
        [InlineData(3, 5, 3.0, 3)]
        [InlineData(5.5, 2, 1.5, 2)]          // O2 is Convert.ToInt32 = banker's rounding (1.5 -> 2)
        [InlineData(2.5, 10, 2.5, 2)]         // banker's rounding (2.5 -> 2)
        [InlineData(7.6, 10, 7.6, 8)]         // rounded, not truncated
        [InlineData(2, double.PositiveInfinity, 2.0, 2)]
        [InlineData(10, 0, double.NaN, null)]
        [InlineData(double.PositiveInfinity, 2, double.NaN, null)]
        [InlineData(double.NaN, 2, double.NaN, null)]
        [InlineData(3e9, 4e9, 3e9, null)]     // bigger than int.MaxValue
        [InlineData(-3e9, 4e9, -3e9, null)]   // smaller than int.MinValue
        public void InputValueChanged_BothInputs_EmitsRemainderAndIntegerRemainder(double i1, double i2, double o1, object o2)
        {
            Set(ModuloLogicFactory.RuleInput1, i1);
            var result = Set(ModuloLogicFactory.RuleInput2, i2);

            Assert.Equal(2, result.Count);
            Assert.Equal(o1, (double)result[0].Value, 10);
            Assert.Equal(o2, result[1].Value);
            Assert.Equal(ModuloLogicFactory.RuleOutput1, result[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
            Assert.Equal(ModuloLogicFactory.RuleOutput2, result[1].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Fact]
        public void InputValueChanged_IntegerOutputIsBoxedInt()
        {
            Set(ModuloLogicFactory.RuleInput1, 10);

            Assert.IsType<int>(Set(ModuloLogicFactory.RuleInput2, 4)[1].Value);
        }

        [Fact]
        public void InputValueChanged_OnlyDivisorKnown_BothOutputsNull()
        {
            var result = Set(ModuloLogicFactory.RuleInput2, 3);

            Assert.Equal(2, result.Count);
            Assert.Null(result[0].Value);
            Assert.Null(result[1].Value);
        }

        [Fact]
        public void InputValueChanged_NullAfterBothKnown_KeepsLastValues()
        {
            Set(ModuloLogicFactory.RuleInput1, 10);
            Set(ModuloLogicFactory.RuleInput2, 4);

            var result = Set(ModuloLogicFactory.RuleInput1, null);

            Assert.Equal(2.0, result[0].Value);
            Assert.Equal(2, result[1].Value);
        }

        [Fact]
        public void InputValueChanged_DivisorBackFromZero_RecoversIntegerOutput()
        {
            Set(ModuloLogicFactory.RuleInput1, 10);
            Assert.Null(Set(ModuloLogicFactory.RuleInput2, 0)[1].Value);

            var result = Set(ModuloLogicFactory.RuleInput2, 6);

            Assert.Equal(4.0, result[0].Value);
            Assert.Equal(4, result[1].Value);
        }

        [Fact]
        public void InputValueChanged_UnparsableString_EmitsNothingAndKeepsInputs()
        {
            Set(ModuloLogicFactory.RuleInput1, 10);
            Set(ModuloLogicFactory.RuleInput2, 4);

            Assert.Empty(Set(ModuloLogicFactory.RuleInput2, "four"));
            Assert.Equal(1.0, Set(ModuloLogicFactory.RuleInput1, 9)[0].Value);
        }

        [Fact]
        public async Task Start_NoInputsKnown_DispatchesNothing()
        {
            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            var outputs = RuleInstance.RuleInterfaceInstance
                .Where(a => a.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput1 || a.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput2)
                .Select(a => a.ObjId)
                .ToList();
            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);

            Assert.DoesNotContain(values.Keys, k => outputs.Contains(k));
        }

        [Fact]
        public async Task StopStart_AfterDivisionByZero_DispatchesNaNAndNull()
        {
            Set(ModuloLogicFactory.RuleInput1, 10);
            Set(ModuloLogicFactory.RuleInput2, 0);

            await Context.Dispatcher.ClearValues();
            await Logic.Stop();
            await Logic.Start();

            Assert.Equal(double.NaN, StartValueTestHelper.OutputValue(this, ModuloLogicFactory.RuleOutput1));
            Assert.Null(StartValueTestHelper.OutputValue(this, ModuloLogicFactory.RuleOutput2));
        }
    }
}
