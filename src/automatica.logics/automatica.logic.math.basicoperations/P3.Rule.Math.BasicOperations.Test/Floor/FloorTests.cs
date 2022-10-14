using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Math.BasicOperations.Floor;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Tests.Floor
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
