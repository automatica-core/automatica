using P3.Logic.Operations.Climate.MixingValve;
using Xunit;
using F = P3.Logic.Operations.Climate.MixingValve.MixingValveLogicFactory;

namespace P3.Logic.Operations.Tests.Climate
{
    public class MixingValveTests : ClimateTest<MixingValveLogicFactory>
    {
        private MixingValveLogic Valve => (MixingValveLogic)Logic;

        [Fact]
        public void TooCold_OpensByTheTravelTime()
        {
            Send(F.InTarget, 40);
            Send(F.InCurrent, 30);

            // first sample: P part only, 2 % per K * 10 K = 20 %
            Run(1);
            Assert.Equal(20, Num(F.OutValve));
            Assert.True(Flag(F.OutOpen));
            Assert.False(Flag(F.OutClose));

            // 150 s travel time: 20 % take 30 s
            Run(30);
            Assert.False(Flag(F.OutOpen));
            Assert.Equal(20, Valve.Position, 6);
        }

        [Fact]
        public void IntegralPart_GrowsPerSample()
        {
            Send(F.InTarget, 40);
            Send(F.InCurrent, 30);
            Run(1);
            Run(40);
            // + 0.03 % per K and s * 10 K * 40 s
            Assert.Equal(32, Num(F.OutValve), 6);
        }

        [Fact]
        public void TooWarm_Closes_InvertedModeOpens()
        {
            Send(F.InTarget, 40);
            Send(F.InCurrent, 30);
            Run(1);
            Run(40);
            Send(F.InCurrent, 50);
            Run(40);
            Assert.True(Num(F.OutValve) < 32);

            SetParam(F.ParamMode, "1");
            Run(40);
            Run(40);
            Assert.True(Num(F.OutValve) > 32);
        }

        [Fact]
        public void Off_ClosesWithCalibrationRun()
        {
            Send(F.InOff, true);
            Run(1);
            Assert.True(Flag(F.OutClose));
            Assert.Equal(0, Num(F.OutValve));

            // 20 % of 150 s calibration although the tracked position is already 0
            Run(28);
            Assert.True(Flag(F.OutClose));
            Run(3);
            Assert.False(Flag(F.OutClose));
        }

        [Fact]
        public void OffModeOpen_OpensFully()
        {
            SetParam(F.ParamOffMode, "1");
            Send(F.InOff, true);
            Run(1);
            Assert.Equal(100, Num(F.OutValve));
            Assert.True(Flag(F.OutOpen));
            Run(150 + 30 + 2);
            Assert.False(Flag(F.OutOpen));
            Assert.Equal(100, Valve.Position, 6);
        }

        [Fact]
        public void PositionLimits_AndInvertedAnalogOutput()
        {
            SetParam(F.ParamMaxPosition, 60);
            SetParam(F.ParamInvert, true);
            Send(F.InTarget, 70);
            Send(F.InCurrent, 20);
            Run(1);
            // 100 limited to 60, V inverted
            Assert.Equal(60, Valve.TargetPosition, 6);
            Assert.Equal(40, Num(F.OutValve));
        }

        [Fact]
        public void Error_AfterTenMinutesOfLargeDeviation()
        {
            Send(F.InTarget, 60);
            Send(F.InCurrent, 40);
            Run(600, 10);
            Assert.False(Flag(F.OutError));
            Run(20, 10);
            Assert.True(Flag(F.OutError));

            Send(F.InCurrent, 58);
            Run(1);
            Assert.False(Flag(F.OutError));
        }

        [Fact]
        public void DisablePeriphery_KeepsTheTarget()
        {
            Send(F.InCurrent, 30);
            Send(F.InTarget, 40);
            Send(F.InDisablePeriphery, true);
            Send(F.InTarget, 80);
            Run(1);
            Assert.Equal(20, Num(F.OutValve));

            // released: the last target applies
            Send(F.InDisablePeriphery, false);
            Run(40);
            Assert.True(Num(F.OutValve) > 60);
        }
    }
}
