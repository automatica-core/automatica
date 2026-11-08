using System.Linq;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.AlarmChain;
using P3.Logic.Surveillance.Security;
using P3.Logic.Surveillance.Tests.TestSupport;
using Xunit;
using F = P3.Logic.Surveillance.AlarmChain.AlarmChainLogicFactory;

namespace P3.Logic.Surveillance.Tests.AlarmChain
{
    public class AlarmChainTests : SecurityLogicTest<AlarmChainLogicFactory, AlarmChainLogic>
    {
        private readonly TestSource _fire = new("Fire alarm");

        protected override AlarmChainLogic Create(ILogicContext context, ISecurityStateStore store) => new(context, store);

        private string Stage(int n) => (string)Last(F.OutStages[n - 1]) ?? "";

        private void Setup(int stages = 3, int reaction = 60, int repetitions = 2)
        {
            SetParam(F.ParamStages, stages);
            SetParam(F.ParamReactionTime, reaction);
            SetParam(F.ParamMaxRepetitions, repetitions);
            SetParam(F.ParamAlarmText, "Alarm <vt1> at <vcn>");
            SetParam(F.ParamEmergencyText, "Emergency <vt1>, <vca>");
            SetParam(F.ParamCustomerName, "Miller");
            SetParam(F.ParamCustomerAddress, "Main street 1");
        }

        [Fact]
        public void Alarm_HandsOverStageByStage()
        {
            Setup();
            Start();
            Input(F.InText1, "smoke kitchen");

            Input(F.InAlarm, true, _fire);

            Assert.Equal("Alarm smoke kitchen at Miller", Stage(1));
            Assert.Equal("", Stage(2));
            Assert.Equal(1, LastNumber(F.OutStage));

            Advance(60);
            Assert.Equal("", Stage(1));
            Assert.Equal("Alarm smoke kitchen at Miller", Stage(2));
            Assert.Equal(2, LastNumber(F.OutStage));

            Advance(60);
            Assert.Equal("", Stage(2));
            Assert.Equal("Alarm smoke kitchen at Miller", Stage(3));
            Assert.Equal("", Stage(4));
        }

        [Fact]
        public void AfterRepetitions_EmergencyServicesAreAlarmed()
        {
            Setup(stages: 2, reaction: 10, repetitions: 2);
            Start();
            Input(F.InText1, "smoke");
            Input(F.InAlarm, true);

            // stage 1, 2, (repetition) 1, 2 -> emergency
            Advance(10);
            Advance(10);
            Assert.Equal(1, LastNumber(F.OutStage));
            Advance(10);
            Assert.Equal("", (string)Last(F.OutEmergency) ?? "");
            Advance(10);

            Assert.Equal("Emergency smoke, Main street 1", Last(F.OutEmergency));
            Assert.Equal("", Stage(1));
            Assert.Equal("", Stage(2));
            Assert.Equal("emergency", VisuState()["status"]!.ToString());
        }

        [Fact]
        public void Urgent_SendsAllStagesAndRepeats()
        {
            Setup(stages: 3, reaction: 30);
            Start();

            Input(F.InUrgent, true);

            Assert.All(Enumerable.Range(1, 3), n => Assert.StartsWith("Alarm", Stage(n)));
            Assert.Equal("", Stage(4));
            Assert.Equal(-1, LastNumber(F.OutStage));

            var before = Values(F.OutStages[0]).Count;
            Advance(30);
            Assert.True(Values(F.OutStages[0]).Count > before);
        }

        [Fact]
        public void ReactionTimeZero_SendsAllStagesAtOnce()
        {
            Setup(stages: 4, reaction: 0);
            Start();

            Input(F.InAlarm, true);

            Assert.All(Enumerable.Range(1, 4), n => Assert.StartsWith("Alarm", Stage(n)));
            Assert.Equal("", Stage(5));
        }

        [Fact]
        public void Confirm_StopsChainAndRecordsCause()
        {
            Setup();
            Start();
            Input(F.InAlarm, true);
            Advance(60);

            var neighbour = new TestSource("Neighbour button");
            Pulse(F.InConfirm, neighbour);

            Assert.Equal("", Stage(2));
            Assert.Equal(0, LastNumber(F.OutStage));
            Assert.Equal("Neighbour button", Last(F.OutConfirmCause));
            Assert.NotEqual("", Last(F.OutConfirmTime));

            Advance(600);
            Assert.All(Enumerable.Range(1, 3), n => Assert.Equal("", Stage(n)));
        }

        [Fact]
        public void HeldConfirm_LocksTheChain()
        {
            Setup();
            Start();
            Input(F.InConfirm, true);

            Input(F.InAlarm, true);
            Assert.Equal("", Stage(1));

            Input(F.InConfirm, false);
            Input(F.InAlarm, false);
            Input(F.InAlarm, true);
            Assert.StartsWith("Alarm", Stage(1));
        }

        [Fact]
        public void EmergencyInput_AlarmsEmergencyServicesDirectly()
        {
            Setup();
            Start();
            Input(F.InText1, "panic");

            Input(F.InEmergency, true);

            Assert.Equal("Emergency panic, Main street 1", Last(F.OutEmergency));
        }

        [Fact]
        public void Placeholders_NumbersAndTimeToEmergency()
        {
            Setup(stages: 2, reaction: 10, repetitions: 3);
            SetParam(F.ParamAlarmText, "<v1.1> C <vn> <vtes>s");
            Start();
            Input(F.InText1, 47.26);

            Input(F.InAlarm, true);

            // 3 repetitions x 2 stages x 10 s = 60 s
            Assert.Matches("^47\\.3 C .* 60s$", Stage(1));
        }

        [Fact]
        public void TextChanges_FollowOnActiveStage()
        {
            Setup();
            Start();
            Input(F.InText1, "a");
            Input(F.InAlarm, true);
            Input(F.InText1, "b");

            Assert.Equal("Alarm b at Miller", Stage(1));
        }

        [Fact]
        public void Visu_Acknowledge()
        {
            Setup();
            Start();
            Assert.Empty(ActionCommands());
            Input(F.InAlarm, true);

            Assert.Equal(new[] { AlarmChainLogic.CmdAcknowledge }, ActionCommands());
            Assert.Equal(SecurityCommandResult.Ok, Command(AlarmChainLogic.CmdAcknowledge, user: "anna"));
            Assert.Equal("anna", Last(F.OutConfirmCause));
            Assert.Equal(SecurityCommandResult.NotPossible, Command(AlarmChainLogic.CmdAcknowledge));
        }

        [Fact]
        public void RunningChain_SurvivesRestart()
        {
            Setup();
            Start();
            Input(F.InAlarm, true);
            Advance(60);

            Restart();

            Assert.Equal(2, LastNumber(F.OutStage));
            Assert.StartsWith("Alarm", Stage(2));
            Advance(60);
            Assert.Equal(3, LastNumber(F.OutStage));
        }
    }
}
