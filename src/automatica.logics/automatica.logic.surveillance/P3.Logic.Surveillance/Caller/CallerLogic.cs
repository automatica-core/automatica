using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.Caller.CallerLogicFactory;

namespace P3.Logic.Surveillance.Caller
{
    public class CallerState : SecurityStateBase
    {
        /// <summary>
        /// Start of every call of the last 24 hours (rate limit)
        /// </summary>
        public List<DateTimeOffset> Calls { get; set; } = new();
        public string LastResult { get; set; }
        public bool LastError { get; set; }
    }

    /// <summary>
    /// Call generator modelled on the Loxone "Call Generator", see <see cref="CallerLogicFactory"/>.
    /// Numbers and message support the placeholders &lt;v1&gt;..&lt;v8&gt;, &lt;v1.2&gt; (two decimals) and &lt;vn&gt; (block name).
    /// Limits like the Loxone caller service: 100 calls per hour, 200 per day, further calls are dropped.
    /// Not implemented: dial key outputs (they need a webhook the provider can reach) and user fields (&lt;user.phone&gt;).
    /// </summary>
    public class CallerLogic : PersistentLogicBase<CallerState>
    {
        public const string DefaultMessage = "Alarm <vn>: <v1>";
        public const int MaxCallsPerHour = 100;
        public const int MaxCallsPerDay = 200;

        public const int ProviderSimulation = 0;
        public const int ProviderTwilio = 1;

        private static readonly Regex Placeholder = new("<(vn|v[1-8](\\.\\d)?)>", RegexOptions.Compiled);

        private readonly Func<CallerLogic, ITelephonyProvider> _providerFactory;
        private readonly object[] _values = new object[F.ValueCount];
        private bool _triggerOn;
        private bool _locked;
        private DateTimeOffset? _dueAt;
        private DateTimeOffset? _repeatAt;
        private Task _calls = Task.CompletedTask;

        /// <param name="providerFactory">null: picked from the provider parameter</param>
        public CallerLogic(ILogicContext context, ISecurityStateStore store, Func<CallerLogic, ITelephonyProvider> providerFactory) : base(context, store)
        {
            _providerFactory = providerFactory;
        }

        protected override TimeSpan? TickInterval => TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// The calls started last (tests wait for them)
        /// </summary>
        internal Task Calls
        {
            get
            {
                lock (Sync)
                {
                    return _calls;
                }
            }
        }

        protected override bool OrderedOutputs => true;

        private ITelephonyProvider Provider()
        {
            if (_providerFactory != null)
            {
                return _providerFactory(this);
            }

            return ParamInt(F.ParamProvider, ProviderSimulation) == ProviderTwilio
                ? new TwilioTelephonyProvider(ParamString(F.ParamTwilioSid), ParamString(F.ParamTwilioToken), ParamString(F.ParamTwilioFrom))
                : new SimulatedTelephonyProvider();
        }

        protected override void OnStarted(bool restored)
        {
            Prune(Now);
            UpdateOutputs(true);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            var index = Array.IndexOf(F.InValues, template);
            if (index >= 0)
            {
                _values[index] = value;
                return false;
            }

            if (template == F.InOff)
            {
                _locked = ToBool(value);
                if (_locked)
                {
                    _dueAt = null;
                    _repeatAt = null;
                }
                return false;
            }

            if (template != F.InTrigger)
            {
                return false;
            }

            var rising = Rising(template, source, value);
            _triggerOn = ToBool(value);
            if (!_triggerOn)
            {
                _repeatAt = null;
                return false;
            }

            if (!rising || _locked)
            {
                return false;
            }

            var delay = TimeSpan.FromMilliseconds(Math.Max(0, ParamDouble(F.ParamTriggerDelay, 0)));
            if (delay > TimeSpan.Zero)
            {
                _dueAt = Now + delay;
                return false;
            }

            return Call(Now);
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;
            if (_dueAt is { } due && now >= due)
            {
                _dueAt = null;
                changed = Call(now);
            }
            else if (_repeatAt is { } repeat && now >= repeat && _triggerOn && !_locked)
            {
                changed = Call(now);
            }

            if (Prune(now))
            {
                UpdateOutputs();
                changed = true;
            }
            return changed;
        }

