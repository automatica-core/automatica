using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Selection
{
    /// <summary>
    /// Sequencer (Loxone "Sequencer"): every 1 on Tr switches on the next of the outputs O1..Max, after Max it starts at O1 again.
    /// P selects an output directly (0 = all off), 1 on R activates the default output (0 = all off).
    /// While DisPc is 1, Tr and P are ignored (child lock). Sel is the number of the active output.
    /// </summary>
    public class SequencerRule : Automatica.Core.Logic.Logic
    {
        private readonly OneHotOutputs _outputs;

        private int _max = SequencerLogicFactory.DefaultMax;
        private int _default = SequencerLogicFactory.DefaultValue;
        private bool _disabled;

        public SequencerRule(ILogicContext context) : base(context)
        {
            _outputs = new OneHotOutputs(context, SequencerLogicFactory.RuleOutputs, SequencerLogicFactory.RuleSelected);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == SequencerLogicFactory.RuleParamMax)
            {
                _max = System.Math.Clamp(OneHotOutputs.ToIndex(value) ?? SequencerLogicFactory.DefaultMax, 0, _outputs.Count);
            }
            else if (instance.This2RuleInterfaceTemplate == SequencerLogicFactory.RuleParamDefault)
            {
                _default = OneHotOutputs.ToIndex(value) ?? SequencerLogicFactory.DefaultValue;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == SequencerLogicFactory.RuleDisable)
            {
                _disabled = Helper.ConvertValueToBool(value);
            }
            else if (template == SequencerLogicFactory.RuleReset)
            {
                if (Helper.ConvertValueToBool(value))
                {
                    return _outputs.Select(_default >= 0 && _default <= _max ? _default : 0);
                }
            }
            else if (_disabled)
            {
                return new List<ILogicOutputChanged>();
            }
            else if (template == SequencerLogicFactory.RuleTrigger)
            {
                if (Helper.ConvertValueToBool(value) && _max > 0)
                {
                    var next = _outputs.Selected + 1;
                    return _outputs.Select(next > _max ? 1 : next);
                }
            }
            else if (template == SequencerLogicFactory.RulePosition)
            {
                var index = OneHotOutputs.ToIndex(value);
                if (index.HasValue && index.Value >= 0 && index.Value <= _max)
                {
                    return _outputs.Select(index.Value);
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
