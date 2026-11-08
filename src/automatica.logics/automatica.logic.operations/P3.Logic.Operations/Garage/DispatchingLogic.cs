using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// Base of the Loxone style blocks (garage, centrals, media controller, irrigation): finds the interfaces by template and
    /// dispatches the outputs itself, so timer driven changes and input driven changes take the same way.
    /// </summary>
    public abstract class DispatchingLogic : Automatica.Core.Logic.Logic
    {
        private readonly Dictionary<Guid, RuleInterfaceInstance> _byTemplate;
        private readonly Dictionary<Guid, object> _lastSent = new();

        /// <summary>
        /// Guards the state against inputs and timer ticks running at the same time
        /// </summary>
        protected readonly object Sync = new();

        protected DispatchingLogic(ILogicContext context) : base(context)
        {
            _byTemplate = context.RuleInstance.RuleInterfaceInstance
                .GroupBy(a => a.This2RuleInterfaceTemplate)
                .ToDictionary(a => a.Key, a => a.First());
        }

        /// <summary>
        /// The interface of this instance for a template, null when the instance does not have it (created by an older version)
        /// </summary>
        protected RuleInterfaceInstance Interface(Guid template)
        {
            return _byTemplate.TryGetValue(template, out var instance) ? instance : null;
        }

        protected Guid TemplateOf(RuleInterfaceInstance instance) => instance.This2RuleInterfaceTemplate;

        /// <summary>
        /// Sends the value when it differs from the value sent last
        /// </summary>
        protected void Send(Guid output, object value)
        {
            if (_lastSent.TryGetValue(output, out var last) && Equals(last, value))
            {
                return;
            }
            SendAlways(output, value);
        }

        /// <summary>
        /// Sends the value every time (commands, pulses: the receiver reacts to every telegram)
        /// </summary>
        protected void SendAlways(Guid output, object value)
        {
            var instance = Interface(output);
            if (instance == null)
            {
                return;
            }
            _lastSent[output] = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(instance, value).Instance, value);
        }

        protected object LastSent(Guid output) => _lastSent.TryGetValue(output, out var value) ? value : null;

        /// <summary>
        /// The values sent last, by interface id, like the base class reports the returned outputs
        /// </summary>
        protected Dictionary<string, object> SentValuesForVisu()
        {
            var ret = new Dictionary<string, object>();
            if (base.GetDataForVisu() is IDictionary<string, object> baseValues)
            {
                foreach (var pair in baseValues)
                {
                    ret[pair.Key] = pair.Value;
                }
            }
            foreach (var pair in _lastSent)
            {
                var instance = Interface(pair.Key);
                if (instance != null)
                {
                    ret[$"{instance.ObjId}"] = pair.Value;
                }
            }
            return ret;
        }

        /// <summary>
        /// The value came from the visualization (DisPc does not block it)
        /// </summary>
        protected static bool IsFromVisu(IDispatchable source) => source?.Source == DispatchableSource.Visualization;
    }

    /// <summary>
    /// Lenient conversions of input and parameter values
    /// </summary>
    public static class LogicValue
    {
        /// <summary>
        /// true for bool true, non-zero numbers and the strings "true" / non-zero numbers
        /// </summary>
        public static bool IsTruthy(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sd) && sd != 0;
                case IConvertible c:
                    try
                    {
                        return c.ToDouble(CultureInfo.InvariantCulture) != 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        public static double? ToDouble(object value)
        {
            try
            {
                double d = value switch
                {
                    null => double.NaN,
                    bool b => b ? 1 : 0,
                    string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                    IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
                    _ => double.NaN
                };
                return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static double ToDouble(object value, double fallback) => ToDouble(value) ?? fallback;

        public static int ToInt(object value, int fallback)
        {
            var d = ToDouble(value);
            if (!d.HasValue || d.Value > int.MaxValue || d.Value < int.MinValue)
            {
                return fallback;
            }
            return (int)Math.Round(d.Value, MidpointRounding.AwayFromZero);
        }

        public static string ToText(object value) => value switch
        {
            null => "",
            string s => s,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };

        /// <summary>
        /// <see cref="System.Timers.Timer.Interval"/> must be &gt; 0 and &lt;= int.MaxValue
        /// </summary>
        public static double TimerInterval(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || milliseconds < 1)
            {
                return 1;
            }
            return Math.Min(milliseconds, int.MaxValue);
        }
    }
}
