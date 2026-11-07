using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.FlipFlop
{
    /// <summary>
    /// Set/reset memory. S=1 sets the output, R=1 resets it, S=R=0 keeps it.
    /// When S and R are 1 at the same time the dominant input wins (RS: reset, SR: set).
    /// </summary>
    public class FlipFlopRule : Automatica.Core.Logic.Logic
    {
        private readonly Guid _setTemplate;
        private readonly Guid _resetTemplate;
        private readonly bool _setDominant;
        private readonly RuleInterfaceInstance _output;

        private bool _set;
        private bool _reset;
        private bool _q;

        public FlipFlopRule(ILogicContext context, Guid setTemplate, Guid resetTemplate, Guid outputTemplate, bool setDominant) : base(context)
        {
            _setTemplate = setTemplate;
            _resetTemplate = resetTemplate;
            _setDominant = setDominant;
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == outputTemplate);
        }

        internal bool State => _q;

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == _setTemplate)
            {
                _set = Helper.ConvertValueToBool(value);
            }
            else if (instance.This2RuleInterfaceTemplate == _resetTemplate)
            {
                _reset = Helper.ConvertValueToBool(value);
            }
            else
            {
                return new List<ILogicOutputChanged>();
            }

            if (_set && _reset)
            {
                _q = _setDominant;
            }
            else if (_set)
            {
                _q = true;
            }
            else if (_reset)
            {
                _q = false;
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, _q));
        }
    }
}
