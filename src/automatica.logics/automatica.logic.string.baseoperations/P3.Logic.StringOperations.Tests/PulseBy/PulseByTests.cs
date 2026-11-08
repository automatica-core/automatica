using P3.Logic.StringOperations.PulseBy;
using P3.Logic.StringOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.StringOperations.Tests.PulseBy
{
    public class PulseByTests : TextLogicTest<PulseByLogicFactory>
    {
        private static readonly System.Guid P = PulseByLogicFactory.OutputPulse;

        private PulseByRule Rule => (PulseByRule)Sut;

        private void Text(string text) => Assert.Empty(Input(PulseByLogicFactory.InputText, text));

        [Theory]
        // the Loxone example
        [InlineData("???nder*", "grinder", true)]
        [InlineData("???nder*", "thunderstorm", true)]
        [InlineData("???nder*", "under", false)]
        // contains
        [InlineData("rain", "light rain today", true)]
        [InlineData("r?in", "light rain", true)]
        [InlineData("Alarm*zone 2", "Alarm in zone 2", true)]
        [InlineData("Alarm*zone 2", "zone 2 Alarm", false)]
        // regex characters are plain text
        [InlineData("a.b", "axb", false)]
        [InlineData("(1)", "call (1)", true)]
        [InlineData("RAIN", "rain", false)]
        public void Wildcards(string pattern, string text, bool expected)
        {
            Assert.Equal(expected, PulseByRule.Contains(text, pattern, false));
        }

        [Fact]
        public void IgnoreCase()
        {
            Assert.True(PulseByRule.Contains("light RAIN", "rain", true));
        }

        [Fact]
        public void Match_PulsesForTheDuration()
        {
            Param(PulseByLogicFactory.ParamPatterns[1], "door*open");
            Param(PulseByLogicFactory.ParamDuration, 2.5);
            Start();

            Text("window open");
            Assert.Empty(Values(P));

            Text("front door is open");
            Assert.Equal(new object[] { true }, Values(P));
            Assert.Equal(2500, SutTimer.Interval);
            Assert.True(SutTimer.Enabled);

            FireTick();
            Assert.Equal(new object[] { true, false }, Values(P));
            Assert.False(Rule.Pulsing);
        }

        [Fact]
        public void MatchDuringThePulse_ExtendsIt()
        {
            Param(PulseByLogicFactory.ParamPatterns[0], "bell");
            Start();
            Text("bell");
            Text("bell");
            Assert.Equal(new object[] { true }, Values(P));
            FireTick();
            Assert.Equal(new object[] { true, false }, Values(P));
        }

        [Fact]
        public void ZeroDuration_PulsesAtOnce()
        {
            Param(PulseByLogicFactory.ParamPatterns[3], "x");
            Param(PulseByLogicFactory.ParamDuration, 0);
            Start();
            Text("x");
            Assert.Equal(new object[] { true, false }, Values(P));
        }

        [Fact]
        public void NoPatterns_NoPulse()
        {
            Start();
            Text("anything");
            Assert.Empty(Values(P));
        }

        [Fact]
        public void IgnoreCaseParameter()
        {
            Param(PulseByLogicFactory.ParamPatterns[0], "ALARM");
            Param(PulseByLogicFactory.ParamIgnoreCase, true);
            Start();
            Text("alarm");
            Assert.Equal(new object[] { true }, Values(P));
        }

        [Fact]
        public void BeforeStart_Ignored()
        {
            Param(PulseByLogicFactory.ParamPatterns[0], "x");
            Text("x");
            Assert.Empty(Values(P));
        }
    }
}
