using P3.Logic.Operations.Climate.RoomController;
using Xunit;
using F = P3.Logic.Operations.Climate.RoomController.RoomControllerLogicFactory;

namespace P3.Logic.Operations.Tests.Climate
{
    public class RoomControllerTests : ClimateTest<RoomControllerLogicFactory>
    {
        private TemperatureMode Mode => (TemperatureMode)(int)Num(F.OutTemperatureMode);

        [Fact]
        public void Comfort_HeatsWithProportionalBand()
        {
            Send(F.InCurrent, 21.5);
            Run(5, 5);

            Assert.Equal(TemperatureMode.Comfort, Mode);
            Assert.Equal(1, Num(F.OutHeatingCoolingMode));
            Assert.Equal(22, Num(F.OutTarget));
            // Pb 2 K: 0.5 K = 25 % (+ a tiny integral part)
            Assert.InRange(Num(F.OutHeating), 25, 25.1);
            Assert.Equal(0, Num(F.OutCooling));
            Assert.Equal(Num(F.OutHeating), Num(F.OutDemand));
            Assert.Equal(Num(F.OutHeating), Num(F.OutHeatingCooling));
        }

        [Fact]
        public void TooWarm_Cools_NegativeDemand()
        {
            Send(F.InCurrent, 25.5);
            Run(5, 5);

            Assert.Equal(-1, Num(F.OutHeatingCoolingMode));
            Assert.Equal(24.5, Num(F.OutTarget));
            Assert.InRange(Num(F.OutCooling), 50, 50.2);
            Assert.Equal(0, Num(F.OutHeating));
            Assert.Equal(-Num(F.OutCooling), Num(F.OutDemand));
        }

        [Fact]
        public void BetweenSetpoints_NoValve_KeepsDirection()
        {
            Send(F.InCurrent, 21);
            Run(5, 5);
            Assert.Equal(1, Num(F.OutHeatingCoolingMode));

            Send(F.InCurrent, 23);
            Run(5, 5);
            Assert.Equal(1, Num(F.OutHeatingCoolingMode));
            Assert.Equal(0, Num(F.OutHeating));
            Assert.Equal(0, Num(F.OutCooling));
        }

        [Fact]
        public void DefaultEco_UsesEcoSetpoints()
        {
            SetParam(F.ParamDefaultMode, "0");
            Send(F.InCurrent, 18);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Eco, Mode);
            Assert.Equal(19, Num(F.OutTarget));

            Send(F.InCurrent, 28);
            Run(5, 5);
            Assert.Equal(27.5, Num(F.OutTarget));
        }

        [Fact]
        public void ComfortOverride_ExtendedAfterFallingEdge()
        {
            SetParam(F.ParamDefaultMode, "0");
            Send(F.InCurrent, 21);
            Send(F.InComfort, true);
            Assert.Equal(TemperatureMode.Comfort, Mode);

            Send(F.InComfort, false);
            Run(3540, 60);
            Assert.Equal(TemperatureMode.Comfort, Mode);
            Run(60, 60);
            Assert.Equal(TemperatureMode.Eco, Mode);
        }

        [Fact]
        public void EcoWithoutExtension_StaysUntilNextOverride()
        {
            SetParam(F.ParamEcoExtend, 0);
            Send(F.InCurrent, 21);
            Send(F.InEco, true);
            Send(F.InEco, false);
            Run(10000, 100);
            Assert.Equal(TemperatureMode.Eco, Mode);

            Send(F.InBuildingProtection, true);
            Assert.Equal(TemperatureMode.BuildingProtection, Mode);
            Send(F.InComfort, true);
            Assert.Equal(TemperatureMode.Comfort, Mode);
        }

        [Fact]
        public void Presence_KeepsComfortForPet()
        {
            SetParam(F.ParamDefaultMode, "0");
            Send(F.InCurrent, 21);
            Send(F.InPresence, true);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);

            Send(F.InPresence, false);
            Run(1795, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Eco, Mode);

