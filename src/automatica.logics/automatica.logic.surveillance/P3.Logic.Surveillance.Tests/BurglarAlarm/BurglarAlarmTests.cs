using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.BurglarAlarm.BurglarAlarmLogicFactory;

namespace P3.Logic.Surveillance.Tests.BurglarAlarm
{
    public class BurglarAlarmTests : SecurityLogicTest<BurglarAlarmLogicFactory, BurglarAlarmLogic>
    {
        private readonly TestSource _kitchenWindow = new("Kitchen window");
        private readonly TestSource _frontDoor = new("Front door");
        private readonly TestSource _hallMotion = new("Hall motion");
        private readonly TestSource _livingMotion = new("Living motion");

        protected override BurglarAlarmLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        private int Status => (int)LastNumber(F.OutStatus);

        [Fact]
        public void Start_Fresh_IsDisarmed()
        {
            Start();
            Assert.Equal(0, Status);
            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal("disarmed", (string)VisuState()["status"]);
        }

        [Fact]
        public void ArmImmediately_WithAndWithoutPresence()
        {
            Start();
            Pulse(F.InArm);
            Assert.Equal(1, Status);

            Pulse(F.InOff);
            Assert.Equal(0, Status);

            Pulse(F.InArmNoPresence);
            Assert.Equal(2, Status);
        }

        [Fact]
        public void ArmDelayed_CountsDown_ThenArms()
        {
            SetParam(F.ParamArmingDelay, 60);
            Start();

            Pulse(F.InArmDelayed);
            Assert.Equal(0, Status);
            Assert.Equal(60, LastNumber(F.OutArmingRemaining));
            Assert.Equal("arming", (string)VisuState()["status"]);

            Advance(30);
            Assert.Equal(30, LastNumber(F.OutArmingRemaining));
            Assert.Equal(0, Status);

            Advance(30);
            Assert.Equal(1, Status);
            Assert.Equal(0, LastNumber(F.OutArmingRemaining));
        }

        [Fact]
        public void DisarmDuringArmingDelay_Cancels()
        {
            SetParam(F.ParamArmingDelay, 60);
            Start();
            Pulse(F.InArmDelayed);
            Pulse(F.InOff);
            Advance(120);
            Assert.Equal(0, Status);
        }

        [Fact]
        public void Window_WhileArmed_TriggersLevelsInTime()
        {
            Start();
            Pulse(F.InArm);
            Input(F.InWindow, true, _kitchenWindow);

            Assert.True(LastBool(F.OutAlarm));
            Assert.True(LastBool(F.OutSilent));
            Assert.False(LastBool(F.OutAudible));
            Assert.Equal("Kitchen window", Last(F.OutCause));

            Advance(20);
            Assert.True(LastBool(F.OutAudible));
            Assert.False(LastBool(F.OutVisual));

            Advance(20);
            Assert.True(LastBool(F.OutVisual));
            Advance(50);
            Assert.True(LastBool(F.OutInternal));
            Advance(60);
            Assert.True(LastBool(F.OutExternal));
            Assert.False(LastBool(F.OutRemote));
            Advance(150);
            Assert.True(LastBool(F.OutRemote));
            // levels accumulate
            Assert.True(LastBool(F.OutSilent));
            Assert.Equal(6, (int)VisuState()["level"]);
        }

        [Fact]
        public void Sensors_WhileDisarmed_DoNothing()
        {
            Start();
            Input(F.InWindow, true, _kitchenWindow);
            Input(F.InPresence, true, _hallMotion);
            Assert.False(LastBool(F.OutAlarm));
            Assert.True(LastBool(F.OutOpenContacts));
            Assert.Equal(2, LastNumber(F.OutActiveSensors));
        }

