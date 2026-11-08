using P3.Logic.Operations.Climate.AirConditioning;
using P3.Logic.Operations.Climate.Common;
using P3.Logic.Operations.Climate.FanCoil;
using P3.Logic.Operations.Climate.FlowTemperature;
using Xunit;
using A = P3.Logic.Operations.Climate.AirConditioning.AcUnitLogicFactory;
using AC = P3.Logic.Operations.Climate.AirConditioning.AcCentralLogicFactory;
using FC = P3.Logic.Operations.Climate.FanCoil.FanCoilCentralLogicFactory;
using FT = P3.Logic.Operations.Climate.FlowTemperature.FlowTemperatureLogicFactory;

namespace P3.Logic.Operations.Tests.Climate
{
    public class FlowTemperatureTests : ClimateTest<FlowTemperatureLogicFactory>
    {
        [Fact]
        public void Heating_WorstRoomWins_WithPhaseIncrease()
        {
            Send(FT.InOutdoor, 10);
            Send(FT.InRoomTargets[0], 21);
            Send(FT.InRoomCurrents[0], 20);
            // 1 K below: target + gain * 1 K
            var room1 = HeatingCurveMath.Heating(22, 10, 0.5, 0);
            Assert.Equal(room1, Num(FT.OutFlow), 1);
            Assert.Equal(21, Num(FT.OutRoomTarget));
            Assert.Equal(room1 - HeatingCurveMath.Heating(21, 10, 0.5, 0), Num(FT.OutIncrease), 1);
            Assert.True(Flag(FT.OutPump));
            Assert.False(Flag(FT.OutError));
            Assert.Equal(room1 + 5, Num(FT.OutBuffer), 1);

            // 3 K below: + gain * 3 K + 2 K phase increase
            Send(FT.InRoomTargets[1], 22);
            Send(FT.InRoomCurrents[1], 19);
            Assert.Equal(HeatingCurveMath.Heating(27, 10, 0.5, 0), Num(FT.OutFlow), 1);
            Assert.Equal(22, Num(FT.OutRoomTarget));
        }

        [Fact]
        public void NoDemand_MinimumAndNoPump_ErrorWithoutOutdoor()
        {
            Send(FT.InRoomTargets[0], 21);
            Send(FT.InRoomCurrents[0], 22);
            Assert.Equal(20, Num(FT.OutFlow));
            Assert.False(Flag(FT.OutPump));
            Assert.True(Flag(FT.OutError));
        }

        [Fact]
        public void Valves_DecidePumpAndDemand()
        {
            Send(FT.InOutdoor, 0);
            Send(FT.InRoomTargets[0], 21);
            Send(FT.InRoomCurrents[0], 20);
            Send(FT.InRoomValves[0], 0);
            // valve closed: no demand although the room is too cold
            Assert.Equal(20, Num(FT.OutFlow));

            Send(FT.InRoomValves[0], 20);
            Assert.True(Num(FT.OutFlow) > 20);
            Assert.False(Flag(FT.OutPump));

            Send(FT.InRoomValves[1], 50);
            Assert.True(Flag(FT.OutPump));
            Assert.Equal(35, Num(FT.OutLoad));
        }

        [Fact]
        public void BoostStopAndBuffer()
        {
            Send(FT.InOutdoor, 0);
            Send(FT.InRoomTargets[0], 21);
            Send(FT.InRoomCurrents[0], 20);
            Send(FT.InBoost, true);
            Assert.Equal(40, Num(FT.OutFlow));

            Send(FT.InStop, true);
            Assert.Equal(20, Num(FT.OutFlow));
            Assert.Equal(20, Num(FT.OutBuffer));
            Assert.False(Flag(FT.OutPump));

            Send(FT.InStop, false);
            Send(FT.InBoost, false);
            var flow = Num(FT.OutFlow);
            Send(FT.InBuffer, flow + 4);
            Assert.False(Flag(FT.OutPump));
            Send(FT.InBuffer, flow + 6);
            Assert.True(Flag(FT.OutPump));
        }

