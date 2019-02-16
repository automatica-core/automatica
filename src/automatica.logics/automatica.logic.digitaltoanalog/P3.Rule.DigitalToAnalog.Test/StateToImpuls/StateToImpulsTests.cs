using System.Linq;
using System.Threading;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Rules;
using P3.Rule.DigitalToAnalog.StateToImpuls;
using Xunit;

namespace P3.Rule.DigitalToAnalog.Test.StateToImpuls
{
    public class StateToImpulsTests : RuleTest<StateToImpulsRuleFactory>
    {
        //[Fact]
        //public void TestRule()
        //{
        //    DispatcherMock.Instance.Clear();

        //    Rule.ValueChanged(GetRuleInterfaceByTemplate(StateToImpulsRuleFactory.RuleInput), Dispatchable, 1);
        //    Assert.Equal(0, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).Count);

        //    Thread.Sleep(1200);

        //    Assert.Equal(1, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).Count);
        //    Assert.Equal(0, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).First().Value);


        //}
        //[Fact]
        //public void TestRule2()
        //{
        //    var param = GetRuleInterfaceByTemplate(StateToImpulsRuleFactory.DelayParameter);
        //    param.Value = 2000;

        //    DispatcherMock.Instance.Clear();

        //    Rule.ValueChanged(GetRuleInterfaceByTemplate(StateToImpulsRuleFactory.RuleInput), Dispatchable, 1);
        //    Assert.Equal(0, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).Count);

        //    Thread.Sleep(1000);
        //    Assert.Equal(0, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).Count);
        //    Thread.Sleep(1500);

        //    Assert.Equal(1, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).Count);
        //    Assert.Equal(0, DispatcherMock.Instance.GetValues(DispatchableType.RuleInstance).First().Value);


        //}
    }
}
