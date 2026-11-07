using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Counter;
using P3.Logic.Math.BasicOperations.Formula;
using P3.Logic.Math.BasicOperations.MinMax;
using P3.Logic.Math.BasicOperations.Scaler;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.NewBlocks
{
    internal static class Outputs
    {
        public static object Of(this IList<ILogicOutputChanged> changes, Guid template) =>
            changes.Single(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == template).Value;
    }

    public class FormulaRuleTests : LogicTest<FormulaLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void DefaultFormula_PassesI1()
        {
            Assert.Equal(4.5, Send(FormulaLogicFactory.RuleInput1, 4.5)[0].Value);
        }

        [Fact]
        public void Formula_UsesAllInputs()
        {
            Send(FormulaLogicFactory.RuleParamFormula, "if(I1 > I2, I3, I4)");
            Assert.Equal(0d, Send(FormulaLogicFactory.RuleInput1, 5)[0].Value);
            Send(FormulaLogicFactory.RuleInput3, "30");
            Assert.Equal(30d, Send(FormulaLogicFactory.RuleInput4, 40)[0].Value);
            Assert.Equal(40d, Send(FormulaLogicFactory.RuleInput2, 6)[0].Value);
        }

        [Fact]
        public void InvalidFormula_SendsNothing()
        {
            Send(FormulaLogicFactory.RuleParamFormula, "I1 +");
            Assert.Empty(Send(FormulaLogicFactory.RuleInput1, 1));

            Send(FormulaLogicFactory.RuleParamFormula, "I1 * 2");
            Assert.Equal(2d, Send(FormulaLogicFactory.RuleInput1, 1)[0].Value);
        }

        [Fact]
        public void NonFiniteResult_SendsNothing()
        {
            Send(FormulaLogicFactory.RuleParamFormula, "1 / I1");
            Assert.Empty(Send(FormulaLogicFactory.RuleInput1, 0));
            Assert.Equal(0.5, Send(FormulaLogicFactory.RuleInput1, 2)[0].Value);
        }
    }

    public class MinMaxTests : LogicTest<MinMaxLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void OnlyInputsWithValue_Count()
        {
            var r = Send(MinMaxLogicFactory.RuleInput2, 10);
            Assert.Equal(10d, r.Of(MinMaxLogicFactory.RuleMin));
            Assert.Equal(10d, r.Of(MinMaxLogicFactory.RuleMax));
            Assert.Equal(10d, r.Of(MinMaxLogicFactory.RuleAverage));

            r = Send(MinMaxLogicFactory.RuleInput4, -2);
            Assert.Equal(-2d, r.Of(MinMaxLogicFactory.RuleMin));
            Assert.Equal(10d, r.Of(MinMaxLogicFactory.RuleMax));
            Assert.Equal(4d, r.Of(MinMaxLogicFactory.RuleAverage));
        }

        [Fact]
        public void NullValue_RemovesTheInput()
        {
            Send(MinMaxLogicFactory.RuleInput1, 1);
            Send(MinMaxLogicFactory.RuleInput2, 3);
            var r = Send(MinMaxLogicFactory.RuleInput1, null);
            Assert.Equal(3d, r.Of(MinMaxLogicFactory.RuleMin));

            Assert.Empty(Send(MinMaxLogicFactory.RuleInput2, null));
        }
    }

    public class UpDownCounterTests : LogicTest<UpDownCounterLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void CountsUpDownAndResets()
        {
            Assert.Equal(1d, Send(UpDownCounterLogicFactory.RuleUp, true).Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Empty(Send(UpDownCounterLogicFactory.RuleUp, false));
            Assert.Equal(2d, Send(UpDownCounterLogicFactory.RuleUp, 1).Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Equal(1d, Send(UpDownCounterLogicFactory.RuleDown, 1).Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Equal(0d, Send(UpDownCounterLogicFactory.RuleReset, 1).Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Equal(-1d, Send(UpDownCounterLogicFactory.RuleDown, 1).Of(UpDownCounterLogicFactory.RuleOutput));
        }

        [Fact]
        public void StepAndLimit()
        {
            Send(UpDownCounterLogicFactory.RuleParamStep, 2.5);
            Send(UpDownCounterLogicFactory.RuleParamLimit, 5);

            var r = Send(UpDownCounterLogicFactory.RuleUp, true);
            Assert.Equal(2.5, r.Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Equal(false, r.Of(UpDownCounterLogicFactory.RuleReached));

            r = Send(UpDownCounterLogicFactory.RuleUp, true);
            Assert.Equal(5d, r.Of(UpDownCounterLogicFactory.RuleOutput));
            Assert.Equal(true, r.Of(UpDownCounterLogicFactory.RuleReached));

            r = Send(UpDownCounterLogicFactory.RuleDown, true);
            Assert.Equal(false, r.Of(UpDownCounterLogicFactory.RuleReached));
        }

        [Fact]
        public void LimitZero_NeverReached()
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(false, Send(UpDownCounterLogicFactory.RuleUp, true).Of(UpDownCounterLogicFactory.RuleReached));
            }
        }
    }

    public class ScalerTests : LogicTest<ScalerLogicFactory>
    {
        private void Param(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object Send(object value)
        {
            var r = Logic.ValueChanged(GetLogicInterfaceByTemplate(ScalerLogicFactory.RuleInput), Dispatchable, value);
            return r.Count == 0 ? null : r[0].Value;
        }

        [Fact]
        public void Default_MapsZeroTenToZeroHundred()
        {
            Assert.Equal(0d, Send(0));
            Assert.Equal(45d, Send(4.5));
            Assert.Equal(100d, Send(10));
            Assert.Equal(100d, Send(12));   // limited
            Assert.Equal(0d, Send(-1));
        }

        [Fact]
        public void InvertedRange_AndNoLimit()
        {
            Param(ScalerLogicFactory.RuleParamInMin, 4);
            Param(ScalerLogicFactory.RuleParamInMax, 20);
            Param(ScalerLogicFactory.RuleParamOutMin, 100);
            Param(ScalerLogicFactory.RuleParamOutMax, 0);
            Assert.Equal(50d, Send(12));
            Assert.Equal(0d, Send(24));

            Param(ScalerLogicFactory.RuleParamLimit, false);
            Assert.Equal(-25d, Send(24));
        }

        [Fact]
        public void EmptyInputRange_SendsNothing()
        {
            Param(ScalerLogicFactory.RuleParamInMax, 0);
            Assert.Null(Send(5));
        }
    }
}
