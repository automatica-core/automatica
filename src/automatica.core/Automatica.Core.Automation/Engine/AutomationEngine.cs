using System.Collections.Concurrent;
using System.Text.Json;
using Automatica.Core.Automation.Actions;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Triggers;
using Automatica.Core.EF.Models.Automations;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Automation.Engine
{
    internal sealed class AutomationEngine : IAutomationEngine, IAutomationRunner
    {
        internal const int KeepRuns = 50;

        private readonly IAutomationEnvironment _environment;
        private readonly IAutomationStore _store;
        private readonly ILogger<AutomationEngine> _logger;
        private readonly ConditionEvaluator _evaluator;
        private readonly EchoTracker _echo;
        private readonly ActionExecutor _executor;

        private readonly ConcurrentDictionary<Guid, AutomationRuntime> _runtimes = new();
        private readonly SemaphoreSlim _lifecycle = new(1, 1);
        private volatile bool _started;

        public AutomationEngine(IAutomationEnvironment environment, IAutomationStore store, ILogger<AutomationEngine> logger)
        {
            _environment = environment;
            _store = store;
            _logger = logger;
            _evaluator = new ConditionEvaluator(environment);
            _echo = new EchoTracker(environment.Time);
            _executor = new ActionExecutor(environment, _evaluator, _echo, this, logger);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            await _lifecycle.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_started)
                {
                    return;
                }
                _started = true;

                var automations = await _store.LoadAllAsync(token).ConfigureAwait(false);
                _logger.LogInformation($"Loading {automations.Count(a => a.IsEnabled)} automations...");

                foreach (var automation in automations.Where(a => a.IsEnabled))
                {
                    await LoadAsync(automation, serverStartup: true).ConfigureAwait(false);
                }

                _logger.LogInformation("Loading automations...done");
            }
            finally
            {
                _lifecycle.Release();
            }
        }

        public async Task StopAsync()
        {
            await _lifecycle.WaitAsync().ConfigureAwait(false);
            try
            {
                _started = false;
                foreach (var id in _runtimes.Keys.ToList())
                {
                    await UnloadAsync(id).ConfigureAwait(false);
                }
            }
            finally
            {
                _lifecycle.Release();
            }
        }

        public async Task ReloadAsync(Guid automationId, CancellationToken token = default)
        {
            await _lifecycle.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await UnloadAsync(automationId).ConfigureAwait(false);

                if (!_started)
                {
                    return;
                }

                var automation = await _store.LoadAsync(automationId, token).ConfigureAwait(false);
                if (automation is { IsEnabled: true })
                {
                    await LoadAsync(automation, serverStartup: false).ConfigureAwait(false);
                }
            }
            finally
            {
                _lifecycle.Release();
            }
        }

        public async Task RemoveAsync(Guid automationId)
        {
            await _lifecycle.WaitAsync().ConfigureAwait(false);
            try
            {
                await UnloadAsync(automationId).ConfigureAwait(false);
            }
            finally
            {
                _lifecycle.Release();
            }
        }

        public async Task SetEnabledAsync(Guid automationId, bool enabled, CancellationToken token = default)
        {
            await _store.SetEnabledAsync(automationId, enabled, token).ConfigureAwait(false);
            await ReloadAsync(automationId, token).ConfigureAwait(false);
        }

        private AutomationRuntime CreateRuntime(EF.Models.Automations.Automation automation, AutomationDefinition definition)
        {
            return new AutomationRuntime(automation, definition, _environment, _evaluator, _executor, _echo, _logger,
                OnRunFinishedAsync, OnRateLimitExceededAsync);
        }

        private async Task LoadAsync(EF.Models.Automations.Automation automation, bool serverStartup)
        {
            AutomationDefinition definition;
            try
            {
                definition = AutomationJson.Parse(automation.Definition);
            }
            catch (AutomationDefinitionException e)
            {
                _logger.LogError(e, $"Automation {automation.Name} ({automation.ObjId}) has an invalid definition, it is not started");
                return;
            }

            var runtime = CreateRuntime(automation, definition);
            _runtimes[automation.ObjId] = runtime;
            await runtime.StartTriggersAsync(serverStartup).ConfigureAwait(false);
            _logger.LogInformation($"Automation {automation.Name} started ({definition.Triggers.Count} triggers, {definition.Actions.Count} actions, mode {automation.Mode})");
        }

        private async Task UnloadAsync(Guid automationId)
        {
            if (_runtimes.TryRemove(automationId, out var runtime))
            {
                await runtime.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<Guid?> RunAsync(Guid automationId, bool skipConditions, CancellationToken token = default)
        {
            return RunAsync(automationId, skipConditions, 0, token);
        }

        public async Task<Guid?> RunAsync(Guid automationId, bool skipConditions, int depth, CancellationToken token)
        {
            if (!_runtimes.TryGetValue(automationId, out var runtime))
            {
                // a disabled automation: loaded without triggers, only for this manual run
                await _lifecycle.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (!_runtimes.TryGetValue(automationId, out runtime))
                    {
                        var automation = await _store.LoadAsync(automationId, token).ConfigureAwait(false);
                        if (automation == null)
                        {
                            return null;
                        }

                        AutomationDefinition definition;
                        try
                        {
                            definition = AutomationJson.Parse(automation.Definition);
                        }
                        catch (AutomationDefinitionException e)
                        {
                            _logger.LogError(e, $"Automation {automation.Name} has an invalid definition");
                            return null;
                        }

                        runtime = CreateRuntime(automation, definition);
                        _runtimes[automationId] = runtime;
                        if (automation.IsEnabled && _started)
                        {
                            await runtime.StartTriggersAsync(false).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    _lifecycle.Release();
                }
            }

            var trigger = depth > 0 ? new TriggerEvent(null, "", null, null) : TriggerEvent.Manual;
            return runtime.StartRun(trigger, skipConditions, depth);
        }

        public bool StopRun(Guid automationId, Guid runId)
        {
            return _runtimes.TryGetValue(automationId, out var runtime) && runtime.StopRun(runId);
        }

        public int GetActiveRunCount(Guid automationId)
        {
            return _runtimes.TryGetValue(automationId, out var runtime) ? runtime.ActiveRunCount : 0;
        }

        public async Task<IReadOnlyList<AutomationRunInfo>> GetRunsAsync(Guid automationId, int take = KeepRuns, CancellationToken token = default)
        {
            var live = _runtimes.TryGetValue(automationId, out var runtime) ? runtime.GetRecentRuns() : Array.Empty<AutomationRunInfo>();
            var stored = await _store.GetRunsAsync(automationId, take, token).ConfigureAwait(false);

            var result = new Dictionary<Guid, AutomationRunInfo>();
            foreach (var run in stored)
            {
                result[run.ObjId] = new AutomationRunInfo(run.ObjId, run.This2Automation,
                    DateTime.SpecifyKind(run.StartedAt, DateTimeKind.Utc),
                    run.FinishedAt.HasValue ? DateTime.SpecifyKind(run.FinishedAt.Value, DateTimeKind.Utc) : null,
                    run.State, run.TriggerId, run.Error, ParseSteps(run.Steps));
            }

            // the in memory runs are newer (active) or the same
            foreach (var run in live)
            {
                result[run.RunId] = run;
            }

            return result.Values.OrderByDescending(a => a.StartedAt).Take(take).ToList();
        }

        private IReadOnlyList<RunStep> ParseSteps(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<RunStep>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<RunStep>>(json, AutomationJson.Options) ?? new List<RunStep>();
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "Could not read the steps of an automation run");
                return Array.Empty<RunStep>();
            }
        }

        public async Task<IReadOnlyList<ValidationProblem>> ValidateAsync(AutomationDefinition definition, Guid? automationId, CancellationToken token = default)
        {
            var ids = await _store.GetIdsAsync(token).ConfigureAwait(false);
            return new AutomationValidator(_environment, ids, automationId).Validate(definition);
        }

        public TestResult Test(AutomationDefinition definition, string triggerId)
        {
            var steps = new List<RunStep>();
            var passed = _evaluator.Evaluate(definition.Conditions, "conditions", _evaluator.CreateContext(triggerId), steps.Add);
            return new TestResult(passed, steps);
        }

        private async Task OnRunFinishedAsync(AutomationRunContext run)
        {
            var info = run.Snapshot();
            var entity = new AutomationRun
            {
                ObjId = info.RunId,
                This2Automation = info.AutomationId,
                StartedAt = info.StartedAt,
                FinishedAt = info.FinishedAt,
                State = info.State,
                TriggerId = info.TriggerId,
                Error = info.Error,
                Steps = JsonSerializer.Serialize(info.Steps, AutomationJson.Options)
            };

            await _store.SaveRunAsync(entity, KeepRuns).ConfigureAwait(false);
        }

        private async Task OnRateLimitExceededAsync(Guid automationId)
        {
            try
            {
                await SetEnabledAsync(automationId, false).ConfigureAwait(false);
                await _environment.NotifyAsync(automationId, "Automation switched off",
                    $"The automation started more than {AutomationRuntime.RateLimitRuns} times within a minute and was switched off. Check its triggers and actions for a loop.",
                    NotifySeverity.Warning, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not switch off automation {automationId}");
            }
        }
    }
}
