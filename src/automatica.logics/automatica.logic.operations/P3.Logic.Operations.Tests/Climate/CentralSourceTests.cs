using P3.Logic.Operations.Climate.Common;
using P3.Logic.Operations.Climate.HeatingCoolingController;
using P3.Logic.Operations.Climate.Hvac;
using Xunit;
using H = P3.Logic.Operations.Climate.HeatingCoolingController.HeatingCoolingControllerLogicFactory;
using V = P3.Logic.Operations.Climate.Hvac.HvacControllerLogicFactory;

namespace P3.Logic.Operations.Tests.Climate
{
    public class HeatingCoolingControllerTests : ClimateTest<HeatingCoolingControllerLogicFactory>
    {
        [Fact]
        public void StartsAtTheAverageThreshold()
        {
            Send(H.InDemands[0], 40);
            Send(H.InDemands[1], 10);
            Run(1);
            Assert.False(Flag(H.OutHeating));
            Assert.Equal(25, Num(H.OutHeatingLoad));
            Assert.Equal(3, Num(H.OutRoomSourceMode));

            Send(H.InDemands[1], 30);
            Run(1);
            Assert.True(Flag(H.OutHeating));
            Assert.False(Flag(H.OutCooling));
            Assert.Equal(1, Num(H.OutRoomSourceMode));
            Assert.True(Flag(H.OutFan));
        }

        [Fact]
        public void KeepsRunningWhileThereIsLoad_MinRuntime_RestartDelay()
        {
            SetParam(H.ParamMinRuntime, 10);
            Send(H.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(H.OutHeating));

            // below the threshold but still a load: keeps running
            Send(H.InDemands[0], 5);
            Run(60);
            Assert.True(Flag(H.OutHeating));

            Send(H.InDemands[0], 0);
            Run(530);
            Assert.True(Flag(H.OutHeating));
            Run(10);
            Assert.False(Flag(H.OutHeating));

            // restart delay of 3 minutes
            Send(H.InDemands[0], 50);
            Run(175);
            Assert.False(Flag(H.OutHeating));
            Run(5);
            Assert.True(Flag(H.OutHeating));
        }

        [Fact]
        public void ModeChange_GoesThroughOff()
        {
            Send(H.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(H.OutHeating));

            Send(H.InDemands[0], -50);
            Run(1);
            Assert.False(Flag(H.OutHeating));
            Assert.False(Flag(H.OutCooling));
            Run(180);
            Assert.True(Flag(H.OutCooling));
            Assert.Equal(2, Num(H.OutRoomSourceMode));
            Assert.Equal(50, Num(H.OutCoolingLoad));
        }

        [Fact]
        public void SwitchingValve_MovesFirst()
        {
            SetParam(H.ParamValveDelay, 30);
            Send(H.InDemands[0], -50);
            Run(1);
            Assert.True(Flag(H.OutSwitchingValve));
            Assert.False(Flag(H.OutCooling));
            Run(29);
            Assert.False(Flag(H.OutCooling));
            Run(1);
            Assert.True(Flag(H.OutCooling));
        }

        [Fact]
        public void Stage2_AfterTimeOrBoost()
        {
            SetParam(H.ParamStage2, 1);
            Send(H.InDemands[0], 50);
            Run(1);
            Assert.False(Flag(H.OutHeating2));
            Run(60);
            Assert.True(Flag(H.OutHeating2));

            SetParam(H.ParamStage2, 60);
            Assert.False(Flag(H.OutHeating2));
            Assert.False(Flag(H.OutAdditionalHeating));
            Send(H.InBoost, true);
            Assert.True(Flag(H.OutHeating2));
            Assert.True(Flag(H.OutAdditionalHeating));
        }

        [Fact]
        public void FanOverrun()
        {
            Send(H.InDemands[0], 50);
            Run(1);
            Send(H.InDemands[0], 0);
            Run(1);
            Assert.False(Flag(H.OutHeating));
            Assert.True(Flag(H.OutFan));
            Run(118);
            Assert.True(Flag(H.OutFan));
            Run(2);
            Assert.False(Flag(H.OutFan));
        }

        [Fact]
        public void ServiceMode_AndOff()
        {
            Send(H.InService, 2);
            Run(1);
            Assert.True(Flag(H.OutCooling));

            Send(H.InService, 3);
            Run(1);
            Assert.False(Flag(H.OutCooling));

            Send(H.InManualHeating, true);
            Send(H.InService, 0);
            Run(1);
            Assert.True(Flag(H.OutHeating));

            Send(H.InOff, true);
            Run(1);
            Assert.False(Flag(H.OutHeating));
            Assert.False(Flag(H.OutFan));
        }

