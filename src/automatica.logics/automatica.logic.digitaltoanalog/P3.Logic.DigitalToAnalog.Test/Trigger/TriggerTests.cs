using System.Linq;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.DigitalToAnalog.Trigger;
using Xunit;

namespace P3.Logic.DigitalToAnalog.Tests.Trigger
{
    public class TriggerTests: LogicTest<TriggerLogicFactory>
    {
        [Fact]
        public async void TestRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleValueInput), Dispatchable, 10);
            var values = Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, true);

            Assert.Equal(1, values.Count);
            Assert.Equal(10, values.First().Value);
        }
    }
}
