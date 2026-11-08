using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.Control.Base;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Automation.Triggers
{
    /// <summary>
    /// A fired trigger
    /// </summary>
    internal sealed record TriggerEvent(string TriggerId, string Path, object Value, object Previous)
    {
        public static TriggerEvent Manual { get; } = new(null, "", null, null);
    }

    internal sealed class TriggerContext
    {
        public TriggerContext(Guid automationId, IAutomationEnvironment environment, EchoTracker echo, ConditionEvaluator evaluator, ILogger logger, bool serverStartup)
        {
            AutomationId = automationId;
            Environment = environment;
            Echo = echo;
            Evaluator = evaluator;
            Logger = logger;
            ServerStartup = serverStartup;
        }

        public Guid AutomationId { get; }
        public IAutomationEnvironment Environment { get; }
        public EchoTracker Echo { get; }
        public ConditionEvaluator Evaluator { get; }
        public ILogger Logger { get; }

        /// <summary>
        /// True when the engine starts with the server, false when a single automation is (re)loaded
        /// </summary>
        public bool ServerStartup { get; }
    }

    internal interface ITriggerHandler : IAsyncDisposable
    {
        Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire);
    }

    internal static class TriggerHandlerFactory
    {
        public static ITriggerHandler Create(TriggerDefinition trigger, string path)
        {
            return trigger switch
            {
                DatapointTrigger datapoint => new DatapointTriggerHandler(datapoint, path),
                TimeTrigger time => new TimeTriggerHandler(time, path),
                SunTrigger sun => new SunTriggerHandler(sun, path),
                ConditionsTrigger conditions => new ConditionsTriggerHandler(conditions, path),
                ControlTrigger control => new ControlTriggerHandler(control, path),
                StartupTrigger startup => new StartupTriggerHandler(startup, path),
                SceneTrigger scene => new SceneTriggerHandler(scene, path),
                _ => throw new NotSupportedException($"Trigger {trigger?.GetType().Name} is not supported")
            };
        }
    }

    /// <summary>
    /// Value of a datapoint changes into the configured state (edge). An unknown previous value never fires.
    /// </summary>
    internal sealed class DatapointTriggerHandler : ITriggerHandler
    {
        private readonly DatapointTrigger _trigger;
        private readonly string _path;
        private readonly object _value;
        private readonly object _from;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private TriggerContext _context;
        private Func<TriggerEvent, Task> _fire;
        private IDisposable _subscription;
        private ITimer _pending;
        private object _last;
        private object _pendingPrevious;
        private volatile bool _disposed;

        public DatapointTriggerHandler(DatapointTrigger trigger, string path)
        {
            _trigger = trigger;
            _path = path;
            _value = AutomationValues.FromJson(trigger.Value);
            _from = AutomationValues.FromJson(trigger.From);
        }

        public async Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            _context = context;
            _fire = fire;
            _last = AutomationValues.ToPlain(context.Environment.GetValue(_trigger.Node));
            _subscription = await context.Environment.SubscribeAsync(_trigger.Node, OnValueAsync);
        }

        internal bool Matches(object value)
        {
            return _trigger.Change switch
            {
                DatapointChange.To => AutomationValues.AreEqual(value, _value),
                DatapointChange.Above => AutomationValues.Compare(value, _value) > 0,
                DatapointChange.Below => AutomationValues.Compare(value, _value) < 0,
                _ => true
            };
        }

        private async Task OnValueAsync(AutomationValueChange change)
        {
            if (_disposed)
            {
                return;
            }

            var value = AutomationValues.ToPlain(change.Value);
            TriggerEvent fireEvent = null;

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var previous = _last;
                _last = value;

                if (previous == null || value == null)
                {
                    return;
                }

                bool fires;
                if (_trigger.Change == DatapointChange.Any)
                {
                    fires = !AutomationValues.AreEqual(previous, value);
                }
                else
                {
                    var matchedBefore = Matches(previous);
                    var matchesNow = Matches(value);

                    if (!matchesNow)
                    {
                        CancelPending();
                        return;
                    }

                    fires = !matchedBefore;
                }

                if (!fires || (_from != null && !AutomationValues.AreEqual(previous, _from)))
                {
                    return;
                }

                if (_context.Echo.IsEcho(_context.AutomationId, _trigger.Node, value))
                {
                    _context.Logger.LogDebug($"Automation {_context.AutomationId}: ignored own write {value} to {_trigger.Node}");
                    return;
                }

                if (_trigger.ForSeconds > 0)
                {
                    // "any change for N s" restarts the timer on every change, a state trigger keeps the first one
                    if (_trigger.Change == DatapointChange.Any || _pending == null)
                    {
                        CancelPending();
                        _pendingPrevious = previous;
                        _pending = _context.Environment.Time.CreateTimer(_ => _ = OnDurationElapsedAsync(), null,
                            TimeSpan.FromSeconds(_trigger.ForSeconds), Timeout.InfiniteTimeSpan);
                    }
                    return;
                }

                fireEvent = new TriggerEvent(_trigger.Id, _path, value, previous);
            }
            finally
            {
                _lock.Release();
            }

            if (fireEvent != null)
            {
                await _fire(fireEvent).ConfigureAwait(false);
            }
        }

        private async Task OnDurationElapsedAsync()
        {
            TriggerEvent fireEvent;
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed || _pending == null)
                {
                    return;
                }

                _pending.Dispose();
                _pending = null;

                if (_last == null || !Matches(_last))
                {
                    return;
                }

                fireEvent = new TriggerEvent(_trigger.Id, _path, _last, _pendingPrevious);
            }
            finally
            {
                _lock.Release();
            }

            await _fire(fireEvent).ConfigureAwait(false);
        }

        private void CancelPending()
        {
            _pending?.Dispose();
            _pending = null;
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            _subscription?.Dispose();
            _pending?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Base for triggers that fire at a computed time and then compute the next one
    /// </summary>
    internal abstract class ScheduledTriggerHandler : ITriggerHandler
    {
        private static readonly TimeSpan RetryInterval = TimeSpan.FromHours(6);

        private readonly object _lock = new();
        private ITimer _timer;
        private DateTimeOffset _lastOccurrence = DateTimeOffset.MinValue;
        private DateTimeOffset? _scheduled;
        private volatile bool _disposed;

        protected TriggerContext Context { get; private set; }
        private Func<TriggerEvent, Task> _fire;

        protected abstract string TriggerId { get; }
        protected abstract string Path { get; }

        /// <summary>
        /// Next occurrence after <paramref name="after"/>, null when there is none in the next days
        /// </summary>
        protected abstract DateTimeOffset? Next(DateTimeOffset after);

        public Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            Context = context;
            _fire = fire;
            Schedule();
            return Task.CompletedTask;
        }

        private void Schedule()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _timer?.Dispose();
                var time = Context.Environment.Time;
                var now = time.GetUtcNow();
                var after = now > _lastOccurrence ? now : _lastOccurrence;

                DateTimeOffset? next;
                try
                {
                    next = Next(after);
                }
                catch (Exception e)
                {
                    Context.Logger.LogError(e, $"Automation {Context.AutomationId}: could not compute the next time of {Path}");
                    next = null;
                }

                _scheduled = next;
                var due = next.HasValue ? next.Value - now : RetryInterval;
                if (due < TimeSpan.Zero)
                {
                    due = TimeSpan.Zero;
                }

                _timer = time.CreateTimer(_ => _ = ElapsedAsync(), null, due, Timeout.InfiniteTimeSpan);
            }
        }

        private async Task ElapsedAsync()
        {
            DateTimeOffset? occurrence;
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                occurrence = _scheduled;
                if (occurrence.HasValue)
                {
                    _lastOccurrence = occurrence.Value;
                }
            }

            try
            {
                if (occurrence.HasValue)
                {
                    await _fire(new TriggerEvent(TriggerId, Path, occurrence.Value.ToString("HH:mm"), null)).ConfigureAwait(false);
                }
            }
            finally
            {
                Schedule();
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                _disposed = true;
                _timer?.Dispose();
                _timer = null;
            }
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class TimeTriggerHandler : ScheduledTriggerHandler
    {
        private readonly TimeTrigger _trigger;
        private readonly TimeSpan _at;
        private readonly bool _valid;

        public TimeTriggerHandler(TimeTrigger trigger, string path)
        {
            _trigger = trigger;
            Path = path;
            _valid = AutomationValues.TryParseTime(trigger.At, out _at);
        }

        protected override string TriggerId => _trigger.Id;
        protected override string Path { get; }

        protected override DateTimeOffset? Next(DateTimeOffset after)
        {
            return _valid ? LocalTime.NextTime(Context.Environment.Time, _at, _trigger.Days, after) : null;
        }
    }

    internal sealed class SunTriggerHandler : ScheduledTriggerHandler
    {
        private readonly SunTrigger _trigger;

        public SunTriggerHandler(SunTrigger trigger, string path)
        {
            _trigger = trigger;
            Path = path;
        }

        protected override string TriggerId => _trigger.Id;
        protected override string Path { get; }

        protected override DateTimeOffset? Next(DateTimeOffset after)
        {
            return LocalTime.NextSun(Context.Environment, _trigger.Event, _trigger.OffsetMinutes, _trigger.Days, after);
        }
    }

    /// <summary>
    /// The conditions change from not met to met (the state when the automation starts never fires)
    /// </summary>
    internal sealed class ConditionsTriggerHandler : ITriggerHandler
    {
        private readonly ConditionsTrigger _trigger;
        private readonly string _path;
        private ConditionWatcher _watcher;
        private bool _met;

        public ConditionsTriggerHandler(ConditionsTrigger trigger, string path)
        {
            _trigger = trigger;
            _path = path;
        }

        public async Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            var conditionsPath = $"{_path}/conditions";
            _met = context.Evaluator.Evaluate(_trigger.Conditions, conditionsPath, context.Evaluator.CreateContext(_trigger.Id), null);

            _watcher = new ConditionWatcher(context.Environment, _trigger.Conditions, async () =>
            {
                var met = context.Evaluator.Evaluate(_trigger.Conditions, conditionsPath, context.Evaluator.CreateContext(_trigger.Id), null);
                var fires = met && !_met;
                _met = met;
                if (fires)
                {
                    await fire(new TriggerEvent(_trigger.Id, _path, true, false)).ConfigureAwait(false);
                }
            }, context.Logger);
            await _watcher.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_watcher != null)
            {
                await _watcher.DisposeAsync();
            }
        }
    }

    internal sealed class ControlTriggerHandler : ITriggerHandler
    {
        private readonly ControlTrigger _trigger;
        private readonly string _path;
        private readonly object _lock = new();
        private IControlValueCallback _callbackControl;
        private Guid _callbackId;
        private SwitchState _last = SwitchState.Unknown;
        private volatile bool _disposed;

        public ControlTriggerHandler(ControlTrigger trigger, string path)
        {
            _trigger = trigger;
            _path = path;
        }

        public Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            var control = context.Environment.GetControl(_trigger.Control);
            if (control is not ISwitch switchControl)
            {
                context.Logger.LogWarning($"Automation {context.AutomationId}: control {_trigger.Control} of {_path} not found or has no on/off state");
                return Task.CompletedTask;
            }

            _last = switchControl.State;
            _callbackControl = switchControl;
            _callbackId = switchControl.RegisterValueCallback(() =>
            {
                if (_disposed)
                {
                    return;
                }

                SwitchState previous;
                var state = switchControl.State;
                lock (_lock)
                {
                    previous = _last;
                    _last = state;
                }

                if (previous == SwitchState.Unknown || state == SwitchState.Unknown || previous == state)
                {
                    return;
                }

                var fires = _trigger.To switch
                {
                    ControlStateChange.On => state == SwitchState.On,
                    ControlStateChange.Off => state == SwitchState.Off,
                    _ => true
                };

                var on = state == SwitchState.On;
                if (fires && !context.Echo.IsEcho(context.AutomationId, _trigger.Control, on))
                {
                    _ = fire(new TriggerEvent(_trigger.Id, _path, on, previous == SwitchState.On));
                }
            });

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            _callbackControl?.UnregisterValueCallback(_callbackId);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// A scene block pulses its "activated" output (1 and back to 0): every change to true fires, also the first one after
    /// a restart when the previous value is still unknown (unlike the datapoint trigger).
    /// </summary>
    internal sealed class SceneTriggerHandler : ITriggerHandler
    {
        private readonly SceneTrigger _trigger;
        private readonly string _path;
        private readonly object _lock = new();
        private IDisposable _subscription;
        private bool _high;
        private volatile bool _disposed;

        public SceneTriggerHandler(SceneTrigger trigger, string path)
        {
            _trigger = trigger;
            _path = path;
        }

        public async Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            var output = context.Environment.FindLogicOutput(_trigger.Scene, SceneTrigger.ActivatedOutputKey);
            if (output == null)
            {
                context.Logger.LogWarning($"Automation {context.AutomationId}: scene {_trigger.Scene} of {_path} not found");
                return;
            }

            _high = IsTrue(context.Environment.GetValue(output.Value));
            _subscription = await context.Environment.SubscribeAsync(output.Value, change =>
            {
                if (_disposed)
                {
                    return Task.CompletedTask;
                }

                var high = IsTrue(change.Value);
                bool rising;
                lock (_lock)
                {
                    rising = high && !_high;
                    _high = high;
                }

                return rising ? fire(new TriggerEvent(_trigger.Id, _path, true, false)) : Task.CompletedTask;
            });
        }

        private static bool IsTrue(object value)
        {
            return AutomationValues.AreEqual(AutomationValues.ToPlain(value), true);
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            _subscription?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class StartupTriggerHandler : ITriggerHandler
    {
        private readonly StartupTrigger _trigger;
        private readonly string _path;

        public StartupTriggerHandler(StartupTrigger trigger, string path)
        {
            _trigger = trigger;
            _path = path;
        }

        public Task StartAsync(TriggerContext context, Func<TriggerEvent, Task> fire)
        {
            if (context.ServerStartup)
            {
                _ = fire(new TriggerEvent(_trigger.Id, _path, null, null));
            }
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