        [Fact]
        public void OutdoorLimits_AndHeatingOnlyMode()
        {
            Send(H.InOutdoor, 20);
            Send(H.InDemands[0], 50);
            Run(1);
            Assert.False(Flag(H.OutHeating));

            Send(H.InOutdoor, 10);
            Run(1);
            Assert.True(Flag(H.OutHeating));

            // cooling only above LimC
            Send(H.InDemands[0], -50);
            Run(200);
            Assert.False(Flag(H.OutCooling));
            Send(H.InOutdoor, 25);
            Run(1);
            Assert.True(Flag(H.OutCooling));

            SetParam(H.ParamMode, "2");
            Run(200);
            Assert.False(Flag(H.OutCooling));
        }

        [Fact]
        public void ExcessEnergy_ForwardedWhileTheModeRuns()
        {
            Send(H.InExcessHeating, true);
            Assert.False(Flag(H.OutExcessHeating));
            Send(H.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(H.OutExcessHeating));
        }
    }

    public class HvacControllerTests : ClimateTest<HvacControllerLogicFactory>
    {
        [Fact]
        public void Conventional_TwoStages()
        {
            Send(V.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(V.OutW1));
            Assert.False(Flag(V.OutW2));
            Assert.False(Flag(V.OutY));
            Assert.True(Flag(V.OutFan));
            Assert.Equal(1, Num(V.OutHeatingCoolingMode));
            Assert.False(Flag(V.OutReversingValve));

            Run(300);
            Assert.True(Flag(V.OutW2));
        }

        [Fact]
        public void Stage2_ByDemand()
        {
            Send(V.InDemands[0], 95);
            Send(V.InDemands[1], 5);
            Run(1);
            Assert.True(Flag(V.OutW2));
        }

        [Fact]
        public void DualFuel_FurnaceReplacesTheHeatPump()
        {
            SetParam(V.ParamType, "1");
            Send(V.InOutdoor, 5);
            Send(V.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(V.OutY));
            Assert.False(Flag(V.OutW1));

            Send(V.InBoost, true);
            Assert.False(Flag(V.OutY));
            Assert.True(Flag(V.OutW1));

            Send(V.InBoost, false);
            Send(V.InOutdoor, -5);
            Assert.False(Flag(V.OutY));
            Assert.True(Flag(V.OutW1));
        }

        [Fact]
        public void ElectricBackup_AddsW1_EmergencyHeat()
        {
            SetParam(V.ParamType, "2");
            Send(V.InOutdoor, 5);
            Send(V.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(V.OutY));
            Assert.False(Flag(V.OutW1));

            Send(V.InBoost, true);
            Assert.True(Flag(V.OutY));
            Assert.True(Flag(V.OutW1));

            Send(V.InEmergencyHeat, true);
            Assert.True(Flag(V.OutEmergency));
            Assert.False(Flag(V.OutY));
            Assert.False(Flag(V.OutW1));
        }

        [Fact]
        public void HeatPumpCooling_ReversingValve_AndProtection()
        {
            SetParam(V.ParamType, "1");
            SetParam(V.ParamMinOutdoorCooling, 5);
            Send(V.InOutdoor, 10);
            Send(V.InDemands[0], -50);
            Run(1);
            // below Pmic 12
            Assert.False(Flag(V.OutY));

            Send(V.InOutdoor, 25);
            Run(1);
            Assert.True(Flag(V.OutY));
            Assert.True(Flag(V.OutReversingValve));
            Assert.Equal(-1, Num(V.OutHeatingCoolingMode));
            Assert.Equal(2, Num(V.OutRoomSourceMode));

            SetParam(V.ParamDirection, true);
            Assert.False(Flag(V.OutReversingValve));
        }

        [Fact]
        public void MinimumOffTime_AndModeInput()
        {
            Send(V.InDemands[0], 50);
            Run(1);
            Send(V.InMode, 0);
            Run(1);
            Assert.False(Flag(V.OutW1));

            Send(V.InMode, 1);
            Run(298);
            Assert.False(Flag(V.OutW1));
            Run(2);
            Assert.True(Flag(V.OutW1));
        }

        [Fact]
        public void Humidifier_WithHysteresisWhileHeating()
        {
            Send(V.InHumidity, 40);
            Run(1);
            Assert.False(Flag(V.OutHumidifier));

            Send(V.InDemands[0], 50);
            Run(1);
            Assert.True(Flag(V.OutHumidifier));
            Send(V.InHumidity, 46);
            Assert.True(Flag(V.OutHumidifier));
            Send(V.InHumidity, 47);
            Assert.False(Flag(V.OutHumidifier));
        }
    }
}