            Send(F.InPresence, true);
            Send(F.InDisablePresence, true);
            Assert.Equal(TemperatureMode.Eco, Mode);
        }

        [Fact]
        public void OpenWindow_BuildingProtection_OnlyWhenOutsideWorksAgainst()
        {
            Send(F.InCurrent, 20);
            Send(F.InOutdoor, 5);
            Run(5, 5);
            Assert.Equal(1, Num(F.OutHeatingCoolingMode));

            Send(F.InWindow, true);
            Run(295, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);
            Run(5, 5);
            Assert.Equal(TemperatureMode.BuildingProtection, Mode);
            Assert.Equal(5, Num(F.OutTarget));
            Assert.Equal(0, Num(F.OutHeating));

            // warmer outside than in the room while heating - the open window helps
            Send(F.InOutdoor, 25);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);

            Send(F.InOutdoor, 5);
            Send(F.InWindow, false);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);
        }

        [Fact]
        public void OffMode_AndOffInput_LockAndCancelOverrides()
        {
            Send(F.InCurrent, 20);
            Send(F.InEco, true);
            Send(F.InEco, false);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Eco, Mode);

            Send(F.InOff, true);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Off, Mode);
            Assert.Equal(0, Num(F.OutHeating));
            Assert.Equal(0, Num(F.OutDemand));
            Assert.Equal(0, Num(F.OutHeatingCoolingMode));

            // the rising edge cancelled the eco override
            Send(F.InOff, false);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Comfort, Mode);

            Send(F.InMode, -1);
            Run(5, 5);
            Assert.Equal(TemperatureMode.Off, Mode);
            Assert.Equal(0, Num(F.OutHeating));
        }

        [Fact]
        public void SourceMode_ClosesValves_DemandStays()
        {
            Send(F.InCurrent, 21);
            Send(F.InSourceMode, 2);
            Run(5, 5);
            Assert.Equal(0, Num(F.OutHeating));
            Assert.Equal(0, Num(F.OutHeatingCooling));
            Assert.True(Num(F.OutDemand) > 0);

            Send(F.InSourceMode, 1);
            Assert.True(Num(F.OutHeating) > 0);
        }

        [Fact]
        public void ExcessEnergy_ShiftsSetpoints()
        {
            Send(F.InCurrent, 21);
            Send(F.InExcessHeating, true);
            Run(5, 5);
            Assert.Equal(23, Num(F.OutTarget));

            Send(F.InExcessHeating, false);
            Send(F.InExcessCooling, true);
            Send(F.InCurrent, 24);
            Run(5, 5);
            // cooling setpoint 24.5 - 1
            Assert.Equal(-1, Num(F.OutHeatingCoolingMode));
            Assert.Equal(23.5, Num(F.OutTarget));
        }

        [Fact]
        public void VisuComfortTemperature_KeepsTheCoolingGap()
        {
            Send(F.InComfortTemperature, 21);
            Assert.Equal(21, Num(F.OutComfortHeating));
            Assert.Equal(23.5, Num(F.OutComfortCooling));

            Send(F.InCurrent, 20.5);
            Run(5, 5);
            Assert.Equal(21, Num(F.OutTarget));
        }

        [Fact]
        public void FixedSetpoint_AndHeatingOnly()
        {
            Send(F.InMode, 3);
            Send(F.InTarget, 25);
            Send(F.InCurrent, 24);
            Run(5, 5);
            Assert.Equal(TemperatureMode.ManualTarget, Mode);
            Assert.Equal(25, Num(F.OutTarget));
            Assert.Equal(1, Num(F.OutHeatingCoolingMode));

            // heating only: too warm does not cool
            Send(F.InMode, 4);
            Send(F.InCurrent, 30);
            Run(5, 5);
            Assert.Equal(0, Num(F.OutCooling));
            Assert.Equal(0, Num(F.OutHeating));
            Assert.NotEqual(-1, Num(F.OutHeatingCoolingMode));

            // cooling only
            Send(F.InMode, 5);
            Run(5, 5);
            Assert.Equal(-1, Num(F.OutHeatingCoolingMode));
            Assert.Equal(26.5, Num(F.OutTarget));
            Assert.True(Num(F.OutCooling) > 0);
        }

        [Fact]
        public void Shading_Boost_Error()
        {
            Send(F.InCurrent, 28);
            Run(5, 5);
            Assert.True(Flag(F.OutShading));
            Assert.True(Flag(F.OutBoost));
            Assert.False(Flag(F.OutError));

            Send(F.InCurrent, 29);
            Assert.True(Flag(F.OutError));

            // shading hysteresis 0.4 K below Tsc 23.5
            Send(F.InCurrent, 23.3);
            Assert.True(Flag(F.OutShading));
            Send(F.InCurrent, 23);
            Assert.False(Flag(F.OutShading));
            Assert.False(Flag(F.OutBoost));
        }
    }
}
