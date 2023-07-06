using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.DigitalToAnalog.StateToImpuls
{
    public class StateToImpulsRule : Automatica.Core.Logic.Logic
    {
        private readonly ILogicContext _context;
        private int? _i1 = null;
        
        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _paramDelay;
        
        public StateToImpulsRule(ILogicContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.RuleOutput);


            _paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.DelayParameter);

        }

        public int Delay
        {
            get
            {
                if (_paramDelay != null && _paramDelay.ValueInteger.HasValue)
                {
                    return (int)_paramDelay.ValueInteger.Value;
                }

                return 1000;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == StateToImpulsLogicFactory.RuleInput)
            {
                _i1 = Convert.ToInt32(value);
            }

            if (_i1.HasValue && _i1.Value > 0)
            {
                Task.Run(() =>
                {
                    var change = new LogicOutputChanged(_output1, 0);
                    Thread.Sleep(Delay);
                    _context.Dispatcher.DispatchValue(change.Instance, change.Value);
                });
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
