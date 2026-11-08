using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.EmergencyAlarm;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.EmergencyAlarm.EmergencyAlarmLogicFactory;

namespace P3.Logic.Surveillance.Tests.EmergencyAlarm
{
    public class EmergencyAlarmTests : SecurityLogicTest<EmergencyAlarmLogicFactory, EmergencyAlarmLogic>
    {
        private readonly TestSource _bathroom = new("Button bathroom");

        protected override EmergencyAlarmLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        [Fact]
        public void HoldTg_RaisesAlarmAfterHoldTime()
        {
            SetParam(F.ParamHoldAlarm, 4);
            Start();

            Input(F.InToggleHold, true, _bathroom);
            Advance(3.75);
            Assert.False(LastBool(F.OutAlarm));
            Advance(0.25);

            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("Button bathroom", Last(F.OutCause));
            Assert.Contains(true, Values(F.OutAlarmOn));
        }

        [Fact]
        public void ShortPress_DoesNothing()
        {
            Start();
            Input(F.InToggleHold, true, _bathroom);
            Advance(1);
            Input(F.InToggleHold, false, _bathroom);
            Advance(10);

            Assert.False(LastBool(F.OutAlarm));
        }

        [Fact]
        public void ReleaseAfterHoldTime_BetweenTicks_StillRaisesAlarm()
        {
            SetParam(F.ParamHoldAlarm, 4);
            Start();
            Input(F.InToggleHold, true, _bathroom);
            FakeTimeProvider.SetDateTime(UtcNow.AddSeconds(5));

            Input(F.InToggleHold, false, _bathroom);

            Assert.True(LastBool(F.OutAlarm));
        }

        [Fact]
        public void HoldTg_WhileAlarm_Acknowledges()
        {
            SetParam(F.ParamHoldConfirm, 2);
            Start();
            Pulse(F.InActivate, _bathroom);
            Assert.True(LastBool(F.OutAlarm));

            var nurse = new TestSource("Button hall");
            Input(F.InToggleHold, true, nurse);
            Advance(2);

            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal("Button hall", Last(F.OutConfirmCause));
            Assert.Contains(true, Values(F.OutAlarmOff));
        }

        [Fact]
        public void Escalation_TurnsOnAfterTime()
        {
            SetParam(F.ParamEscalation, 30);
            Start();
            Pulse(F.InActivate, _bathroom);

            Advance(29);
            Assert.False(LastBool(F.OutEscalated));
            Advance(1);
            Assert.True(LastBool(F.OutEscalated));
            Assert.Equal("escalated", VisuState()["status"]!.ToString());

            Command(EmergencyAlarmLogic.CmdAcknowledge);
            Assert.False(LastBool(F.OutEscalated));
        }

        [Fact]
        public void Off_PreventsAlarmAndAcknowledges()
        {
            Start();
            Pulse(F.InActivate);
            Input(F.InOff, true);
            Assert.False(LastBool(F.OutAlarm));

            Pulse(F.InActivate);
            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal(SecurityCommandResult.NotPossible, Command(EmergencyAlarmLogic.CmdActivate));
        }

        [Fact]
        public void Visu_ActivateAndAcknowledge()
        {
            Start();
            Assert.Equal(new[] { EmergencyAlarmLogic.CmdActivate }, ActionCommands());

            Assert.Equal(SecurityCommandResult.Ok, Command(EmergencyAlarmLogic.CmdActivate, user: "maria"));
            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("maria", Last(F.OutCause));
            Assert.Equal(new[] { EmergencyAlarmLogic.CmdAcknowledge }, ActionCommands());

            Assert.Equal(SecurityCommandResult.Ok, Command(EmergencyAlarmLogic.CmdAcknowledge, user: "tom"));
            Assert.False(LastBool(F.OutAlarm));
            Assert.Equal("tom", Last(F.OutConfirmCause));
        }

        [Fact]
        public void Alarm_SurvivesRestart()
        {
            Start();
            Pulse(F.InActivate, _bathroom);

            Restart();

            Assert.True(LastBool(F.OutAlarm));
            Assert.Equal("Button bathroom", Last(F.OutCause));
            Assert.Contains(VisuState()["log"]!, e => (string)e["event"] == "emergency");
        }

        [Fact]
        public void ReplayedValueAfterRestart_DoesNotRaiseAlarm()
        {
            Start(settle: false);
            Input(F.InActivate, true, _bathroom);

            Assert.False(LastBool(F.OutAlarm));
        }
    }
}
