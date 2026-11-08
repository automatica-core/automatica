using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Surveillance.Security
{
    public class SecurityLogEntry
    {
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// What happened, a short event key (e.g. "armed", "alarm", "acknowledged"), translated by the visualization
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// Who or what caused it (user name, input name, level). Never a code or tag.
        /// </summary>
        public string Detail { get; set; }
    }

    /// <summary>
    /// The tests switch the real timers off and fire the ticks themselves
    /// </summary>
    internal static class SecurityTimers
    {
        public static bool Enabled { get; set; } = true;
    }

    public class SecurityStateBase
    {
        public List<SecurityLogEntry> Log { get; set; } = new();
    }

    /// <summary>
    /// Base of the security logics: keeps the state in a <see cref="ISecurityStateStore"/> so it survives restarts, evaluates
    /// its timeouts in a periodic tick against <see cref="ILogicContext.TimeProvider"/> and dispatches only changed outputs.
    /// Inputs, ticks and commands run under one lock; the values are dispatched after the lock is released.
    /// </summary>
    public abstract class PersistentLogicBase<TState> : Automatica.Core.Logic.Logic where TState : SecurityStateBase, new()
    {
        public const int MaxLogEntries = 50;

        /// <summary>
        /// Pulse inputs (arm, acknowledge...) only set their baseline right after the start: a value replayed by a driver after
        /// a restart must not arm or disarm anything
        /// </summary>
        public static readonly TimeSpan StartupGrace = TimeSpan.FromSeconds(10);

        protected readonly object Sync = new();
        private readonly ISecurityStateStore _store;
        private readonly Dictionary<Guid, object> _lastOutputs = new();
        private readonly List<(RuleInterfaceInstance Interface, object Value)> _pending = new();
        private readonly Dictionary<(Guid Template, Guid Source), bool> _lastInputs = new();
        private readonly Dictionary<Guid, object> _params = new();
        private readonly List<Action> _deferred = new();
        private readonly object _flushLock = new();
        private Timer _timer;
        private DateTimeOffset _startedAt;

        protected PersistentLogicBase(ILogicContext context, ISecurityStateStore store) : base(context)
        {
            _store = store;
        }

        protected TState State { get; private set; } = new();

        protected bool Started { get; private set; }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        protected DateTimeOffset Now => Context.TimeProvider.GetUtcNow();

        /// <summary>
        /// Interval of <see cref="OnTick"/>, null for logics without timeouts
        /// </summary>
        protected virtual TimeSpan? TickInterval => TimeSpan.FromSeconds(1);

        protected RuleInterfaceInstance Interface(Guid template) =>
            Context.RuleInstance.RuleInterfaceInstance.FirstOrDefault(a => a.This2RuleInterfaceTemplate == template);

        #region lifecycle

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _params.Clear();
                foreach (var param in instance.RuleInterfaceInstance)
                {
                    _params[param.This2RuleInterfaceTemplate] = param.Value;
                }

                TState restored = null;
                try
                {
                    restored = _store.Load<TState>(Id);
                }
                catch (Exception e)
                {
                    Context.Logger.LogError(e, $"Could not load the state of security logic {Name}");
                }

                State = restored ?? new TState();
                _lastOutputs.Clear();
                _lastInputs.Clear();
                _startedAt = Now;
                Started = true;
                OnStarted(restored != null);
            }

            Flush();

            if (TickInterval is { } interval && SecurityTimers.Enabled)
            {
                _timer?.Dispose();
                _timer = new Timer(_ => Tick(), null, interval, interval);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;

            lock (Sync)
            {
                if (Started)
                {
                    Persist();
                }
                Started = false;
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// After the start (state loaded or fresh). Set all outputs here, they are dispatched once the lock is released.
        /// </summary>
        protected abstract void OnStarted(bool restored);

        #endregion

        #region tick

        /// <summary>
        /// Evaluates the timeouts. Runs every <see cref="TickInterval"/>, the tests call it directly.
        /// </summary>
        internal void Tick()
        {
            try
            {
                lock (Sync)
                {
                    if (!Started)
                    {
                        return;
                    }

                    if (OnTick(Now))
                    {
                        Persist();
                    }
                }

                Flush();
            }
            catch (Exception e)
            {
                Context.Logger.LogError(e, $"Error in the tick of security logic {Name}");
            }
        }

        /// <returns>True if the state changed and must be saved</returns>
        protected virtual bool OnTick(DateTimeOffset now) => false;

        #endregion

        #region inputs and parameters

        protected sealed override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            List<ILogicOutputChanged> ret;
            lock (Sync)
            {
                if (!Started)
                {
                    return new List<ILogicOutputChanged>();
                }

                if (OnInput(instance.This2RuleInterfaceTemplate, value, source))
                {
                    Persist();
                }

                if (OrderedOutputs)
                {
                    ret = new List<ILogicOutputChanged>();
                }
                else
                {
                    // returned instead of dispatched, the core dispatches them
                    ret = _pending.Select(a => (ILogicOutputChanged)new LogicOutputChanged(a.Interface, a.Value)).ToList();
                    _pending.Clear();
                }
            }

            if (OrderedOutputs)
            {
                Flush();
            }
            else
            {
                RunDeferred();
            }
            return ret;
        }

        /// <returns>True if the state changed and must be saved</returns>
        protected abstract bool OnInput(Guid template, object value, IDispatchable source);

        protected sealed override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                _params[instance.This2RuleInterfaceTemplate] = value;
                if (Started)
                {
                    OnParameterChanged(instance.This2RuleInterfaceTemplate);
                }
            }

            Flush();
        }

        protected virtual void OnParameterChanged(Guid template)
        {
        }

        protected object Param(Guid template) => _params.TryGetValue(template, out var v) ? v : Interface(template)?.Value;

        protected double ParamDouble(Guid template, double fallback) => ToDouble(Param(template), fallback);

        protected int ParamInt(Guid template, int fallback) => (int)Math.Round(ToDouble(Param(template), fallback));

        protected bool ParamBool(Guid template, bool fallback = false) => Param(template) == null ? fallback : ToBool(Param(template));

        protected string ParamString(Guid template) => Convert.ToString(Param(template), CultureInfo.InvariantCulture) ?? "";

        protected TimeSpan ParamSeconds(Guid template, double fallback) => TimeSpan.FromSeconds(Math.Max(0, ParamDouble(template, fallback)));

        /// <summary>
        /// Right after the start: values may be replays of the last known values, not new events
        /// </summary>
        protected bool InStartupGrace => Now - _startedAt < StartupGrace;

        /// <summary>
        /// True on a rising edge (false -> true) of a pulse input, per source (several buttons may share one input).
        /// Right after the start the first value only sets the baseline.
        /// </summary>
        protected bool Rising(Guid template, IDispatchable source, object value)
        {
            var now = ToBool(value);
            var key = (template, source?.Id ?? Guid.Empty);
            var known = _lastInputs.TryGetValue(key, out var previous);
            _lastInputs[key] = now;

            if (!now || (known && previous))
            {
                return false;
            }

            return known || Now - _startedAt >= StartupGrace;
        }

        public static bool ToBool(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    s = s.Trim();
                    if (bool.TryParse(s, out var parsed))
                    {
                        return parsed;
                    }
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    {
                        return Math.Abs(d) > double.Epsilon;
                    }
                    return s.Equals("on", StringComparison.OrdinalIgnoreCase);
                default:
                    try
                    {
                        return Math.Abs(Convert.ToDouble(value, CultureInfo.InvariantCulture)) > double.Epsilon;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
            }
        }

        public static double ToDouble(object value, double fallback)
        {
            switch (value)
            {
                case null:
                    return fallback;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : fallback;
                default:
                    try
                    {
                        var v = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return double.IsNaN(v) ? fallback : v;
                    }
                    catch (Exception)
                    {
                        return fallback;
                    }
            }
        }

        #endregion

        #region outputs

        /// <summary>
        /// Sets an output, it is dispatched only if the value changed (or always with <paramref name="force"/>)
        /// </summary>
        protected void SetOutput(Guid template, object value, bool force = false)
        {
            var output = Interface(template);
            if (output == null)
            {
                return;
            }

            if (!force && _lastOutputs.TryGetValue(template, out var last) && Equals(last, value))
            {
                return;
            }

            _lastOutputs[template] = value;
            _pending.Add((output, value));
        }

        /// <summary>
        /// A short pulse: true, then false
        /// </summary>
        protected void PulseOutput(Guid template)
        {
            SetOutput(template, true, true);
            SetOutput(template, false, true);
        }

        internal object LastOutput(Guid template) => _lastOutputs.TryGetValue(template, out var v) ? v : null;

        /// <summary>
        /// Runs an action once the lock is released (e.g. a command to another logic)
        /// </summary>
        protected void Defer(Action action) => _deferred.Add(action);

        private void RunDeferred()
        {
            List<Action> deferred;
            lock (Sync)
            {
                if (_deferred.Count == 0)
                {
                    return;
                }
                deferred = _deferred.ToList();
                _deferred.Clear();
            }

            foreach (var action in deferred)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Context.Logger.LogError(e, $"Error in a deferred action of logic {Name}");
                }
            }
        }

        /// <summary>
        /// Dispatches the pending output values and runs the deferred actions, outside of the lock
        /// </summary>
        protected void Flush()
        {
            // one flush at a time: values are dispatched in the order they were set, also from background work
            lock (_flushLock)
            {
                List<(RuleInterfaceInstance Interface, object Value)> pending;
                lock (Sync)
                {
                    pending = _pending.ToList();
                    _pending.Clear();
                }

                foreach (var (output, value) in pending)
                {
                    try
                    {
                        Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(output), value).GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        Context.Logger.LogError(e, $"Could not dispatch an output of security logic {Name}");
                    }
                }
            }

            RunDeferred();
        }

        /// <summary>
        /// Logics with background work (network, calls) set outputs from other threads. With true the outputs of an input
        /// are dispatched by the logic itself (in order with the background results) instead of being returned to the core,
        /// so an older value can never overtake a newer one.
        /// </summary>
        protected virtual bool OrderedOutputs => false;

        #endregion

        #region state, log and notifications

        protected void Persist()
        {
            try
            {
                _store.Save(Id, State);
            }
            catch (Exception e)
            {
                Context.Logger.LogError(e, $"Could not save the state of security logic {Name}");
            }
        }

        protected void AddLog(string eventKey, string detail = null)
        {
            State.Log.Insert(0, new SecurityLogEntry { Time = Now, Event = eventKey, Detail = detail });
            if (State.Log.Count > MaxLogEntries)
            {
                State.Log.RemoveRange(MaxLogEntries, State.Log.Count - MaxLogEntries);
            }
        }

        /// <summary>
        /// A notification of the server (push / notification center), fire and forget
        /// </summary>
        protected void Notify(string subjectKey, string body, NotificationSeverity severity)
        {
            try
            {
                var subject = Context.LocalizationProvider?.GetTranslation(subjectKey) ?? subjectKey;
                _ = Context.CreateNotification($"{subject}: {Name}", body ?? "", severity).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Context.Logger.LogWarning(t.Exception, $"Could not create a notification for security logic {Name}");
                    }
                }, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                Context.Logger.LogWarning(e, $"Could not create a notification for security logic {Name}");
            }
        }

        /// <summary>
        /// Name of the data point or logic that sent a value, for the log
        /// </summary>
        protected static string SourceName(IDispatchable source) => string.IsNullOrWhiteSpace(source?.Name) ? null : source.Name;

        #endregion

        /// <summary>
        /// The visualization never gets the parameter values (they may hold keys)
        /// </summary>
        public override object GetDataForVisu()
        {
            lock (Sync)
            {
                return BuildState(false);
            }
        }

        /// <summary>
        /// The state for the visualization: never codes, tags or other secrets
        /// </summary>
        protected abstract object BuildState(bool includeAdminData);

        protected object LogForVisu(int count = 20) => State.Log.Take(count).Select(a => new { time = a.Time, @event = a.Event, detail = a.Detail }).ToList();
    }

    /// <summary>
    /// A <see cref="PersistentLogicBase{TState}"/> that the visualization shows and commands through webapi/security-visu
    /// (<see cref="ISecurityControl"/>): the alarms and the access controller.
    /// </summary>
    public abstract class SecurityLogicBase<TState> : PersistentLogicBase<TState>, ISecurityControl where TState : SecurityStateBase, new()
    {
        protected SecurityLogicBase(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public abstract string SecurityKind { get; }
        public abstract IReadOnlyCollection<string> SecurityCommands { get; }

        #region visualization and commands

        public object GetSecurityState(bool includeAdminData)
        {
            lock (Sync)
            {
                return BuildState(includeAdminData);
            }
        }


        public Task<SecurityCommandResult> ExecuteSecurityCommandAsync(string command, IReadOnlyDictionary<string, string> arguments,
            SecurityCommandContext context, CancellationToken token = default)
        {
            SecurityCommandResult result;
            lock (Sync)
            {
                if (!Started)
                {
                    return Task.FromResult(SecurityCommandResult.NotPossible);
                }

                if (command == null || !SecurityCommands.Contains(command))
                {
                    return Task.FromResult(SecurityCommandResult.UnknownCommand);
                }

                result = OnCommand(command, arguments ?? new Dictionary<string, string>(), context);
                if (result == SecurityCommandResult.Ok)
                {
                    Persist();
                }
            }

            Flush();
            return Task.FromResult(result);
        }

        protected abstract SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context);

        #endregion
    }
}
