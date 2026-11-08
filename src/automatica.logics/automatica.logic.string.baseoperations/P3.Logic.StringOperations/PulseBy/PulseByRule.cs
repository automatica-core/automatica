using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.PulseBy
{
    /// <summary>
    /// A text that contains a pattern sends 1, the timer sends 0 after Pd seconds; a new match during the pulse starts the
    /// duration again. Both go out through the dispatcher, a pulse of 0 s sends 1 and 0 right away.
    /// </summary>
    public class PulseByRule : TextLogicBase
    {
        private readonly object _sync = new();
        private System.Timers.Timer _timer;
        private bool _pulse;

        public PulseByRule(ILogicContext context) : base(context)
        {
        }

        internal bool Pulsing => _pulse;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
                _pulse = false;
            }
            return base.Stop(instance, token);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != PulseByLogicFactory.InputText || value == null || !Matches(TextValue.ToText(value)))
            {
                return NoChanges();
            }

            lock (_sync)
            {
                if (_timer == null)
                {
                    return NoChanges();
                }

                var seconds = Math.Max(0, ParameterDouble(PulseByLogicFactory.ParamDuration, PulseByLogicFactory.DefaultDuration));
                if (!_pulse)
                {
                    _pulse = true;
                    Dispatch(PulseByLogicFactory.OutputPulse, true);
                }

                if (seconds <= 0)
                {
                    _timer.Stop();
                    _pulse = false;
                    Dispatch(PulseByLogicFactory.OutputPulse, false);
                }
                else
                {
                    _timer.Stop();
                    _timer.Interval = Math.Clamp(seconds * 1000, 1, int.MaxValue);
                    _timer.Start();
                }
            }
            return NoChanges();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_pulse || _timer == null)
                {
                    return;
                }
                _pulse = false;
                Dispatch(PulseByLogicFactory.OutputPulse, false);
            }
        }

        internal bool Matches(string text)
        {
            var ignoreCase = ParameterBool(PulseByLogicFactory.ParamIgnoreCase);
            foreach (var id in PulseByLogicFactory.ParamPatterns)
            {
                var pattern = ParameterText(id);
                if (!string.IsNullOrEmpty(pattern) && Contains(text, pattern, ignoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// true when the text contains the pattern; * = any characters, ? = one character
        /// </summary>
        internal static bool Contains(string text, string pattern, bool ignoreCase)
        {
            var regex = new StringBuilder();
            foreach (var c in pattern)
            {
                regex.Append(c switch
                {
                    '*' => ".*",
                    '?' => ".",
                    _ => Regex.Escape(c.ToString())
                });
            }

            var options = RegexOptions.Singleline | RegexOptions.CultureInvariant;
            if (ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }
            try
            {
                return Regex.IsMatch(text ?? string.Empty, regex.ToString(), options, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