        [Fact]
        public void Acknowledge_ResetsAlarm_StaysArmed_AndRetriggers()
        {
            Start();
            Pulse(F.InArm);
            Input(F.InDoor, true, _frontDoor);
            Advance(25);
            Assert.True(LastBool(F.OutAudible));

            Pulse(F.InConfirm);
            Assert.False(LastBool(F.OutAlarm));
            Assert.False(LastBool(F.OutSilent));
            Assert.False(LastBool(F.OutAudible));
            Assert.Equal(1, Status);

            Input(F.InDoor, false, _frontDoor);
            Input(F.InDoor, true, _frontDoor);
            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void Disarm_EndsAlarm()
        {
            Start();
            Pulse(F.InArm);
            Input(F.InGlassBreakage, true, new TestSource("Glass"));
            Pulse(F.InOff);
            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal(0, Status);
        }

        [Fact]
        public void Presence_IgnoredWhenArmedWithoutPresence()
        {
            Start();
            Pulse(F.InArmNoPresence);
            Input(F.InPresence, true, _hallMotion);
            Assert.False(LastBool(F.OutAlarm));

            Input(F.InWindow, true, _kitchenWindow);
            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void SinglePresenceSensor_TriggersAtOnce()
        {
            Start();
            Pulse(F.InArm);
            Input(F.InPresence, true, _hallMotion);
            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("Hall motion", Last(F.OutCause));
        }

        [Fact]
        public void TwoPresenceSensors_NeedASecondOneWithinTheWindow()
        {
            SetParam(F.ParamSecondPresence, 60);
            Start();
            // both sensors are known (seen while disarmed)
            Input(F.InPresence, true, _hallMotion);
            Input(F.InPresence, false, _hallMotion);
            Input(F.InPresence, true, _livingMotion);
            Input(F.InPresence, false, _livingMotion);
            Pulse(F.InArm);

            Input(F.InPresence, true, _hallMotion);
            Assert.False(LastBool(F.OutAlarm));
            Input(F.InPresence, false, _hallMotion);

            // the same sensor again does not confirm
            Input(F.InPresence, true, _hallMotion);
            Assert.False(LastBool(F.OutAlarm));
            Input(F.InPresence, false, _hallMotion);

            Advance(30);
            Input(F.InPresence, true, _livingMotion);
            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("Living motion", Last(F.OutCause));
        }

        [Fact]
        public void TwoPresenceSensors_SecondTooLate_DoesNotTrigger()
        {
            SetParam(F.ParamSecondPresence, 60);
            Start();
            Input(F.InPresence, true, _hallMotion);
            Input(F.InPresence, false, _hallMotion);
            Input(F.InPresence, true, _livingMotion);
            Input(F.InPresence, false, _livingMotion);
            Pulse(F.InArm);

            Input(F.InPresence, true, _hallMotion);
            Input(F.InPresence, false, _hallMotion);
            Advance(90);
            Input(F.InPresence, true, _livingMotion);
            Assert.False(LastBool(F.OutAlarm));
        }

        [Fact]
        public void ArmWithOpenWindow_TriggersAlarm_ByDefault()
        {
            Start();
            Input(F.InWindow, true, _kitchenWindow);
            Pulse(F.InArm);
            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("Attention, Kitchen window is still open!", Last(F.OutOpenContactsText));
        }

        [Fact]
        public void ArmWithOpenWindow_ArmOpenContacts_IgnoresItUntilItChanges()
        {
            SetParam(F.ParamArmOpenContacts, true);
            Start();
            Input(F.InWindow, true, _kitchenWindow);
            Input(F.InDoor, true, _frontDoor);
            Pulse(F.InArm);

            Assert.False(LastBool(F.OutAlarm));
            Assert.True(LastBool(F.OutOpenContacts));
            Assert.Equal("Attention! Several windows or doors are still open!", Last(F.OutOpenContactsText));

            // the window is closed and opened again while armed: that is an intrusion
            Input(F.InWindow, false, _kitchenWindow);
            Assert.False(LastBool(F.OutAlarm));
            Input(F.InWindow, true, _kitchenWindow);
            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void MaxAlarmDuration_FallsBackToSilentAlarm()
        {
            SetParam(F.ParamMaxAlarm, 400);
            Start();
            Pulse(F.InArm);
            Input(F.InWindow, true, _kitchenWindow);
            Advance(350);
            Assert.True(LastBool(F.OutRemote));

            Advance(60);
            Assert.False(LastBool(F.OutRemote));
            Assert.False(LastBool(F.OutAudible));
            Assert.True(LastBool(F.OutSilent));
            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void MaxAlarmDuration_WithSilentConfirm_AcknowledgesEverything()
        {
            SetParam(F.ParamMaxAlarm, 100);
            SetParam(F.ParamSilentConfirm, true);
            Start();
            Pulse(F.InArm);
            Input(F.InWindow, true, _kitchenWindow);
            Advance(101);
            Assert.False(LastBool(F.OutSilent));
            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal(1, Status);
        }

        [Fact]
        public void TestMode_OnlySetsTestOutput()
        {
            SetParam(F.ParamTestMode, true);
            Start();
            Pulse(F.InArm);
            Input(F.InWindow, true, _kitchenWindow);
            Advance(400);

            Assert.True(LastBool(F.OutTest));
            Assert.False(LastBool(F.OutAlarm));
            Assert.False(LastBool(F.OutSilent));
            Assert.False(LastBool(F.OutRemote));
            Pulse(F.InConfirm);
            Assert.False(LastBool(F.OutTest));
        }

        [Fact]
        public void DisablePeriphery_BlocksArmInputs_NotOffOrVisu()
        {
            Start();
            Input(F.InDisablePeriphery, true);
            Pulse(F.InArm);
            Assert.Equal(0, Status);

            Assert.Equal(SecurityCommandResult.Ok, Command(BurglarAlarmLogic.CmdArm));
            Assert.Equal(1, Status);

            Pulse(F.InOff);
            Assert.Equal(0, Status);
        }

        [Fact]
        public void Toggle_ArmsAndDisarms()
        {
            Start();
            Pulse(F.InToggle);
            Assert.Equal(1, Status);
            Pulse(F.InToggle);
            Assert.Equal(0, Status);
            Pulse(F.InToggleNoPresence);
            Assert.Equal(2, Status);
        }

        [Fact]
        public void OffHeld_LocksArming()
        {
            Start();
            Input(F.InOff, true);
            Pulse(F.InArm);
            Assert.Equal(0, Status);
            Assert.Equal(SecurityCommandResult.NotPossible, Command(BurglarAlarmLogic.CmdArm));

            Input(F.InOff, false);
            Pulse(F.InArm);
            Assert.Equal(1, Status);
        }

        [Fact]
        public void ActiveSensors_StretchedByPulseExtension()
        {
            SetParam(F.ParamPulseExtension, 10);
            Start();
            var vibration = new TestSource("Vibration");
            Input(F.InOther, true, vibration);
            Input(F.InOther, false, vibration);
            Assert.Equal(1, LastNumber(F.OutActiveSensors));
            Advance(11);
            Assert.Equal(0, LastNumber(F.OutActiveSensors));
        }

        [Fact]
        public void State_SurvivesRestart_ArmedAndAlarm()
        {
            Start();
            Pulse(F.InArm);
            Input(F.InWindow, true, _kitchenWindow);
            Advance(25);

            Restart();

            Assert.Equal(1, Status);
            Assert.True(LastBool(F.OutAlarm));
            Assert.True(LastBool(F.OutAudible));
            Assert.Equal("Kitchen window", Last(F.OutCause));
            Assert.Contains(VisuState()["log"]!, a => (string)a["event"] == "alarm");
        }

        [Fact]
        public void State_SurvivesRestart_ArmingDelayContinues()
        {
            SetParam(F.ParamArmingDelay, 100);
            Start();
            Pulse(F.InArmDelayed);
            Advance(40);

            Restart(settle: false);
            Advance(50);
            Assert.Equal(0, Status);
            Advance(15);
            Assert.Equal(1, Status);
        }

        [Fact]
        public void AfterRestart_ReplayedArmPulse_IsIgnored()
        {
            Start();
            Restart(settle: false);
            // a driver replays the last value of the arm button right after the start
            Input(F.InArm, true);
            Assert.Equal(0, Status);
        }

        [Fact]
        public void AfterRestart_ArmedSensorsIgnoredDuringStartDelay()
        {
            SetParam(F.ParamStartDelay, 10);
            Start();
            Pulse(F.InArm);
            Restart(settle: false);

            Input(F.InPresence, true, _hallMotion);
            Assert.False(LastBool(F.OutAlarm));

            Advance(11);
            Input(F.InPresence, false, _hallMotion);
            Input(F.InPresence, true, _hallMotion);
            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void Visu_ActionsFollowTheState()
        {
            Start();
            Assert.Contains(BurglarAlarmLogic.CmdArmDelayed, ActionCommands());
            Assert.DoesNotContain(BurglarAlarmLogic.CmdDisarm, ActionCommands());

            Assert.Equal(SecurityCommandResult.Ok, Command(BurglarAlarmLogic.CmdArm, user: "anna"));
            Assert.Equal(new[] { BurglarAlarmLogic.CmdDisarm }, ActionCommands());

            Input(F.InWindow, true, _kitchenWindow);
            Assert.Equal(new[] { BurglarAlarmLogic.CmdAcknowledge, BurglarAlarmLogic.CmdDisarm }, ActionCommands());

            Assert.Equal(SecurityCommandResult.Ok, Command(BurglarAlarmLogic.CmdDisarm, user: "bert"));
            var log = VisuState()["log"]!.ToList();
            Assert.Equal("disarmed", (string)log[0]["event"]);
            Assert.Equal("bert", (string)log[0]["detail"]);
            Assert.Contains(log, a => (string)a["event"] == "armed" && (string)a["detail"] == "anna");
        }

        [Fact]
        public void Visu_Commands()
        {
            Start();
            Assert.Equal(SecurityCommandResult.UnknownCommand, Command("open-sesame"));
            Assert.Equal(SecurityCommandResult.NotPossible, Command(BurglarAlarmLogic.CmdDisarm));
            Assert.Equal(SecurityCommandResult.NotPossible, Command(BurglarAlarmLogic.CmdAcknowledge));
            Assert.Equal(SecurityCommandResult.Ok, Command(BurglarAlarmLogic.CmdArmDelayedNoPresence));
            Assert.Equal("arming", (string)VisuState()["status"]);
            Assert.NotNull(VisuState()["countdownUntil"]);
        }

        [Fact]
        public void DataForVisu_NeverContainsParameters()
        {
            Start();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(Sut.GetDataForVisu());
            Assert.DoesNotContain(F.ParamArmingDelay.ToString(), json);
            Assert.Contains("\"status\"", json);
        }

        [Fact]
        public void Log_IsLimited()
        {
            Start();
            for (var i = 0; i < 40; i++)
            {
                Pulse(F.InArm);
                Pulse(F.InOff);
            }
            Assert.Equal(SecurityLogicBase<BurglarAlarmState>.MaxLogEntries, Sut.CurrentState.Log.Count);
        }
    }
}
