using System;
using System.Linq;
using P3.Logic.Time.SequenceController;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.SequenceController
{
    /// <summary>
    /// Sequence controller: one line per step, the steps are fired by the test (<see cref="TimeLogicTest{T}.FireTick"/>),
    /// sleep uses the fake clock.
    /// </summary>
    public class SequenceControllerTests : TimeLogicTest<SequenceControllerLogicFactory>
    {
        private static readonly DateTime T0 = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
        private static Guid AQ(int n) => SequenceControllerLogicFactory.RuleOutputs[n - 1];
        private static Guid AI(int n) => SequenceControllerLogicFactory.RuleInputs[n - 1];
        private static Guid S(int n) => SequenceControllerLogicFactory.RuleStartSequence[n - 1];
        private static readonly Guid Active = SequenceControllerLogicFactory.RuleActiveSequence;
        private static readonly Guid Line = SequenceControllerLogicFactory.RuleLine;
        private static readonly Guid Text = SequenceControllerLogicFactory.RuleText;
        private static readonly Guid Errors = SequenceControllerLogicFactory.RuleErrors;

        private SequenceControllerRule Rule => (SequenceControllerRule)Sut;

        private void Run(string program, int interval = 100)
        {
            SetUtcNow(T0);
            Interface(SequenceControllerLogicFactory.RuleParamProgram).Value = program;
            Interface(SequenceControllerLogicFactory.RuleParamInterval).Value = interval;
            Start();
        }

        private void Advance(TimeSpan by) => SetUtcNow(FakeTimeProvider.GetUtcNow().UtcDateTime + by);

        private void Ticks(int count)
        {
            for (var i = 0; i < count; i++)
            {
                FireTick();
            }
        }

        private object[] Out(int n) => Values(AQ(n)).ToArray();

        [Fact]
        public void Template_HasLoxoneInterfaces()
        {
            // S1-8, AI1-8, S, Off / AQ1-8, S, L, TQ, Err / program, interval
            Assert.Equal(18 + 12 + 2, RuleInstance.RuleInterfaceInstance.Count);
        }

        [Fact]
        public void Interval_IsClamped()
        {
            Run("set AQ1 = 1", interval: 5);
            Assert.Equal(SequenceControllerLogicFactory.MinIntervalMs, Rule.Interval);
            Assert.Equal(SequenceControllerLogicFactory.MinIntervalMs, SutTimer.Interval);
            Input(SequenceControllerLogicFactory.RuleParamInterval, 5000);
            Assert.Equal(SequenceControllerLogicFactory.MaxIntervalMs, Rule.Interval);
        }

        [Fact]
        public void Start_RunsTheFirstLineRightAway_ThenOneLinePerStep()
        {
            Run("set AQ1 = AI1 * 2\nset AQ2 = AQ1 + 1\nset AQ1 = 0");
            Input(AI(1), 3);

            Input(S(1), true);
            Assert.Equal(new object[] { 6.0 }, Out(1));
            Assert.Empty(Out(2));
            Assert.Equal(1, Last(Active));
            Assert.Equal(2, Last(Line));
            Assert.True(SutTimer.Enabled);

            FireTick();
            Assert.Equal(new object[] { 7.0 }, Out(2));
            FireTick();
            Assert.Equal(new object[] { 6.0, 0.0 }, Out(1));

            // past the last line the sequence ends
            FireTick();
            Assert.Equal(0, Last(Active));
            Assert.Equal(0, Last(Line));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void FalsyStartValue_DoesNothing()
        {
            Run("set AQ1 = 1");
            Input(S(1), false);
            Input(S(1), 0);
            Assert.Empty(Out(1));
            Assert.Equal(0, Rule.ActiveSequence);
        }

        [Fact]
        public void BlankLinesAndComments_NeedNoStep()
        {
            Run("set AQ1 = 1\n\n// wait\n   \nset AQ1 = 2");
            Input(S(1), 1);
            FireTick();
            Assert.Equal(new object[] { 1.0, 2.0 }, Out(1));
        }

        [Fact]
        public void Sleep_WaitsOnTheFakeClock_ThenContinuesInTheSameStep()
        {
            Run("set AQ1 = 1\nsleep 10 s\nset AQ1 = 2");
            Input(S(1), 1);
            FireTick(); // sleep starts
            Assert.Equal(2, Rule.CurrentLine);

            Advance(TimeSpan.FromSeconds(9));
            Ticks(5);
            Assert.Equal(new object[] { 1.0 }, Out(1));

            Advance(TimeSpan.FromSeconds(1));
            FireTick();
            Assert.Equal(new object[] { 1.0, 2.0 }, Out(1));
        }

        [Fact]
        public void WaitCondition_StaysUntilMet()
        {
            // the Loxone example: waits until AI1 + 3 is greater than the variable value1
            Run("set value1 = 10\nwaitcondition AI1 + 3 > value1\nset AQ1 = 1");
            Input(AI(1), 0);
            Input(S(1), 1);
            Ticks(4);
            Assert.Empty(Out(1));
            Assert.Equal(2, Rule.CurrentLine);

            Input(AI(1), 8);
            Ticks(2);
            Assert.Equal(new object[] { 1.0 }, Out(1));
        }

        [Fact]
        public void WaitCondition_WithInputs()
        {
            Run("waitcondition AI1 > AI2\nset AQ1 = 1");
            Input(AI(1), 1);
            Input(AI(2), 5);
            Input(S(1), 1);
            Ticks(3);
            Assert.Empty(Out(1));
            Assert.Equal(1, Rule.CurrentLine);

            Input(AI(1), 6);
            FireTick(); // condition met, moves on
            FireTick();
            Assert.Equal(new object[] { 1.0 }, Out(1));
        }

        [Fact]
        public void SetPulse_EndsWithTheNextStep()
        {
            Run("setpulse AQ1\nsetpulse AQ2 = 4\nset AQ3 = 1");
            Input(S(1), 1);
            Assert.Equal(new object[] { 1.0 }, Out(1));
            FireTick();
            Assert.Equal(new object[] { 1.0, 0.0 }, Out(1));
            Assert.Equal(new object[] { 4.0 }, Out(2));
            FireTick();
            Assert.Equal(new object[] { 4.0, 0.0 }, Out(2));
        }

        [Fact]
        public void Goto_Loops()
        {
            Run("set value1 = value1 + 1\nset AQ1 = value1\ngoto 1");
            Input(S(1), 1);
            Ticks(8);
            Assert.Equal(new object[] { 1.0, 2.0, 3.0 }, Out(1));
            Assert.Equal(1, Rule.ActiveSequence);
        }

        [Fact]
        public void IfElse()
        {
            const string program = "if AI1 >= 4\nset AQ1 = 1\nelse\nset AQ1 = 2\nendif\nset AQ2 = 9";
            Run(program);
            Input(AI(1), 5);
            Input(S(1), 1);
            Ticks(3);
            Assert.Equal(new object[] { 1.0 }, Out(1));
            Assert.Equal(new object[] { 9.0 }, Out(2));

            ClearRecorded();
            Input(AI(1), 1);
            Input(S(1), 1);
            Ticks(3);
            Assert.Equal(new object[] { 2.0 }, Out(1));
        }

        [Fact]
        public void If_WithoutElse_SkipsToEndif()
        {
            Run("if AI1 > 0\nset AQ1 = 1\nset AQ1 = 2\nendif\nset AQ2 = 3");
            Input(S(1), 1); // AI1 = 0: if is false
            FireTick();
            Assert.Empty(Out(1));
            Assert.Equal(new object[] { 3.0 }, Out(2));
        }

        [Fact]
        public void NestedIfs()
        {
            Run("if AI1 > 0\nif AI2 > 0\nset AQ1 = 1\nelse\nset AQ1 = 2\nendif\nendif\nset AQ2 = 1");
            Input(AI(1), 1);
            Input(S(1), 1);
            Ticks(5);
            Assert.Equal(new object[] { 2.0 }, Out(1));
            Assert.Equal(new object[] { 1.0 }, Out(2));
        }

        [Fact]
        public void StartSequence_And_Return()
        {
            Run("sequence Main\nset AQ1 = 1\nstartsequence Sub\nset AQ1 = 3\nsequence Sub\nset AQ2 = 2\nreturn\nset AQ2 = 99");
            Input(S(1), 1);
            FireTick(); // startsequence
            Assert.Equal(2, Last(Active));
            FireTick(); // AQ2 = 2
            FireTick(); // return
            Assert.Equal(1, Last(Active));
            FireTick(); // AQ1 = 3
            Assert.Equal(new object[] { 1.0, 3.0 }, Out(1));
            Assert.Equal(new object[] { 2.0 }, Out(2));
        }

        [Fact]
        public void CalledSequence_ReturnsAtItsEnd()
        {
            Run("sequence Main\nstartsequence 2\nset AQ1 = 1\nsequence Sub\nset AQ2 = 2");
            Input(S(1), 1);
            Ticks(3);
            Assert.Equal(new object[] { 2.0 }, Out(2));
            Assert.Equal(new object[] { 1.0 }, Out(1));
        }

        [Fact]
        public void Return_WithoutCaller_EndsTheSequence()
        {
            Run("set AQ1 = 1\nreturn\nset AQ1 = 2");
            Input(S(1), 1);
            Ticks(3);
            Assert.Equal(new object[] { 1.0 }, Out(1));
            Assert.Equal(0, Rule.ActiveSequence);
        }

        [Fact]
        public void SelectInput_StartsAndStops()
        {
            Run("sequence A\nset AQ1 = 1\ngoto 1\nsequence B\nset AQ1 = 2\ngoto 1");
            Input(SequenceControllerLogicFactory.RuleSelect, 2);
            Assert.Equal(2, Rule.ActiveSequence);
            Assert.Equal(new object[] { 2.0 }, Out(1));

            Input(SequenceControllerLogicFactory.RuleSelect, 0);
            Assert.Equal(0, Rule.ActiveSequence);
            Assert.False(SutTimer.Enabled);

            // an unknown number is ignored
            Input(SequenceControllerLogicFactory.RuleSelect, 7);
            Assert.Equal(0, Rule.ActiveSequence);
        }

        [Fact]
        public void Off_ResetsOutputs_AndLocks()
        {
            Run("set AQ1 = 5\nset TQ = \"on\"\ngoto 1");
            Input(S(1), 1);
            FireTick();
            Assert.Equal("on", Last(Text));

            Input(SequenceControllerLogicFactory.RuleOff, true);
            Assert.Equal(0.0, Last(AQ(1)));
            Assert.Equal("", Last(Text));
            Assert.Equal(0, Rule.ActiveSequence);

            Input(S(1), 1);
            Assert.Equal(0, Rule.ActiveSequence);

            Input(SequenceControllerLogicFactory.RuleOff, false);
            Input(S(1), 1);
            Assert.Equal(1, Rule.ActiveSequence);
        }

        [Fact]
        public void TextOutput_JoinsTextAndValues()
        {
            Run("set AQ2 = 27.5\nset TQ = \"Value of AQ2 is \" AQ2\nset TQ = AQ2 / 3");
            Input(S(1), 1);
            FireTick();
            Assert.Equal("Value of AQ2 is 27.5", Last(Text));
            FireTick();
            Assert.Equal("9.166667", Last(Text));
        }

        [Fact]
        public void SetInput_OverridesUntilTheLinkSendsAgain()
        {
            Run("set AI1 = 10\nset AQ1 = AI1");
            Input(S(1), 1);
            FireTick();
            Assert.Equal(new object[] { 10.0 }, Out(1));

            Input(AI(1), 3);
            Assert.Equal(3, Rule.Value("AI1"));
        }

        [Fact]
        public void SameValue_IsNotSentAgain_ButPulsesAre()
        {
            Run("set AQ1 = 1\nset AQ1 = 1\nset AQ1 = 1");
            Input(S(1), 1);
            Ticks(2);
            Assert.Equal(new object[] { 1.0 }, Out(1));
        }

        [Fact]
        public void NotANumber_IsNotWritten()
        {
            Run("set AQ1 = sqrt(-1)\nset AQ1 = 1 / 0\nset AQ1 = 2");
            Input(S(1), 1);
            Ticks(2);
            Assert.Equal(new object[] { 2.0 }, Out(1));
        }

        [Fact]
        public void InvalidLines_AreSkipped_AndReportedOnErr()
        {
            Run("set AQ1 = 1\nfly away\nset AQ1 = 2");
            Assert.Contains("unknown command", (string)Last(Errors));

            Input(S(1), 1);
            FireTick();
            Assert.Equal(new object[] { 1.0, 2.0 }, Out(1));
        }

        [Fact]
        public void ValidProgram_SendsEmptyErrors()
        {
            Run("set AQ1 = 1");
            Assert.Equal("", Last(Errors));
        }

        [Fact]
        public void NewProgram_StopsTheSequence_AndKeepsVariables()
        {
            Run("set level = 4\ngoto 1");
            Input(S(1), 1);
            Assert.Equal(4, Rule.Value("level"));

            // variables exist when the program assigns them somewhere
            Input(SequenceControllerLogicFactory.RuleParamProgram, "set AQ1 = level + 1\nset level = AQ1");
            Assert.Equal(0, Rule.ActiveSequence);
            Input(S(1), 1);
            Assert.Equal(new object[] { 5.0 }, Out(1));
        }

        [Fact]
        public void RestartingASequence_BeginsAtLineOne()
        {
            Run("set AQ1 = 1\nset AQ1 = 2\nset AQ1 = 3");
            Input(S(1), 1);
            FireTick();
            Input(S(1), 1);
            Assert.Equal(new object[] { 1.0, 2.0, 1.0 }, Out(1));
        }

        [Fact]
        public void BeforeStart_InputsDoNothing()
        {
            SetUtcNow(T0);
            Interface(SequenceControllerLogicFactory.RuleParamProgram).Value = "set AQ1 = 1";
            Input(S(1), 1);
            Assert.Empty(Out(1));
        }

        [Fact]
        public void LateTick_AfterStop_DoesNothing()
        {
            Run("set AQ1 = 1\nset AQ1 = 2");
            Input(S(1), 1);
            Stop();
            FireTick();
            Assert.Equal(new object[] { 1.0 }, Out(1));
        }

        [Fact]
        public void Line_ShowsTheWaitingLine()
        {
            Run("set AQ1 = 1\n\nwaitcondition AI1 > 0");
            Input(S(1), 1);
            FireTick();
            Assert.Equal(3, Rule.CurrentLine);
            Assert.Equal(3, Last(Line));
        }

        [Fact]
        public void EmptyProgram_CannotStart()
        {
            Run("");
            Input(S(1), 1);
            Assert.Equal(0, Rule.ActiveSequence);
        }
    }
}