        [Fact]
        public void Cooling_LowestFlowWins()
        {
            SetParam(FT.ParamMode, "1");
            SetParam(FT.ParamMin, 15);
            SetParam(FT.ParamMax, 22);
            Send(FT.InOutdoor, 30);
            Send(FT.InRoomTargets[0], 24);
            Send(FT.InRoomCurrents[0], 23);
            // no cooling demand: maximum
            Assert.Equal(22, Num(FT.OutFlow));

            Send(FT.InRoomCurrents[0], 26);
            // 24 - 2 K - 2 K phase = 20, 10 K below the outdoor temperature
            Assert.Equal(15, Num(FT.OutFlow));
            Assert.Equal(10, Num(FT.OutBuffer));
        }
    }

    public class AcUnitTests : ClimateTest<AcUnitLogicFactory>
    {
        [Fact]
        public void ToggleOnOff()
        {
            Send(A.InToggle, true);
            Assert.True(Flag(A.OutStatus));
            Send(A.InToggle, false);
            Send(A.InToggle, true);
            Assert.False(Flag(A.OutStatus));

            Send(A.InOn, true);
            Assert.True(Flag(A.OutStatus));
            Send(A.InOff, true);
            Assert.False(Flag(A.OutStatus));
            Send(A.InOff, false);
            Assert.False(Flag(A.OutStatus));
        }

        [Fact]
        public void AutoMode_SignedDemand()
        {
            Send(A.InOn, true);
            Send(A.InTarget, 22);
            Send(A.InCurrent, 20);
            Assert.Equal(2, Num(A.OutDemand));
            Send(A.InCurrent, 24.5);
            Assert.Equal(-2.5, Num(A.OutDemand));
            Send(A.InCurrent, 22);
            Assert.Equal(0, Num(A.OutDemand));

            // heat mode: at least 0.1
            Send(A.InMode, A.ModeHeat);
            Assert.Equal(0.1, Num(A.OutDemand));
            Assert.Equal(A.ModeHeat, (int)Num(A.OutMode));
        }

        [Fact]
        public void LocalPauses_AndCentralPause()
        {
            Send(A.InOn, true);
            Send(A.InTarget, 22);
            Send(A.InCurrent, 20);
            Send(A.InWindow, true);
            Assert.True(Flag(A.OutPaused));
            Assert.False(Flag(A.OutStatus));
            Assert.Equal(0, Num(A.OutDemand));
            Send(A.InWindow, false);

            // the central pause keeps the demand, otherwise the central could never switch over
            Send(A.InCentralPause, true);
            Assert.True(Flag(A.OutPaused));
            Assert.Equal(2, Num(A.OutDemand));
        }

        [Fact]
        public void PauseTimer_RunsAfterTheFallingEdge()
        {
            Send(A.InOn, true);
            Send(A.InPauseTimer, true);
            Send(A.InPauseTimer, false);
            Run(7199, 100);
            Assert.True(Flag(A.OutPaused));
            Run(2);
            Assert.False(Flag(A.OutPaused));
            Assert.True(Flag(A.OutStatus));
        }

        [Fact]
        public void RoomControllerMode_SwitchesTheUnit()
        {
            Send(A.InRoomMode, 1);
            Assert.True(Flag(A.OutStatus));
            Assert.Equal(A.ModeHeat, (int)Num(A.OutMode));
            Assert.Equal(1, Num(A.OutDemand));

            Send(A.InRoomMode, -1);
            Assert.Equal(A.ModeCool, (int)Num(A.OutMode));
            Assert.Equal(-1, Num(A.OutDemand));

            Send(A.InRoomMode, 0);
            Assert.False(Flag(A.OutStatus));
        }

        [Fact]
        public void SilentFan_AndTargetLimits()
        {
            Send(A.InFan, 5);
            Assert.Equal(5, Num(A.OutFan));
            Send(A.InSilent, true);
            Assert.Equal(2, Num(A.OutFan));

            Send(A.InTarget, 35);
            Assert.Equal(30, Num(A.OutTarget));
            Send(A.InTarget, 10);
            Assert.Equal(16, Num(A.OutTarget));
        }
    }

    public class AcCentralTests : ClimateTest<AcCentralLogicFactory>
    {
        [Fact]
        public void LargerDemandWins_OthersPaused()
        {
            Send(AC.InDemands[0], 2);
            Send(AC.InDemands[1], -1);
            Run(1);
            Assert.True(Flag(AC.OutHeating));
            Assert.False(Flag(AC.OutPauses[0]));
            Assert.True(Flag(AC.OutPauses[1]));
            Assert.False(Flag(AC.OutPauses[2]));
        }

