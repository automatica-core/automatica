using Automatica.Core.UnitTests.Base.Rules;
using Automatica.Core.UnitTests.Rules;
using Xunit;
using P3.Rule.Math.BasicOperations.Floor;

namespace P3.Rule.Math.BasicOperations.Test.Floor
{
    
    public class FloorTests : RuleTest<FloorRuleFactory>
    {
        [Fact]
        public void TestFloorRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(FloorRuleFactory.RuleInput1), Dispatchable, 1.1000)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(FloorRuleFactory.RuleInput1), Dispatchable, null)[0].ValueInteger == 1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(FloorRuleFactory.RuleInput1), Dispatchable, 9.923112)[0].ValueInteger == 9);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(FloorRuleFactory.RuleInput1), Dispatchable, -5.923112)[0].ValueInteger == -6);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(FloorRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == FloorRuleFactory.RuleOutput);
        }
    }
}
