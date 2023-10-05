using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.RangeCircular
{
    public class RangeCircularControl : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _value;
        public RangeCircularControl(ILogicContext context) : base(context)
        {
            _value = context.RuleInstance.RuleInterfaceInstance.First(
                a => a.This2RuleInterfaceTemplateNavigation.Key == "value");
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(true);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.ObjId == _value.ObjId)
            {
                var ret = new List<ILogicOutputChanged>();
                ret.Add(new LogicOutputChanged(_value, value));
                return ret;
            }

            return base.InputValueChanged(instance, source, value);
        }


        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(true);
        }
    }
}
