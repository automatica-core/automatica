using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Threshold
{
    /// <summary>
    /// Switch with hysteresis (two-position controller).
    /// ON threshold above OFF threshold: 1 from V >= ON, 0 from V &lt;= OFF (e.g. fan, cooling).
    /// ON threshold below OFF threshold: 1 from V &lt;= ON, 0 from V >= OFF (e.g. heating).
    /// Between the thresholds the output keeps its state.
    /// </summary>
    public class ThresholdSwitchRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal double OnThreshold = ThresholdSwitchLogicFactory.DefaultOn;
        internal double OffThreshold = ThresholdSwitchLogicFactory.DefaultOff;
        private bool _q;

        public ThresholdSwitchRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ThresholdSwitchLogicFactory.RuleOutput);
        }

        internal static double? ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
                case IConvertible c:
                    try
                    {
                        var d = c.ToDouble(CultureInfo.InvariantCulture);
                        return double.IsNaN(d) ? null : d;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == ThresholdSwitchLogicFactory.RuleParamOn)
            {
                OnThreshold = ToDouble(value) ?? ThresholdSwitchLogicFactory.DefaultOn;
            }
            else if (instance.This2RuleInterfaceTemplate == ThresholdSwitchLogicFactory.RuleParamOff)
            {
                OffThreshold = ToDouble(value) ?? ThresholdSwitchLogicFactory.DefaultOff;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        internal bool Evaluate(double v)
        {
            if (OnThreshold >= OffThreshold)
            {
                if (v >= OnThreshold)
                {
                    _q = true;
                }
                else if (v <= OffThreshold)
                {
                    _q = false;
                }
            }
            else
            {
                if (v <= OnThreshold)
                {
                    _q = true;
                }
                else if (v >= OffThreshold)
                {
                    _q = false;
                }
            }

            return _q;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != ThresholdSwitchLogicFactory.RuleInput)
            {
                return new List<ILogicOutputChanged>();
            }

            var v = ToDouble(value);
            if (!v.HasValue)
            {
                return new List<ILogicOutputChanged>();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, Evaluate(v.Value)));
        }
    }
}
