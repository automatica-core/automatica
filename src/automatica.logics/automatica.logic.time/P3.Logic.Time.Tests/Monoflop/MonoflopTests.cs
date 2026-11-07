using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.Monoflop;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Time.Tests.Monoflop
{
    public class MonoflopTests: LogicTest<MonoflopLogicFactory>
    {
        private object Current()
        {
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            return values.Count == 1 ? values.First().Value.Value : null;
        }

        /// <summary>
        /// lets the running pulse timer elapse now instead of waiting the configured seconds
        /// </summary>
        private void ElapseNow()
        {
            var timer = (System.Threading.Timer)Logic.GetType().GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Logic);
            Assert.NotNull(timer);
            timer.Change(0, Timeout.Infinite);
        }

        private long Delay => (long)Logic.GetType().GetField("_delay", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Logic);

        [Fact]
        public async Task TestMonoflopRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();


            await Logic.Start();
            Logic.ValueChanged(GetLogicInterfaceByTemplate(MonoflopLogicFactory.RuleTrigger), Dispatchable, true);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);

            // default delay 5 s
            Assert.Equal(5, Delay);
            ElapseNow();
            await Poll.UntilAsync(() => Equals(false, Current()));

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }

        [Fact]
        public async Task TestMonoflopRule2()
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

            Assert.Equal(1, Delay);
            ElapseNow();
            await Poll.UntilAsync(() => Equals(false, Current()));

            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }
    }
}