        [Fact]
        public void ChangeoverAfterMinRuntimeAndRestartDelay()
        {
            Send(AC.InDemands[0], 2);
            Run(1);
            Send(AC.InDemands[0], 0);
            Send(AC.InDemands[1], -5);
            Run(599);
            Assert.True(Flag(AC.OutHeating));
            Assert.True(Flag(AC.OutPauses[1]));
            Run(1);
            Assert.False(Flag(AC.OutHeating));
            Run(180);
            Assert.True(Flag(AC.OutCooling));
            Assert.False(Flag(AC.OutPauses[1]));
        }

        [Fact]
        public void OutdoorLimit_PausesTheHeatingUnit_OffPausesAll()
        {
            Send(AC.InOutdoor, 25);
            Send(AC.InDemands[0], 2);
            Run(1);
            Assert.False(Flag(AC.OutHeating));
            Assert.True(Flag(AC.OutPauses[0]));

            Send(AC.InOff, true);
            Assert.True(Flag(AC.OutPauses[3]));
        }
    }

    public class FanCoilUnitTests : ClimateTest<FanCoilUnitLogicFactory>
    {
        private static readonly FanCoilIds I = FanCoilUnitLogicFactory.Ids;

        [Fact]
        public void Standalone_Heating_FanRateLimited()
        {
            Send(I.Target, 22);
            Send(I.Current, 21);
            Run(1);
            Assert.Equal(100, Num(I.OutHeating));
            Assert.Equal(0, Num(I.OutCooling));
            Assert.Equal(50.6, Num(I.OutDemand));
            Assert.True(Flag(I.OutStatus));

            // PI wants 50.6 %, the fan moves once a minute by 20 %
            Assert.Equal(0, Num(I.OutFan));
            Run(59);
            Assert.Equal(20, Num(I.OutFan));
            Assert.Equal(1, Num(I.OutFanStep));
            Run(60);
            Assert.Equal(40, Num(I.OutFan));
            Run(60);
            Assert.InRange(Num(I.OutFan), 51, 52);

            // target exceeded, inside the 0.5 K start band: stops, no cooling
            Send(I.Current, 22.3);
            Run(61);
            Assert.Equal(0, Num(I.OutHeating));
            Assert.Equal(0, Num(I.OutCooling));
            Assert.Equal(0, Num(I.OutDemand));

            // 1 K too warm: cools
            Send(I.Current, 23);
            Run(1);
            Assert.Equal(100, Num(I.OutCooling));
        }

        [Fact]
        public void Standalone_Cooling_StartsHalfAKelvinOff()
        {
            Send(I.Target, 22);
            Send(I.Current, 22.4);
            Run(1);
            Assert.Equal(0, Num(I.OutCooling));

            Send(I.Current, 22.6);
            Run(1);
            Assert.Equal(100, Num(I.OutCooling));
            Assert.True(Num(I.OutDemand) < 0);
        }

        [Fact]
        public void RoomControllerMode_AndAvailability()
        {
            Send(I.Target, 22);
            Send(I.Current, 21);
            Send(I.RoomMode, 0);
            Run(1);
            Assert.Equal(0, Num(I.OutHeating));

            Send(I.RoomMode, 1);
            Run(1);
            Assert.Equal(100, Num(I.OutHeating));

            Send(I.HeatingAvailable, false);
            Run(60);
            Assert.Equal(0, Num(I.OutHeating));
            Assert.Equal(0, Num(I.OutFan));
        }

        [Fact]
        public void WindowAndPauseTimer_SwitchOff()
        {
            Send(I.Target, 22);
            Send(I.Current, 21);
            Run(1);
            Send(I.Window, true);
            Assert.Equal(0, Num(I.OutHeating));
            Assert.Equal(0, Num(I.OutFan));
            Assert.False(Flag(I.OutStatus));
            Assert.Equal(0, Num(I.OutMode));
            Send(I.Window, false);
            Run(1);
            Assert.Equal(100, Num(I.OutHeating));

            Send(I.PauseTimer, true);
            Run(1);
            Assert.Equal(0, Num(I.OutHeating));
            Run(7200, 100);
            Assert.Equal(100, Num(I.OutHeating));
        }

