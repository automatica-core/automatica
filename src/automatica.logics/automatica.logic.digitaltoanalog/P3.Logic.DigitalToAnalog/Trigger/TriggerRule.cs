using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.DigitalToAnalog.Trigger
{
    public class TriggerRule : Automatica.Core.Logic.Logic
    {
        private readonly ILogicContext _context;

        private object _outputValue;
        private object _paramValue;
        private int _delay;

        private readonly RuleInterfaceInstance _output1;

        public TriggerRule(ILogicContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleOutput);

            var paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.DelayParameter);
            _delay = ToDelay(paramDelay?.Value);

            var paramValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.ValueParameter);

            if (paramValue != null)
            {
                _paramValue = ParseParameterValue(paramValue.Value);
                _outputValue = _paramValue;
            }
        }

        public int Delay => _delay;

        private static int ToDelay(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// The value parameter is a text parameter - a number in it is emitted as number, not as string
        /// </summary>
        internal static object ParseParameterValue(object value)
        {
            if (value is string stringValue && double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            return value;
        }

        private static bool HasValue(object value)
        {
            return value != null && !(value is string s && String.IsNullOrEmpty(s));
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            // parameters are pushed again on every (re)start with the current instance - the instances cached in the constructor would be stale after a reload
            if (instance.This2RuleInterfaceTemplate == TriggerLogicFactory.DelayParameter)
            {
                _delay = ToDelay(value);
            }
            else if (instance.This2RuleInterfaceTemplate == TriggerLogicFactory.ValueParameter)
            {
                var paramValue = ParseParameterValue(value);

                // only a changed parameter overrides a value that was set through the V input
                if (!Equals(paramValue, _paramValue))
                {
                    _paramValue = paramValue;
                    _outputValue = paramValue;
                }
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleValueInput)
            {
                Context.Logger.LogDebug($"{Context.RuleInstance.Name} Change value param to {value}");
                _outputValue = value;
                return new List<ILogicOutputChanged>();
            }

            if (value != null && instance.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleInput && Boolean.TryParse(value.ToString(), out bool bValue) && bValue)
            {
                if (!HasValue(_outputValue))
                {
                    Context.Logger.LogError($"{Context.RuleInstance.Name} OutputValue is not set, we ignore the trigger..");
                    return new List<ILogicOutputChanged>();
                }

                var change = new LogicOutputChanged(_output1, _outputValue);
                Context.Logger.LogDebug($"{Context.RuleInstance.Name} Change output value to {_outputValue}");
                var delay = Delay;
                if (delay > 0)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        await _context.Dispatcher.DispatchValue(change.Instance, change.Value);
                    });
                }
                else
                {
                    return new List<ILogicOutputChanged> {change};
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
