using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.Math.BasicOperations.Addition;
using Xunit;

namespace P3.Rule.Math.BasicOperations.Tests.Addition
{
    public class AdditionTests : RuleTest<AdditionRuleFactory>
    {
        [Fact]
        public void TestAdditionRule()
        {
            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), 1);
            Assert.Equal(1.0, Context.Dispatcher.GetValues(DispatchableType.RuleInstance).Values.First());

            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput2), 10);
            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput3),  1);
            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput4),  10);
            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1),  0);


            RuleInputChanged(GetRuleInterfaceByTemplate(AdditionRuleFactory.RuleInput1), null);
            Assert.Equal(21.0, Context.Dispatcher.GetValues(DispatchableType.RuleInstance).Values.First());
        }
    }
}
