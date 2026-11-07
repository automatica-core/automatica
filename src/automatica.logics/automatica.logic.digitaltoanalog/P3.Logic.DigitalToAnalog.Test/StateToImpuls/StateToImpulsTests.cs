using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.DigitalToAnalog.StateToImpuls;
using Xunit;

// the tests share the static dispatcher mock
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.DigitalToAnalog.Tests.StateToImpuls
{
    public class StateToImpulsTests: LogicTest<StateToImpulsLogicFactory>
    {
        [Fact]
        public async Task TestRule()
        {
            await Context.Dispatcher.ClearValues();
            GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.DelayParameter).Value = 100;
            await Logic.Start();

            // the impulse starts right away with 1 ...
            var value = Logic.ValueChanged(GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.RuleInput), Dispatchable, 1);

            Assert.Single(value);
            Assert.Equal(1, value.First().Value);

            // ... and ends with 0 after the impulse time
            var output = GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.RuleOutput);
            await TestSupport.Poll.UntilAsync(() => Equals(Context.Dispatcher.GetValue(DispatchableType.RuleInstance, output.ObjId)?.Value, 0));

            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(0, values[output.ObjId].Value);
        }

        [Fact]
        public void TestRule_ZeroInputNoImpulse()
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.RuleInput), Dispatchable, 0));
        }
    }
}
