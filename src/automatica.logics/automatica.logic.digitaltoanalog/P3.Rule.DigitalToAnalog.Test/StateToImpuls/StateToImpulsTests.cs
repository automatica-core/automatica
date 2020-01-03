using System.Linq;
using System.Threading;
using Automatica.Core.UnitTests.Base.Rules;
using P3.Rule.DigitalToAnalog.StateToImpuls;
using Xunit;

namespace P3.Rule.DigitalToAnalog.Tests.StateToImpuls
{
    public class StateToImpulsTests : RuleTest<StateToImpulsRuleFactory>
    {
        [Fact]
        public void TestRule()
        {
            var value = Rule.ValueChanged(GetRuleInterfaceByTemplate(StateToImpulsRuleFactory.RuleInput), Dispatchable, 1);
            Assert.Equal(0, value[0].ValueInteger);

            Thread.Sleep(1200);

            Assert.Equal(1, value.Count);
            Assert.Equal(0, value.First().Value);


        }
    }
}
