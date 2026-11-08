using P3.Logic.Operations.Climate.Common;
using P3.Logic.Operations.Climate.Dewpoint;
using P3.Logic.Operations.Climate.HeatingCurve;
using P3.Logic.Operations.Climate.PositionControllers;
using Xunit;

namespace P3.Logic.Operations.Tests.Climate
{
    public class DewpointTests : ClimateTest<DewpointLogicFactory>
    {
        [Fact]
        public void MagnusFormula()
        {
            Send(DewpointLogicFactory.InTemperature, 20);
            Assert.Null(Out(DewpointLogicFactory.OutDewpoint));

            Send(DewpointLogicFactory.InHumidity, 50);
            Assert.Equal(9.26, Num(DewpointLogicFactory.OutDewpoint), 2);

            Send(DewpointLogicFactory.InHumidity, 100);
            Assert.Equal(20, Num(DewpointLogicFactory.OutDewpoint), 2);
        }

        [Fact]
        public void BelowZero_UsesIceCoefficients_AndOffset()
        {
            SetParam(DewpointLogicFactory.ParamOffset, 1.5);
            Send(DewpointLogicFactory.InHumidity, 80);
            Send(DewpointLogicFactory.InTemperature, -10);
            // k2 = 22.46, k3 = 272.62
            var expected = HeatingCurveMath.DewPoint(-10, 80).Value + 1.5;
            Assert.Equal(expected, Num(DewpointLogicFactory.OutDewpoint), 2);
            Assert.InRange(expected - 1.5, -12.5, -12.0);
        }

        [Fact]
        public void OutOfRange_SendsNothing()
        {
            Send(DewpointLogicFactory.InHumidity, 50);
            Assert.Empty(Send(DewpointLogicFactory.InTemperature, 70));
            Assert.Empty(Send(DewpointLogicFactory.InTemperature, -70));
        }
    }

    public class HeatingCurveTests : ClimateTest<HeatingCurveLogicFactory>
    {
        [Fact]
        public void WikipediaCurve_LimitedToMinMax()
        {
            Send(HeatingCurveLogicFactory.InOutdoor, 0);
            // 20 + 1.3 * 20 * (1.4347 - 0.42 + 0.09916)
            Assert.Equal(49.0, Num(HeatingCurveLogicFactory.OutFlow), 1);
            Assert.False(Flag(HeatingCurveLogicFactory.OutInvalid));

            Send(HeatingCurveLogicFactory.InOutdoor, -20);
            Assert.Equal(65, Num(HeatingCurveLogicFactory.OutFlow));
            Assert.True(Flag(HeatingCurveLogicFactory.OutInvalid));

            Send(HeatingCurveLogicFactory.InOutdoor, 25);
            Assert.Equal(20, Num(HeatingCurveLogicFactory.OutFlow));
            Assert.False(Flag(HeatingCurveLogicFactory.OutInvalid));
        }

        [Fact]
        public void SetpointLevelAndDisable()
        {
            SetParam(HeatingCurveLogicFactory.ParamSlope, 0.5);
            SetParam(HeatingCurveLogicFactory.ParamOffset, 2);
            Send(HeatingCurveLogicFactory.InOutdoor, 0);
            Send(HeatingCurveLogicFactory.InTarget, 22);
            // 22 + 2 + 0.5 * 22 * (1.4347 - 0.462 + 0.11998)
            Assert.Equal(36.0, Num(HeatingCurveLogicFactory.OutFlow), 1);

            Send(HeatingCurveLogicFactory.InDisable, true);
            Assert.Equal(33.1, Num(HeatingCurveLogicFactory.OutFlow), 1);
        }

        [Fact]
        public void ParameterChange_AppliesAtOnce()
        {
            Send(HeatingCurveLogicFactory.InOutdoor, 0);
            SetParam(HeatingCurveLogicFactory.ParamMax, 40);
            Assert.Equal(40, Num(HeatingCurveLogicFactory.OutFlow));
        }
    }

    public class TwoPositionTests : ClimateTest<TwoPositionLogicFactory>
    {
        [Fact]
        public void Normal_OnBelowOffAbove_HoldsInBetween()
        {
            SetParam(TwoPositionLogicFactory.ParamSetpoint, 20);
            SetParam(TwoPositionLogicFactory.ParamHysteresis, 1);

            Send(TwoPositionLogicFactory.InValue, 19.4);
            Assert.True(Flag(TwoPositionLogicFactory.OutOutput));
            Send(TwoPositionLogicFactory.InValue, 20.4);
            Assert.True(Flag(TwoPositionLogicFactory.OutOutput));
            Send(TwoPositionLogicFactory.InValue, 20.6);
            Assert.False(Flag(TwoPositionLogicFactory.OutOutput));
            Send(TwoPositionLogicFactory.InValue, 19.6);
            Assert.False(Flag(TwoPositionLogicFactory.OutOutput));
        }

        [Fact]
        public void Inverted_AndOffLocks()
        {
            SetParam(TwoPositionLogicFactory.ParamSetpoint, 20);
            SetParam(TwoPositionLogicFactory.ParamInverted, true);
            Send(TwoPositionLogicFactory.InValue, 21);
            Assert.True(Flag(TwoPositionLogicFactory.OutOutput));
            Send(TwoPositionLogicFactory.InValue, 19);
            Assert.False(Flag(TwoPositionLogicFactory.OutOutput));

            Send(TwoPositionLogicFactory.InValue, 22);
            Send(TwoPositionLogicFactory.InOff, true);
            Assert.False(Flag(TwoPositionLogicFactory.OutOutput));
            Send(TwoPositionLogicFactory.InValue, 23);
            Assert.False(Flag(TwoPositionLogicFactory.OutOutput));
        }
    }

    public class ThreePositionTests : ClimateTest<ThreePositionLogicFactory>
    {
        [Fact]
        public void TwoOutputs_AroundTheBand()
        {
            Send(ThreePositionLogicFactory.InValue, 2);
            Assert.True(Flag(ThreePositionLogicFactory.OutOutput1));
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput2));

            Send(ThreePositionLogicFactory.InValue, 5);
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput1));
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput2));

            Send(ThreePositionLogicFactory.InValue, 8);
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput1));
            Assert.True(Flag(ThreePositionLogicFactory.OutOutput2));
        }

        [Fact]
        public void Hysteresis_AndSwappedSetpoints()
        {
            SetParam(ThreePositionLogicFactory.ParamSetpoint1, 7);
            SetParam(ThreePositionLogicFactory.ParamSetpoint2, 3);
            SetParam(ThreePositionLogicFactory.ParamHysteresis, 1);

            Send(ThreePositionLogicFactory.InValue, 2.9);
            Assert.True(Flag(ThreePositionLogicFactory.OutOutput1));
            Send(ThreePositionLogicFactory.InValue, 3.5);
            Assert.True(Flag(ThreePositionLogicFactory.OutOutput1));
            Send(ThreePositionLogicFactory.InValue, 4);
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput1));

            Send(ThreePositionLogicFactory.InOff, true);
            Send(ThreePositionLogicFactory.InValue, 10);
            Assert.False(Flag(ThreePositionLogicFactory.OutOutput2));
        }
    }
}
