using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.StringOperations.Common
{
    /// <summary>
    /// Base of the text / status blocks: keeps the parameter values by template id and finds the outputs
    /// </summary>
    public abstract class TextLogicBase : Automatica.Core.Logic.Logic
    {
        private readonly Dictionary<Guid, object> _parameters = new();
        private readonly Dictionary<Guid, RuleInterfaceInstance> _outputs = new();

        protected TextLogicBase(ILogicContext context) : base(context)
        {
        }

        /// <summary>
        /// true between start and stop; parameter changes before the start only configure the block
        /// </summary>
        protected bool Started { get; private set; }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            Started = true;
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            Started = false;
            return base.Stop(instance, token);
        }

        protected RuleInterfaceInstance Output(Guid template)
        {
            if (!_outputs.TryGetValue(template, out var output) || output == null)
            {
                output = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == template);
                _outputs[template] = output;
            }
            return output;
        }

        /// <summary>
        /// Sends a value outside of an input change (timers, parameter changes)
        /// </summary>
        protected void Dispatch(Guid template, object value)
        {
            var output = Output(template);
            if (output != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
            }
        }

        protected void Dispatch(IEnumerable<ILogicOutputChanged> changes)
        {
            foreach (var change in changes)
            {
                Context.Dispatcher.DispatchValue(change.Instance, change.Value);
            }
        }

        protected ILogicOutputChanged Changed(Guid template, object value)
        {
            var output = Output(template);
            return output == null ? null : new LogicOutputChanged(output, value);
        }

        protected object Parameter(Guid template)
        {
            if (_parameters.TryGetValue(template, out var value))
            {
                return value;
            }
            // not reported yet (before the start): the stored value of the instance
            return Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == template)?.Value;
        }

        protected string ParameterText(Guid template) => Parameter(template)?.ToString() ?? string.Empty;

        protected double ParameterDouble(Guid template, double fallback) => TextValue.ToDouble(Parameter(template), fallback);

        protected bool ParameterBool(Guid template) => TextValue.IsTruthy(Parameter(template));

        protected sealed override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            _parameters[instance.This2RuleInterfaceTemplate] = value;
            base.ParameterValueChanged(instance, source, value);
            OnParameterChanged(instance.This2RuleInterfaceTemplate, value);
        }

        /// <summary>
        /// A parameter has a new value (also once per parameter on start)
        /// </summary>
        protected virtual void OnParameterChanged(Guid template, object value)
        {
        }

        protected static IList<ILogicOutputChanged> NoChanges() => new List<ILogicOutputChanged>();
    }
}
