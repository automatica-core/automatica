using System;
using P3.Logic.StringOperations.Tests.TestSupport;
using P3.Logic.StringOperations.TextGenerator;
using Xunit;

namespace P3.Logic.StringOperations.Tests.TextGenerator
{
    public class TextGeneratorTests : TextLogicTest<TextGeneratorLogicFactory>
    {
        private static Guid Out(int i) => TextGeneratorLogicFactory.Outputs[i];
        private static Guid V(int n) => TextGeneratorLogicFactory.InputValues[n - 1];

        private void Texts(int delayMs = 0, int intervalS = 0)
        {
            Param(TextGeneratorLogicFactory.ParamDelay, delayMs);
            Param(TextGeneratorLogicFactory.ParamInterval, intervalS);
            Param(TextGeneratorLogicFactory.ParamTexts[0], "Outside <v1.1> °C, wind <v2> km/h");
            Param(TextGeneratorLogicFactory.ParamTexts[2], "<v3>!");
            Start();
        }

        private void Trigger(bool on) => Input(TextGeneratorLogicFactory.InputTrigger, on);

        [Fact]
        public void RisingEdge_GeneratesTheTextsWithTemplates()
        {
            Texts();
            Input(V(1), 4.26);
            Input(V(2), 12);
            Input(V(3), "Storm");
            Assert.Empty(Values(Out(0)));

            Trigger(true);
            Assert.Equal("Outside 4.3 °C, wind 12 km/h", Last(Out(0)));
            Assert.Equal("Storm!", Last(Out(2)));
            // outputs without a template stay untouched
            Assert.Empty(Values(Out(1)));
            Assert.Empty(Values(Out(4)));
        }

        [Fact]
        public void FallingEdge_ClearsTheTexts()
        {
            Texts();
            Input(V(3), "x");
            Trigger(true);
            Trigger(false);
            Assert.Equal("", Last(Out(0)));
            Assert.Equal("", Last(Out(2)));
        }

        [Fact]
        public void ValueChanges_DoNotGenerate()
        {
            Texts();
            Trigger(true);
            ClearRecorded();
            Input(V(1), 10);
            Assert.Empty(Values(Out(0)));
            // the trigger staying on is no edge
            Trigger(true);
            Assert.Empty(Values(Out(0)));
        }

        [Fact]
        public void Delay_GeneratesOnTheTimer()
        {
            Texts(delayMs: 250);
            Input(V(1), 1);
            Trigger(true);
            Assert.Empty(Values(Out(0)));
            Assert.True(SutTimer.Enabled);
            Assert.Equal(250, SutTimer.Interval);

            // a value set during the delay is in the text
            Input(V(1), 2);
            FireTick();
            Assert.Equal("Outside 2.0 °C, wind  km/h", Last(Out(0)));
        }

        [Fact]
        public void Delay_FallingEdgeBeforeTheTimer_GeneratesNothing()
        {
            Texts(delayMs: 250);
            Trigger(true);
            Trigger(false);
            FireTick();
            Assert.Equal(new object[] { "" }, Values(Out(0)));
        }

        [Fact]
        public void Interval_UpdatesWhileOn()
        {
            Texts(intervalS: 60);
            Input(V(1), 1);
            Trigger(true);
            Assert.Equal(60000, SutTimer.Interval);
            Assert.True(SutTimer.Enabled);

            Input(V(1), 2);
            FireTick();
            Assert.Equal("Outside 2.0 °C, wind  km/h", Last(Out(0)));

            Trigger(false);
            Assert.False(SutTimer.Enabled);
            FireTick();
            Assert.Equal("", Last(Out(0)));
        }

        [Fact]
        public void BeforeStart_TriggerIsIgnored()
        {
            Param(TextGeneratorLogicFactory.ParamTexts[0], "x");
            Trigger(true);
            Assert.Empty(Values(Out(0)));
        }
    }
}
