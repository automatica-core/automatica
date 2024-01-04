using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.Monoflop;
using Xunit;

namespace P3.Logic.Time.Tests.Monoflop
{
    public class MonoflopTests: LogicTest<MonoflopLogicFactory>
    {
        [Fact]
        public async void TestMonoflopRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();


            await Logic.Start();
            Logic.ValueChanged(GetLogicInterfaceByTemplate(MonoflopLogicFactory.RuleTrigger), Dispatchable, true);

            await Task.Delay(2000);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Equal(1, values.Count);
            Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(3500);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }

        [Fact]
        public async void TestMonoflopRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();
            
            var paramDelay = GetLogicInterfaceByTemplate(MonoflopLogicFactory.RuleParamDelay);
            paramDelay.Value = 1;

            await Logic.Start();
          
            Logic.ValueChanged(GetLogicInterfaceByTemplate(MonoflopLogicFactory.RuleTrigger), Dispatchable, true);

            
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);


            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);

            await Task.Delay(1600);

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }
    }
}
