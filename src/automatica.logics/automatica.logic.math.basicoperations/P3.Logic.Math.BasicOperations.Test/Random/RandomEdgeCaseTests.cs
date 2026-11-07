using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Random;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.Random
{
    /// <summary>
    /// Random values are only checked for ranges and properties, never for concrete values
    /// </summary>
    public class RandomEdgeCaseTests : LogicTest<RandomLogicFactory>
    {
        private IList<ILogicOutputChanged> Trigger(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputTrigger), Dispatchable, value);
        private IList<ILogicOutputChanged> Disable(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(RandomLogicFactory.RuleInputDisabled), Dispatchable, value);

        private void SetRange(int min, int max)
        {
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMin).Value = min;
            GetLogicInterfaceByTemplate(RandomLogicFactory.RuleParamMax).Value = max;
        }

        private double Draw() => (double)Assert.Single(Trigger(true)).Value;

        [Fact]
        public void Trigger_DefaultRange_IsZeroToHundredInclusive()
        {
            var rule = (RandomRule)Logic;

            Assert.Equal(0, rule.Min);
            Assert.Equal(100, rule.Max);
            for (var i = 0; i < 100; i++)
            {
                Assert.InRange(Draw(), 0, 100);
            }
        }

        [Fact]
        public void Trigger_ValuesAreWholeNumbers()
        {
            SetRange(-50, 50);

            for (var i = 0; i < 100; i++)
            {
                var value = Draw();
                Assert.Equal(System.Math.Floor(value), value);
            }
        }

        [Fact]
        public void Trigger_MinEqualsMax_AlwaysReturnsThatValue()
        {
            SetRange(42, 42);

            for (var i = 0; i < 20; i++)
            {
                Assert.Equal(42.0, Draw());
            }
        }

        [Fact]
        public void Trigger_MinBiggerThanMax_BoundsAreSwapped()
        {
            SetRange(10, 5);

            for (var i = 0; i < 100; i++)
            {
                Assert.InRange(Draw(), 5, 10);
            }
        }

        [Fact]
        public void Trigger_NegativeRange_StaysInRange()
        {
            SetRange(-3, -1);

            var seen = new HashSet<double>();
            for (var i = 0; i < 300; i++)
            {
                var value = Draw();
                Assert.InRange(value, -3, -1);
                seen.Add(value);
            }

            // 300 draws over 3 values: missing one has a probability of ~3 * (2/3)^300
            Assert.Equal(new HashSet<double> { -3, -2, -1 }, seen);
        }

        [Fact]
        public void Trigger_FullIntRange_DoesNotOverflow()
        {
            SetRange(int.MinValue, int.MaxValue);

            for (var i = 0; i < 50; i++)
            {
                Assert.InRange(Draw(), int.MinValue, int.MaxValue);
            }
        }

        [Fact]
        public void Trigger_RangeChangedAtRuntime_NextValueUsesNewRange()
        {
            SetRange(1, 1);
            Assert.Equal(1.0, Draw());

            SetRange(7, 7);

            Assert.Equal(7.0, Draw());
        }

        [Fact]
        public void Trigger_OutputIsOnOutputInterface()
        {
            Assert.Equal(RandomLogicFactory.RuleOutput, Assert.Single(Trigger(true)).Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(0.5)]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("1")]
        [InlineData("2.5")]
        public void Trigger_TruthyValue_GeneratesValue(object value)
        {
            Assert.Single(Trigger(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData(0.0)]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("abc")]
        [InlineData("")]
        public void Trigger_FalsyValue_GeneratesNothing(object value)
        {
            Assert.Empty(Trigger(value));
        }

        [Fact]
        public void Trigger_SameTruthyValueTwice_GeneratesTwice()
        {
            Assert.Single(Trigger(true));
            Assert.Single(Trigger(true));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(1)]
        [InlineData("true")]
        [InlineData("1")]
        public void Disabled_TruthyValue_BlocksTrigger(object value)
        {
            Assert.Empty(Disable(value));

            Assert.Empty(Trigger(true));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(0)]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData(null)]
        [InlineData("abc")]
        public void Disabled_FalsyValue_ReEnablesTrigger(object value)
        {
            Disable(true);
            Assert.Empty(Trigger(true));

            Assert.Empty(Disable(value));

            Assert.Single(Trigger(true));
        }
    }
}
