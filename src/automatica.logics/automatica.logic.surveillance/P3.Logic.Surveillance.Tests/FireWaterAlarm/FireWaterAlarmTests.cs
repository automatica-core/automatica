using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.FireWaterAlarm;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.FireWaterAlarm.FireWaterAlarmLogicFactory;

namespace P3.Logic.Surveillance.Tests.FireWaterAlarm
{
    public class FireWaterAlarmTests : SecurityLogicTest<FireWaterAlarmLogicFactory, FireWaterAlarmLogic>
    {
        private readonly TestSource _kitchen = new("Smoke kitchen");
        private readonly TestSource _cellar = new("Water cellar");

        protected override FireWaterAlarmLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        [Fact]
        public void Smoke_StartsPreAlarm_ThenMainAlarmAfterDelay()
        {
            SetParam(F.ParamMainDelay, 60);
            Start();

            Input(F.InSmoke, true, _kitchen);

            Assert.True(LastBool(F.OutPreAlarm));
            Assert.True(LastBool(F.OutPreAlarmSignal));
            Assert.False(LastBool(F.OutMainAlarm));
            Assert.Equal("Smoke kitchen", Last(F.OutCause));
            Assert.Equal(1, LastNumber(F.OutActiveSensors));

            Advance(59);
            Assert.False(LastBool(F.OutMainAlarm));
            Advance(2);
            Assert.True(LastBool(F.OutMainAlarm));
            Assert.True(LastBool(F.OutMainAlarmSignal));
            Assert.Equal("main-alarm", VisuState()["status"]!.ToString());
        }

        [Fact]
        public void Temperature_TriggersAtThreshold()
        {
            SetParam(F.ParamMaxTemperature, 50);
            Start();

            Input(F.InTemperature, 49.9);
            Assert.False(LastBool(F.OutPreAlarm));
            Input(F.InTemperature, 50);
            Assert.True(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void Silence_StopsSignals_MainAlarmSoundsAgain()
        {
            SetParam(F.ParamMainDelay, 30);
            Start();
            Input(F.InWater, true, _cellar);

            Pulse(F.InConfirmSignals);
            Assert.True(LastBool(F.OutPreAlarm));
            Assert.False(LastBool(F.OutPreAlarmSignal));

            Advance(31);
            Assert.True(LastBool(F.OutMainAlarmSignal));
        }

        [Fact]
        public void Acknowledge_ResetsEverything()
        {
            Start();
            Input(F.InSmoke, true, _kitchen);

            Assert.Equal(SecurityCommandResult.Ok, Command(FireWaterAlarmLogic.CmdAcknowledge));

            Assert.False(LastBool(F.OutPreAlarm));
            Assert.False(LastBool(F.OutPreAlarmSignal));
            Assert.Equal(SecurityCommandResult.NotPossible, Command(FireWaterAlarmLogic.CmdAcknowledge));
        }

        [Fact]
        public void SensorStillActive_DoesNotRetriggerUntilItFallsAndRises()
        {
            Start();
            Input(F.InSmoke, true, _kitchen);
            Pulse(F.InConfirm);
            Assert.False(LastBool(F.OutPreAlarm));

            Input(F.InSmoke, true, _kitchen);
            Assert.False(LastBool(F.OutPreAlarm));

            Input(F.InSmoke, false, _kitchen);
            Input(F.InSmoke, true, _kitchen);
            Assert.True(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void MaxAlarm_EndsMainAlarm_PreAlarmStays()
        {
            SetParam(F.ParamMainDelay, 10);
            SetParam(F.ParamMaxAlarm, 60);
            Start();
            Input(F.InSmoke, true, _kitchen);

            Advance(11);
            Assert.True(LastBool(F.OutMainAlarm));
            Advance(50);
            Assert.False(LastBool(F.OutMainAlarm));
            Assert.True(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void MaxAlarm_WithPreAlarmConfirm_AcknowledgesAutomatically()
        {
            SetParam(F.ParamMaxAlarm, 60);
            SetParam(F.ParamPreAlarmConfirm, true);
            Start();
            Input(F.InSmoke, true, _kitchen);

            Advance(61);

            Assert.False(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void ServiceMode_FromVisu_OnlyRaisesTestAlarm_AndEnds()
        {
            SetParam(F.ParamServiceDuration, 600);
            Start();

            Assert.Equal(SecurityCommandResult.Ok, Command(FireWaterAlarmLogic.CmdServiceStart));
            Input(F.InSmoke, true, _kitchen);

            Assert.False(LastBool(F.OutPreAlarm));
            Assert.True(LastBool(F.OutTest));
            Pulse(F.InConfirm);
            Assert.False(LastBool(F.OutTest));

            Advance(601);
            Input(F.InSmoke, false, _kitchen);
            Input(F.InSmoke, true, _kitchen);
            Assert.True(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void ServiceMode_ParameterOne_IsPermanent()
        {
            SetParam(F.ParamServiceMode, 1);
            Start();
            Advance(100000);

            Input(F.InSmoke, true, _kitchen);

            Assert.False(LastBool(F.OutPreAlarm));
            Assert.True(LastBool(F.OutTest));
        }

        [Fact]
        public void Off_LocksSensors()
        {
            Start();
            Input(F.InOff, true);
            Input(F.InSmoke, true, _kitchen);
            Assert.False(LastBool(F.OutPreAlarm));
        }

        [Fact]
        public void Fire_SwitchesLightsOnAndBlindsUp_WaterDoesNot()
        {
            var light = new FakeSwitch("Hall");
            var blind = new FakeBlind("Living");
            Controls.Add(light);
            Controls.Add(blind);
            SetParam(F.ParamLights, ControlsOf(light));
            SetParam(F.ParamBlinds, ControlsOf(blind));
            Start();

            Input(F.InWater, true, _cellar);
            System.Threading.Thread.Sleep(200);
            Assert.Equal(SwitchState.Off, light.State);
            Assert.Equal(0, blind.UpCalls);
            Pulse(F.InConfirm);

            Input(F.InSmoke, true, _kitchen);
            Assert.True(System.Threading.SpinWait.SpinUntil(() => light.State == SwitchState.On && blind.UpCalls == 1, 5000));
        }

        [Fact]
        public void Alarm_SurvivesRestart()
        {
            SetParam(F.ParamMainDelay, 60);
            Start();
            Input(F.InWater, true, _cellar);

            Restart();

            Assert.True(LastBool(F.OutPreAlarm));
            Assert.Equal("water", ((FireWaterAlarmLogic)Control).CurrentState.AlarmKind);
            Advance(60);
            Assert.True(LastBool(F.OutMainAlarm));
        }

        [Fact]
        public void Visu_HasNoDisarm()
        {
            Start();
            Assert.DoesNotContain("disarm", Control.SecurityCommands);
            Assert.Contains(FireWaterAlarmLogic.CmdServiceStart, ActionCommands());
        }
    }
}
