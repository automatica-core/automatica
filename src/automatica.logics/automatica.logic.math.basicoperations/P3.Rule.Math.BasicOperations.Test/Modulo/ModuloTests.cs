using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Math.BasicOperations.Modulo;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Tests.Modulo
{
    
    public class ModuloTests : RuleTest<ModuloRuleFactory>
    {
        [Fact]
        public void TestModuloRule()
        {
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput1), Dispatchable, 2)[0].Value == null);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput1), Dispatchable, 2)[1].Value == null);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput2), Dispatchable, 5)[0].ValueDouble == 2.0);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput2), Dispatchable, 5)[1].ValueInteger == 2);

            Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput1), Dispatchable, 2.3);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput2), Dispatchable, 5.3)[0].ValueDouble == 2.3);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput2), Dispatchable, 5.3)[1].ValueInteger == 2);

            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput1), Dispatchable, 1)[0].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleOutput1);
            Assert.True(Rule.ValueChanged(GetRuleInterfaceByTemplate(ModuloRuleFactory.RuleInput1), Dispatchable, 1)[1].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleOutput2);
        }
    }
}