        private bool Call(DateTimeOffset now)
        {
            var interval = ParamSeconds(F.ParamUpdateInterval, 0);
            _repeatAt = interval > TimeSpan.Zero && _triggerOn ? now + interval : null;

            var message = Format(ParamString(F.ParamMessage), DefaultMessage);
            var numbers = Format(ParamString(F.ParamNumbers), "")
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .ToList();

            if (numbers.Count == 0)
            {
                Result(false, "no phone number");
                return true;
            }

            var language = ParamString(F.ParamLanguage);
            var requests = new List<CallRequest>();
            foreach (var raw in numbers)
            {
                var number = PhoneNumbers.Normalize(raw);
                if (number == null)
                {
                    AddLog("call-invalid-number", PhoneNumbers.Mask(raw));
                    Result(false, "invalid phone number " + PhoneNumbers.Mask(raw));
                    continue;
                }

                Prune(now);
                var lastHour = State.Calls.Count(a => now - a < TimeSpan.FromHours(1));
                if (lastHour >= MaxCallsPerHour || State.Calls.Count >= MaxCallsPerDay)
                {
                    AddLog("call-rate-limited", PhoneNumbers.Mask(number));
                    Result(false, "rate limit reached, call dropped");
                    continue;
                }

                State.Calls.Add(now);
                AddLog("call", PhoneNumbers.Mask(number));
                requests.Add(new CallRequest(number, message, language));
            }

            if (ParamBool(F.ParamNotify, true))
            {
                Notify("SURVEILLANCE.NOTIFY.CALL", message, NotificationSeverity.Warning);
            }

            UpdateOutputs();
            if (requests.Count > 0)
            {
                var provider = Provider();
                _calls = Task.Run(() => RunCalls(provider, requests));
            }
            return true;
        }

        private async Task RunCalls(ITelephonyProvider provider, IReadOnlyList<CallRequest> requests)
        {
            foreach (var request in requests)
            {
                CallResult result;
                try
                {
                    result = await provider.PlaceCallAsync(request).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Context.Logger.LogError(e, $"Call of {Name} to {PhoneNumbers.Mask(request.To)} failed");
                    result = CallResult.Failed(e.GetType().Name);
                }

                lock (Sync)
                {
                    if (!Started)
                    {
                        return;
                    }

                    Result(result.Success, $"{provider.Name} {PhoneNumbers.Mask(request.To)}: {result.Info}");
                    if (!result.Success)
                    {
                        AddLog("call-failed", PhoneNumbers.Mask(request.To));
                    }
                    Persist();
                }
                Flush();
            }
        }

        private void Result(bool success, string text)
        {
            State.LastResult = text;
            State.LastError = !success;
            UpdateOutputs();
        }

        /// <returns>True if calls older than a day were removed</returns>
        private bool Prune(DateTimeOffset now) => State.Calls.RemoveAll(a => now - a >= TimeSpan.FromDays(1)) > 0;

        private void UpdateOutputs(bool force = false)
        {
            SetOutput(F.OutCalls, State.Calls.Count, force);
            SetOutput(F.OutResult, State.LastResult ?? "", force);
            SetOutput(F.OutError, State.LastError, force);
        }

        internal string Format(string template, string fallback)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                template = fallback;
            }

            return Placeholder.Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                if (key == "vn")
                {
                    return Name ?? "";
                }

                var index = key[1] - '1';
                var value = _values[index];
                if (key.Length > 2)
                {
                    var number = ToDouble(value, double.NaN);
                    if (!double.IsNaN(number))
                    {
                        return number.ToString("F" + (key[3] - '0'), CultureInfo.InvariantCulture);
                    }
                }
                return value switch
                {
                    bool b => b ? "1" : "0",
                    IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
                };
            });
        }

        protected override object BuildState(bool includeAdminData) => new
        {
            calls = State.Calls.Count,
            result = State.LastResult ?? "",
            error = State.LastError,
            log = LogForVisu()
        };
    }
}
