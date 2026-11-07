using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Time.DelayedOff;
using P3.Logic.Time.Tests.TestSupport;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Time.Tests.DelayedOff
{
    public class DelayedOffTests: LogicTest<DelayedOffLogicFactory>
    {
        private object Current()
        {
            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            return values.Count == 1 ? values.First().Value.Value : null;
        }

        private System.Timers.Timer LogicTimer =>
            (System.Timers.Timer)Logic.GetType().GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Logic);

        /// <summary>
        /// lets the running off timer elapse now instead of waiting the configured seconds
        /// </summary>
        private void ElapseNow()
        {
            // one shot - with AutoReset a 1 ms timer can elapse twice before the handler stops it
            LogicTimer.AutoReset = false;
            LogicTimer.Interval = 1;
        }

        [Fact]
        public async Task TestDelayedOffRule()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            // default delay 5 s
            Assert.Equal(5000, LogicTimer.Interval);
            ElapseNow();
            await Poll.UntilAsync(() => Equals(false, Current()));

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }

        [Fact]
        public async Task TestDelayedOffRule2()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleParamDelay);
            paramDelay.Value = 1;
            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            Assert.Equal(1000, LogicTimer.Interval);
            ElapseNow();
            await Poll.UntilAsync(() => Equals(false, Current()));

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);
        }


        [Fact]
        public async Task TestDelayedOffRule_DelayZeroSwitchesOffImmediately()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleParamDelay);
            paramDelay.Value = 0;
            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(false, values.First().Value.Value);

            await Task.Delay(200);
            values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);
            Assert.Equal(false, values.First().Value.Value);
        }

        [Fact]
        public async Task TestDelayedOffRuleReset()
        {
            await Context.Dispatcher.ClearValues();
            await Context.Dispatcher.ClearRegistrations();

            var paramDelay = GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleParamDelay);
            paramDelay.Value = 2;

            await Logic.Start();

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleTrigger), Dispatchable, true);

            Logic.ValueChanged(GetLogicInterfaceByTemplate(DelayedOffLogicFactory.RuleReset), Dispatchable, true);

            // the reset stopped the timer - shortening it must not start it again
            Assert.False(LogicTimer.Enabled);
            ElapseNow();
            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(Automatica.Core.Base.IO.DispatchableType.RuleInstance);

            Assert.Single(values);
            Assert.Equal(true, values.First().Value.Value);
        }
    }
}