        [Fact]
        public void ManualFan_Boost_Silent()
        {
            Send(I.Fan, 70);
            Run(1);
            Assert.Equal(70, Num(I.OutFan));
            Assert.Equal(3, Num(I.OutFanStep));

            Send(I.Boost, true);
            Run(1);
            Assert.Equal(100, Num(I.OutFan));

            Send(I.Silent, true);
            Run(1);
            Assert.Equal(10, Num(I.OutFan));
            Assert.Equal(1, Num(I.OutFanStep));
        }

        [Fact]
        public void VentilationMode_RunsTheFanOnly()
        {
            SetParam(I.ParamMode, "4");
            Send(I.Target, 22);
            Send(I.Current, 21);
            Run(1);
            Assert.Equal(0, Num(I.OutHeating));
            Run(59);
            Assert.Equal(20, Num(I.OutFan));
            Run(180);
            Assert.Equal(80, Num(I.OutFan));
            Assert.Equal(4, Num(I.OutMode));
        }
    }

    public class FanCoilFreshAirTests : ClimateTest<FanCoilFreshAirLogicFactory>
    {
        private static readonly FanCoilIds I = FanCoilFreshAirLogicFactory.Ids;

        [Fact]
        public void BadAir_RunsTheFan_ValveByItsOwnController()
        {
            Send(I.Target, 22);
            Send(I.Current, 22);
            Send(I.Co2, 1000);
            Run(1);
            Assert.Equal(0, Num(I.OutHeating));
            Run(59);
            Assert.Equal(20, Num(I.OutFan));
            Run(180);
            Assert.Equal(80, Num(I.OutFan));

            // too cold while the air is bad: valve PI instead of a fully open valve
            Send(I.Current, 21);
            Run(1);
            Assert.InRange(Num(I.OutHeating), 50, 51);

            // good air again after the next CO2 sample: valve fully open
            Send(I.Co2, 400);
            Run(60);
            Assert.Equal(100, Num(I.OutHeating));
        }
    }

    public class FanCoilCentralTests : ClimateTest<FanCoilCentralLogicFactory>
    {
        [Fact]
        public void FourPipe_HeatsAndCoolsAtOnce()
        {
            Send(FC.InDemands[0], 80);
            Send(FC.InDemands[1], -70);
            Run(1);
            Assert.True(Flag(FC.OutHeating));
            Assert.True(Flag(FC.OutCooling));
            Assert.True(Flag(FC.OutUnitsHeat));
            Assert.True(Flag(FC.OutUnitsCool));
            Assert.False(Flag(FC.OutSwitchingValve));
            Assert.Equal(3, Num(FC.OutAvailableMode));

            Send(FC.InHeatingActive, false);
            Assert.False(Flag(FC.OutUnitsHeat));
            Assert.True(Flag(FC.OutHeating));
        }

        [Fact]
        public void TwoPipe_OneModeAtATime_ValveFirst()
        {
            SetParam(FC.ParamPipes, "1");
            SetParam(FC.ParamValveDelay, 20);
            Send(FC.InDemands[0], 80);
            Send(FC.InDemands[1], -70);
            Run(1);
            Assert.True(Flag(FC.OutHeating));
            Assert.False(Flag(FC.OutCooling));
            Assert.True(Flag(FC.OutUnitsHeat));
            Assert.False(Flag(FC.OutUnitsCool));

            Send(FC.InDemands[0], 0);
            Run(1);
            Assert.False(Flag(FC.OutHeating));
            Run(180);
            Assert.True(Flag(FC.OutSwitchingValve));
            Assert.False(Flag(FC.OutCooling));
            Run(20);
            Assert.True(Flag(FC.OutCooling));
        }

        [Fact]
        public void OutdoorLimits_AvailableMode()
        {
            Send(FC.InOutdoor, 25);
            Run(1);
            Assert.Equal(2, Num(FC.OutAvailableMode));
            Send(FC.InOutdoor, 10);
            Run(1);
            Assert.Equal(1, Num(FC.OutAvailableMode));
            Send(FC.InOff, true);
            Assert.Equal(0, Num(FC.OutAvailableMode));
        }
    }
}
