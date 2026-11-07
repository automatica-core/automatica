using Automatica.Core.Automation.Actions;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Triggers;
using Automatica.Core.EF.Models.Automations;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// A loaded automation: its trigger handlers, the mode gate and the active runs.
    /// </summary>
    internal sealed class AutomationRuntime : IAsyncDisposable
    {
        internal const int RateLimitRuns = 30;
        internal static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
        private const int RecentRuns = 10;

        private readonly AutomationDefinition _definition;
        private readonly IAutomationEnvironment _environment;
        private readonly ConditionEvaluator _evaluator;
        private readonly ActionExecutor _executor;
        private readonly EchoTracker _echo;
        private readonly ILogger _logger;
        private readonly Func<AutomationRunContext, Task> _runFinished;
        private readonly Func<Guid, Task> _rateLimitExceeded;

        private readonly object _lock = new();
        private readonly List<ITriggerHandler> _handlers = new();
        private readonly List<AutomationRunContext> _active = new();
        private readonly Queue<(TriggerEvent Trigger, bool SkipConditions, int Depth)> _queue = new();
        private readonly Queue<DateTimeOffset> _triggerTimes = new();
        private readonly LinkedList<AutomationRunInfo> _recent = new();
        private bool _disposed;
        private bool _rateLimited;

        public AutomationRuntime(EF.Models.Automations.Automation automation, AutomationDefinition definition, IAutomationEnvironment environment,
            ConditionEvaluator evaluator, ActionExecutor executor, EchoTracker echo, ILogger logger,
            Func<AutomationRunContext, Task> runFinished, Func<Guid, Task> rateLimitExceeded)
        {
            Id = automation.ObjId;
            Name = automation.Name;
            Mode = automation.Mode;
            MaxRuns = Math.Max(1, automation.MaxRuns);
            IsEnabled = automation.IsEnabled;
            _definition = definition;
            _environment = environment;
            _evaluator = evaluator;
            _executor = executor;
            _echo = echo;
            _logger = logger;
            _runFinished = runFinished;
            _rateLimitExceeded = rateLimitExceeded;
        }

        public Guid Id { get; }
        public string Name { get; }
        public AutomationMode Mode { get; }
        public int MaxRuns { get; }

        /// <summary>
        /// A disabled automation is only loaded for a manual run, its triggers are not started
        /// </summary>
        public bool IsEnabled { get; }

        public int ActiveRunCount
        {
            get
            {
                lock (_lock)
                {
                    return _active.Count;
                }
            }
        }

        public async Task StartTriggersAsync(bool serverStartup)
        {
            var context = new TriggerContext(Id, _environment, _echo, _evaluator, _logger, serverStartup);
            for (var i = 0; i < _definition.Triggers.Count; i++)
            {
                var trigger = _definition.Triggers[i];
                var path = $"triggers/{i}";
                try
                {
                    var handler = TriggerHandlerFactory.Create(trigger, path);
                    lock (_lock)
                    {
                        if (_disposed)
                        {
                            return;
                        }
                        _handlers.Add(handler);
                    }
                    await handler.StartAsync(context, OnTriggerAsync).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Automation {Name}: could not start trigger {path}");
                }
            }
        }

        private async Task OnTriggerAsync(TriggerEvent trigger)
        {
            if (!CheckRateLimit())
            {
                return;
            }

            _logger.LogDebug($"Automation {Name}: trigger {trigger.Path} ({trigger.TriggerId}) fired with {trigger.Value}");
            await Task.Yield();
            StartRun(trigger, false, 0);
        }

        /// <summary>
        /// False when the automation fired too often, it is then switched off by the engine
        /// </summary>
        private bool CheckRateLimit()
        {
            var now = _environment.Time.GetUtcNow();
            bool exceeded;
            lock (_lock)
            {
                if (_disposed || _rateLimited)
                {
                    return false;
                }

                _triggerTimes.Enqueue(now);
                while (_triggerTimes.Count > 0 && now - _triggerTimes.Peek() > RateLimitWindow)
                {
                    _triggerTimes.Dequeue();
                }

                exceeded = _triggerTimes.Count > RateLimitRuns;
                _rateLimited = exceeded;
            }

            if (exceeded)
            {
                _logger.LogWarning($"Automation {Name} fired more than {RateLimitRuns} times within {RateLimitWindow.TotalSeconds}s, it is switched off");
                _ = _rateLimitExceeded(Id);
                return false;
            }

            return true;
        }

        /// <returns>The run id, null when the mode does not allow a run now</returns>
        public Guid? StartRun(TriggerEvent trigger, bool skipConditions, int depth)
        {
            AutomationRunContext run;
            lock (_lock)
            {
                if (_disposed)
                {
                    return null;
                }

                switch (Mode)
                {
                    case AutomationMode.Single when _active.Count > 0:
                        _logger.LogDebug($"Automation {Name} is already running, trigger {trigger.TriggerId} ignored (mode single)");
                        return null;
                    case AutomationMode.Restart:
                        foreach (var active in _active)
                        {
                            active.Cancel("Restarted by a new trigger");
                        }
                        break;
                    case AutomationMode.Queued when _active.Count > 0:
                        if (_queue.Count + _active.Count >= MaxRuns)
                        {
                            _logger.LogWarning($"Automation {Name}: queue is full ({MaxRuns}), trigger {trigger.TriggerId} dropped");
                            return null;
                        }
                        _queue.Enqueue((trigger, skipConditions, depth));
                        return Guid.Empty;
                    case AutomationMode.Parallel when _active.Count >= MaxRuns:
                        _logger.LogWarning($"Automation {Name}: {MaxRuns} runs are active, trigger {trigger.TriggerId} dropped");
                        return null;
                }

                run = new AutomationRunContext(Id, Name, trigger, skipConditions, depth, _environment.Time.GetUtcNow().UtcDateTime);
                _active.Add(run);
            }

            _ = Task.Run(() => ExecuteAsync(run));
            return run.RunId;
        }

        private async Task ExecuteAsync(AutomationRunContext run)
        {
            try
            {
                run.AddStep(new RunStep
                {
                    At = run.StartedAt,
                    Kind = RunStepKind.Trigger,
                    Path = run.Trigger?.Path ?? "",
                    Value = AutomationValues.ToPlain(run.Trigger?.Value),
                    Previous = AutomationValues.ToPlain(run.Trigger?.Previous),
                    Message = string.IsNullOrEmpty(run.Trigger?.Path) ? (run.Depth > 0 ? "Started by another automation" : "Started manually") : null
                });

                if (!run.SkipConditions)
                {
                    var passed = _evaluator.Evaluate(_definition.Conditions, "conditions", _evaluator.CreateContext(run.Trigger?.TriggerId), run.AddStep);
                    if (!passed)
                    {
                        run.Finish(AutomationRunState.ConditionsNotMet, UtcNow);
                        return;
                    }
                }

                await _executor.ExecuteListAsync(_definition.Actions, "actions", run).ConfigureAwait(false);
                run.Finish(AutomationRunState.Done, UtcNow);
            }
            catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
            {
                run.AddStep(new RunStep { At = UtcNow, Kind = RunStepKind.Info, Path = "", Message = run.StopReason ?? "Stopped" });
                run.Finish(AutomationRunState.Stopped, UtcNow);
            }
            catch (StopRunException e)
            {
                run.AddStep(new RunStep { At = UtcNow, Kind = RunStepKind.Info, Path = "", Message = e.Message });
                run.Finish(AutomationRunState.Done, UtcNow);
            }
            catch (AutomationActionException e)
            {
                run.Finish(AutomationRunState.Failed, UtcNow, e.Message);
                _logger.LogWarning($"Automation {Name}: action {e.Path} failed: {e.Message}");
            }
            catch (Exception e)
            {
                run.AddStep(new RunStep { At = UtcNow, Kind = RunStepKind.Error, Path = "", Message = e.Message });
                run.Finish(AutomationRunState.Failed, UtcNow, e.Message);
                _logger.LogError(e, $"Automation {Name}: run failed");
            }
            finally
            {
                await OnFinishedAsync(run).ConfigureAwait(false);
            }
        }

        private DateTime UtcNow => _environment.Time.GetUtcNow().UtcDateTime;

        private async Task OnFinishedAsync(AutomationRunContext run)
        {
            (TriggerEvent Trigger, bool SkipConditions, int Depth)? next = null;
            lock (_lock)
            {
                _active.Remove(run);
                _recent.AddFirst(run.Snapshot());
                while (_recent.Count > RecentRuns)
                {
                    _recent.RemoveLast();
                }

                if (!_disposed && Mode == AutomationMode.Queued && _queue.Count > 0)
                {
                    next = _queue.Dequeue();
                }
            }

            try
            {
                await _runFinished(run).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Automation {Name}: could not store run {run.RunId}");
            }
            finally
            {
                run.Dispose();
            }

            if (next.HasValue)
            {
                StartRun(next.Value.Trigger, next.Value.SkipConditions, next.Value.Depth);
            }
        }

        public bool StopRun(Guid runId)
        {
            lock (_lock)
            {
                var run = _active.FirstOrDefault(a => a.RunId == runId);
                if (run == null)
                {
                    return false;
                }
                run.Cancel("Stopped by a user");
                return true;
            }
        }

        /// <summary>
        /// Active runs and the latest finished ones (they may not be stored yet), newest first
        /// </summary>
        public IReadOnlyList<AutomationRunInfo> GetRecentRuns()
        {
            lock (_lock)
            {
                return _active.Select(a => a.Snapshot()).Concat(_recent).OrderByDescending(a => a.StartedAt).ToList();
            }
        }

        /// <summary>
        /// Stops the triggers and cancels the active runs (does not wait for them)
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            List<ITriggerHandler> handlers;
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                handlers = _handlers.ToList();
                _handlers.Clear();
                _queue.Clear();

                foreach (var run in _active)
                {
                    run.Cancel("Automation was changed or switched off");
                }
            }

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Automation {Name}: error stopping a trigger");
                }
            }

            _echo.Forget(Id);
        }
    }
}
