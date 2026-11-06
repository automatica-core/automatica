using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.DigitalToAnalog.StateToImpuls
{
    public class StateToImpulsRule : Automatica.Core.Logic.Logic
    {
        private const int DefaultDelay = 1000;

        private readonly ILogicContext _context;
        private int? _i1 = null;
        private int _delay;

        private readonly RuleInterfaceInstance _output1;

        public StateToImpulsRule(ILogicContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.RuleOutput);

            var paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.DelayParameter);

            _delay = ToDelay(paramDelay?.ValueInteger);
        }

        public int Delay => _delay;

        private static int ToDelay(object value)
        {
            if (value == null)
            {
                return DefaultDelay;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return DefaultDelay;
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            // parameters are pushed again on every (re)start with the current instance - the instance cached in the constructor would be stale after a reload
            if (instance.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.DelayParameter)
            {
                _delay = ToDelay(value);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.RuleInput)
            {
                _i1 = Convert.ToInt32(value);
            }

            if (_i1.HasValue && _i1.Value > 0)
            {
                // impulse: 1 now, 0 after the impulse time
                var delay = Delay;
                var on = new LogicOutputChanged(_output1, 1);
                var off = new LogicOutputChanged(_output1, 0);

                if (delay <= 0)
                {
                    // no impulse time - dispatch both here, so the 0 can not overtake the 1
                    _context.Dispatcher.DispatchValue(on.Instance, on.Value);
                    _context.Dispatcher.DispatchValue(off.Instance, off.Value);
                    return new List<ILogicOutputChanged>();
                }

                Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await _context.Dispatcher.DispatchValue(off.Instance, off.Value);
                });

                return new List<ILogicOutputChanged> { on };
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
