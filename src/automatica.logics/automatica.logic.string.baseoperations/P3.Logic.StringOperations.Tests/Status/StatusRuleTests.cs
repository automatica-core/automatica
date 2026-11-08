using System.Collections.Generic;
using P3.Logic.StringOperations.Status;
using P3.Logic.StringOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.StringOperations.Tests.Status
{
    public class StatusRuleTests : TextLogicTest<StatusLogicFactory>
    {
        private static readonly System.Guid Text = StatusLogicFactory.OutputText;
        private static readonly System.Guid Value = StatusLogicFactory.OutputValue;
        private static readonly System.Guid Rule = StatusLogicFactory.OutputRule;

        private void Rule1To3()
        {
            // a temperature status: hot, warm, cold, default "no value"
            Param(StatusLogicFactory.ParamDefaultText, "no value");
            Param(StatusLogicFactory.ParamDefaultIcon, "circle-question");
            Param(StatusLogicFactory.ParamCondition[0], "I1 >= 26");
            Param(StatusLogicFactory.ParamText[0], "Hot (<v1.1> °C)");
            Param(StatusLogicFactory.ParamIcon[0], "fire");
            Param(StatusLogicFactory.ParamColor[0], "#ff0000");
            Param(StatusLogicFactory.ParamCondition[1], "I1 >= 20");
            Param(StatusLogicFactory.ParamText[1], "Warm");
            Param(StatusLogicFactory.ParamValue[1], 42);
            Param(StatusLogicFactory.ParamCondition[2], "I1 < 20");
            Param(StatusLogicFactory.ParamText[2], "Cold");
            Start();
        }

        [Fact]
        public void FirstTrueRuleWins()
        {
            Rule1To3();

            Input(StatusLogicFactory.Inputs[0], 27.26);
            Assert.Equal("Hot (27.3 °C)", Last(Text));
            Assert.Equal(1.0, Last(Value)); // default value of rule 1 is its number
            Assert.Equal(1, Last(Rule));

            Input(StatusLogicFactory.Inputs[0], 22);
            Assert.Equal("Warm", Last(Text));
            Assert.Equal(42.0, Last(Value));
            Assert.Equal(2, Last(Rule));

            Input(StatusLogicFactory.Inputs[0], 5);
            Assert.Equal("Cold", Last(Text));
            Assert.Equal(3, Last(Rule));
        }

        [Fact]
        public void NoRule_DefaultStatus()
        {
            Rule1To3();

            Input(StatusLogicFactory.Inputs[0], "sensor error");
            Assert.Equal("no value", Last(Text));
            Assert.Equal(0.0, Last(Value));
            Assert.Equal(0, Last(Rule));
        }

        [Fact]
        public void OnlyChangedOutputsAreSent()
        {
            Rule1To3();

            Assert.Equal(3, Input(StatusLogicFactory.Inputs[0], 21).Count);
            // same rule, same text and value: nothing
            Assert.Empty(Input(StatusLogicFactory.Inputs[0], 23));
            // the text has the value in it: only the text changes
            Input(StatusLogicFactory.Inputs[0], 30);
            ClearRecorded();
            var changes = Input(StatusLogicFactory.Inputs[0], 31);
            Assert.Single(changes);
            Assert.Equal("Hot (31.0 °C)", Last(Text));
        }

        [Fact]
        public void NoBurstOfTextsAtStartup()
        {
            Rule1To3();
            Assert.Empty(Values(Text));
        }

        [Fact]
        public void ChangedRule_AppliesAtOnce()
        {
            Rule1To3();
            Input(StatusLogicFactory.Inputs[0], 22);
            Assert.Equal("Warm", Last(Text));

            ChangeParam(StatusLogicFactory.ParamText[1], "Pleasant");
            Assert.Equal("Pleasant", Last(Text));

            ChangeParam(StatusLogicFactory.ParamCondition[0], "I1 >= 21");
            Assert.Equal("Hot (22.0 °C)", Last(Text));
        }

        [Fact]
        public void InvalidRule_IsSkipped()
        {
            Param(StatusLogicFactory.ParamCondition[0], "I1 >>> 5");
            Param(StatusLogicFactory.ParamText[0], "broken");
            Param(StatusLogicFactory.ParamCondition[1], "I1 > 5");
            Param(StatusLogicFactory.ParamText[1], "big");
            Start();

            Input(StatusLogicFactory.Inputs[0], 10);
            Assert.Equal("big", Last(Text));
        }

        [Fact]
        public void SeveralInputs_AndPlaceholders()
        {
            Param(StatusLogicFactory.ParamCondition[0], "I1 == 1 && I2 *= Alarm");
            Param(StatusLogicFactory.ParamText[0], "<v2> in <v3>");
            Start();

            Input(StatusLogicFactory.Inputs[1], "Water Alarm");
            Input(StatusLogicFactory.Inputs[2], "Basement");
            Assert.Equal(0, Last(Rule));
            Input(StatusLogicFactory.Inputs[0], true);
            Assert.Equal("Water Alarm in Basement", Last(Text));
        }

        [Fact]
        public void VisuData_HasIconAndColorOfTheRule()
        {
            Rule1To3();
            Input(StatusLogicFactory.Inputs[0], 30);

            var status = Data<Dictionary<string, object>>("status");
            Assert.Equal(1, status["rule"]);
            Assert.Equal("fire", status["icon"]);
            Assert.Equal("#ff0000", status["color"]);
            Assert.Equal("Hot (30.0 °C)", status["text"]);

            Input(StatusLogicFactory.Inputs[0], "x");
            status = Data<Dictionary<string, object>>("status");
            Assert.Equal("circle-question", status["icon"]);
        }

        [Fact]
        public void Templates_HaveInputsOutputsAndParameters()
        {
            Assert.Equal(8, StatusLogicFactory.Inputs.Length);
            Assert.NotNull(Interface(StatusLogicFactory.ParamCondition[7]));
            Assert.NotNull(Interface(StatusLogicFactory.ParamColor[7]));
            Assert.Equal("rule8Condition", Interface(StatusLogicFactory.ParamCondition[7]).This2RuleInterfaceTemplateNavigation.Key);
            Assert.Equal("rule3Icon", Interface(StatusLogicFactory.ParamIcon[2]).This2RuleInterfaceTemplateNavigation.Key);
        }
    }
}
