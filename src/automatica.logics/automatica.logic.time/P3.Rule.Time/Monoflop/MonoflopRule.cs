using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace P3.Rule.Time.Monoflop
{
    public class MonoflopRule : Automatica.Core.Rule.Rule
    {
        private readonly RuleInterfaceInstance _delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;

        public MonoflopRule(IRuleContext context) : base(context)
        {
            _delay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MonoflopRuleFactory.RuleParamDelay);

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MonoflopRuleFactory.RuleOutput);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            return base.Stop();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, false);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {   
            _timer.Stop();
            _timer.Interval = _delay.ValueInteger.Value * 1000;
            _timer.Start();


            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, true);

            return new List<IRuleOutputChanged>();
        }
    }
}
