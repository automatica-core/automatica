using System.Linq;
using System.Threading.Tasks;
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

        [Fact]
        public async Task TestRule_NumericTextParameterIsEmittedAsNumber()
        {
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = "50.5";
            await Logic.Start();

            var values = Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, true);

            Assert.Single(values);
            Assert.Equal(50.5, values.First().Value);
        }

        [Fact]
        public async Task TestRule_TextParameterStaysText()
        {
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).Value = "on";
            await Logic.Start();

            var values = Logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, true);

            Assert.Single(values);
            Assert.Equal("on", values.First().Value);
        }

        [Fact]
        public void TestRule_ValueInputSetsMissingValue()
        {
            // no value parameter at all - the V input must still be able to set the value
            GetLogicInterfaceByTemplate(TriggerLogicFactory.ValueParameter).ValueString = null;
            var logic = new TriggerRule(Context);

            Assert.Empty(logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, true));

            logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleValueInput), Dispatchable, 7);
            var values = logic.ValueChanged(GetLogicInterfaceByTemplate(TriggerLogicFactory.RuleInput), Dispatchable, true);

            Assert.Single(values);
            Assert.Equal(7, values.First().Value);
        }
    }
}
