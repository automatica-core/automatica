using System.Diagnostics;
using System.Text.RegularExpressions;
using Automatica.Core.Automation.Definition;
using Automatica.Core.Automation.Engine;
using Automatica.Core.Control.Base;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Automation.Actions
{
    /// <summary>
    /// Callbacks into the engine for actions that act on other automations
    /// </summary>
    internal interface IAutomationRunner
    {
        Task<Guid?> RunAsync(Guid automationId, bool skipConditions, int depth, CancellationToken token);
        Task SetEnabledAsync(Guid automationId, bool enabled, CancellationToken token);
    }

    /// <summary>
    /// Executes the action list of a run, one action after the other, and records a step per action.
    /// </summary>
    internal sealed partial class ActionExecutor
    {
        internal const int MaxChainDepth = 5;

        private readonly IAutomationEnvironment _environment;
        private readonly ConditionEvaluator _evaluator;
        private readonly EchoTracker _echo;
        private readonly IAutomationRunner _runner;
        private readonly ILogger _logger;

        public ActionExecutor(IAutomationEnvironment environment, ConditionEvaluator evaluator, EchoTracker echo, IAutomationRunner runner, ILogger logger)
        {
            _environment = environment;
            _evaluator = evaluator;
            _echo = echo;
            _runner = runner;
            _logger = logger;
        }

        private DateTime UtcNow => _environment.Time.GetUtcNow().UtcDateTime;

        public async Task ExecuteListAsync(IReadOnlyList<ActionDefinition> actions, string basePath, AutomationRunContext run)
        {
            if (actions == null)
            {
                return;
            }

            for (var i = 0; i < actions.Count; i++)
            {
                run.Token.ThrowIfCancellationRequested();

                var action = actions[i];
                var path = $"{basePath}/{i}";

                if (action == null)
                {
                    continue;
                }

                var step = new RunStep { At = UtcNow, Kind = RunStepKind.Action, Path = path, State = RunStepState.Running };
                run.AddStep(step);

                if (!action.Enabled)
                {
                    run.Update(() => step.State = RunStepState.Skipped);
                    continue;
                }

                var watch = Stopwatch.StartNew();
                try
                {
                    await ExecuteAsync(action, path, step, run).ConfigureAwait(false);
                    run.Update(() =>
                    {
                        step.State ??= RunStepState.Done;
                        if (step.State is RunStepState.Running or RunStepState.Waiting)
                        {
                            step.State = RunStepState.Done;
                        }
                        step.DurationMs = watch.ElapsedMilliseconds;
                    });
                }
                catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
                {
                    run.Update(() =>
                    {
                        step.State = RunStepState.Skipped;
                        step.DurationMs = watch.ElapsedMilliseconds;
                        step.Message = run.StopReason;
                    });
                    throw;
                }
                catch (StopRunException e)
                {
                    run.Update(() =>
                    {
                        if (step.State is RunStepState.Running or RunStepState.Waiting or null)
                        {
                            step.State = RunStepState.Done;
                        }
                        step.DurationMs = watch.ElapsedMilliseconds;
                        step.Message ??= e.Message;
                    });
                    throw;
                }
                catch (Exception e)
                {
                    run.Update(() =>
                    {
                        step.State = RunStepState.Failed;
                        step.DurationMs = watch.ElapsedMilliseconds;
                        step.Message = e.Message;
                    });

                    if (!action.ContinueOnError)
                    {
                        throw new AutomationActionException(path, e.Message, e);
                    }

                    _logger.LogWarning(e, $"Automation {run.AutomationName}: action {path} failed, continue");
                }
            }
        }

        private Task ExecuteAsync(ActionDefinition action, string path, RunStep step, AutomationRunContext run)
        {
            return action switch
            {
                SetValueAction setValue => SetValueAsync(setValue, step, run),
                ControlAction control => ControlAsync(control, step, run),
                GroupControlAction group => GroupControlAsync(group, step, run),
                DelayAction delay => DelayAsync(delay, step, run),
                WaitUntilAction wait => WaitUntilAsync(wait, path, step, run),
                NotifyAction notify => NotifyAsync(notify, step, run),
                IfAction ifAction => IfAsync(ifAction, path, step, run),
                RunAutomationAction runAutomation => RunAutomationAsync(runAutomation, step, run),
                EnableAutomationAction enable => EnableAutomationAsync(enable, step, run),
                StopAction stop => throw new StopRunException(string.IsNullOrWhiteSpace(stop.Reason) ? "Stopped by a stop action" : stop.Reason),
                _ => throw new NotSupportedException($"Action {action.GetType().Name} is not supported")
            };
        }

        private async Task SetValueAsync(SetValueAction action, RunStep step, AutomationRunContext run)
        {
            var node = _environment.GetNode(action.Node) ?? throw new InvalidOperationException("Datapoint not found");
            if (!node.IsWriteable)
            {
                throw new InvalidOperationException($"Datapoint '{node.Name}' cannot be written");
            }

            object value;
            switch (action.Mode)
            {
                case SetValueMode.Toggle:
                    AutomationValues.TryToBool(_environment.GetValue(action.Node), out var current);
                    value = !current;
                    break;
                case SetValueMode.Increment:
                    AutomationValues.TryToDouble(_environment.GetValue(action.Node), out var number);
                    value = number + action.Step;
                    break;
                case SetValueMode.Copy:
                    if (action.Source == null)
                    {
                        throw new InvalidOperationException("No source datapoint");
                    }
                    value = _environment.GetValue(action.Source.Value) ?? throw new InvalidOperationException("The value of the source datapoint is unknown");
                    break;
                default:
                    value = AutomationValues.FromJson(action.Value) ?? throw new InvalidOperationException("No value");
                    break;
            }

            value = AutomationValues.ConvertFor(value, node.Type);
            run.Update(() => step.Value = AutomationValues.ToPlain(value));

            _echo.Record(run.AutomationId, node.Id, value);
            await _environment.WriteAsync(node.Id, value, new AutomationWriteSource(run.AutomationId, run.AutomationName), run.Token).ConfigureAwait(false);
        }

        private async Task ControlAsync(ControlAction action, RunStep step, AutomationRunContext run)
        {
            var control = _environment.GetControl(action.Control) ?? throw new InvalidOperationException("Control not found");
            var result = await ApplyAsync(control, action.Command, action.Value, run).ConfigureAwait(false);
            if (result == null)
            {
                throw new InvalidOperationException($"Control '{control.Name}' does not support '{action.Command}'");
            }

            run.Update(() => step.Value = result);
        }

        private async Task GroupControlAsync(GroupControlAction action, RunStep step, AutomationRunContext run)
        {
            var controls = _environment.GetControls(action.Area, action.IncludeSubAreas, action.Category);
            var done = 0;
            var errors = new List<string>();

            foreach (var control in controls)
            {
                run.Token.ThrowIfCancellationRequested();
                try
                {
                    if (await ApplyAsync(control, action.Command, action.Value, run).ConfigureAwait(false) != null)
                    {
                        done++;
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    errors.Add($"{control.Name}: {e.Message}");
                }
            }

            run.Update(() =>
            {
                step.Value = done;
                step.Message = errors.Count > 0 ? string.Join("; ", errors) : (done == 0 ? "No matching controls" : null);
            });

            if (errors.Count > 0 && done == 0)
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }
        }

        /// <returns>What was sent, null when the control does not support the command</returns>
        private async Task<object> ApplyAsync(IControl control, ControlCommand command, int? value, AutomationRunContext run)
        {
            var token = run.Token;
            switch (command)
            {
                case ControlCommand.On when control is ISwitch switchOn:
                    _echo.Record(run.AutomationId, control.Id, true);
                    await switchOn.SwitchOnAsync(token).ConfigureAwait(false);
                    return true;
                case ControlCommand.Off when control is ISwitch switchOff:
                    _echo.Record(run.AutomationId, control.Id, false);
                    await switchOff.SwitchOffAsync(token).ConfigureAwait(false);
                    return false;
                case ControlCommand.Toggle when control is ISwitch toggle:
                    var on = toggle.State != SwitchState.On;
                    _echo.Record(run.AutomationId, control.Id, on);
                    await toggle.SwitchAsync(on, token).ConfigureAwait(false);
                    return on;
                case ControlCommand.Dim when control is IDimmer dimmer:
                    var percent = Math.Clamp(value ?? 100, 0, 100);
                    await dimmer.DimAsync(percent, token).ConfigureAwait(false);
                    return percent;
                case ControlCommand.Up when control is IBlind up:
                    await up.MoveUpAsync(token).ConfigureAwait(false);
                    return "up";
                case ControlCommand.Down when control is IBlind down:
                    await down.MoveDownAsync(token).ConfigureAwait(false);
                    return "down";
                case ControlCommand.Stop when control is IBlind stop:
                    await stop.StopAsync(token).ConfigureAwait(false);
                    return "stop";
                case ControlCommand.Position when control is IBlind position:
                    var pos = Math.Clamp(value ?? 0, 0, 100);
                    await position.MoveAbsoluteAsync(pos, token).ConfigureAwait(false);
                    return pos;
                default:
                    return null;
            }
        }

        private async Task DelayAsync(DelayAction action, RunStep step, AutomationRunContext run)
        {
            var seconds = Math.Max(0, action.Seconds);
            if (action.RandomToSeconds is { } to && to > seconds)
            {
                seconds = Random.Shared.Next(seconds, to + 1);
            }

            run.Update(() =>
            {
                step.Value = seconds;
                step.State = RunStepState.Waiting;
            });
            run.SetState(EF.Models.Automations.AutomationRunState.Waiting);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), _environment.Time, run.Token).ConfigureAwait(false);
            }
            finally
            {
                run.SetState(EF.Models.Automations.AutomationRunState.Running);
            }
        }

        private async Task WaitUntilAsync(WaitUntilAction action, string path, RunStep step, AutomationRunContext run)
        {
            if (action.Condition == null)
            {
                return;
            }

            var conditionPath = $"{path}/condition";
            var time = _environment.Time;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ITimer forTimer = null;
            var forLock = new object();

            async Task EvaluateAsync()
            {
                var met = _evaluator.Evaluate(action.Condition, conditionPath, _evaluator.CreateContext(run.Trigger?.TriggerId), null);
                lock (forLock)
                {
                    if (!met)
                    {
                        forTimer?.Dispose();
                        forTimer = null;
                        return;
                    }

                    if (action.ForSeconds <= 0)
                    {
                        completion.TrySetResult(true);
                    }
                    else if (forTimer == null)
                    {
                        forTimer = time.CreateTimer(_ => completion.TrySetResult(true), null, TimeSpan.FromSeconds(action.ForSeconds), Timeout.InfiniteTimeSpan);
                    }
                }
                await Task.CompletedTask;
            }

            run.Update(() => step.State = RunStepState.Waiting);
            run.SetState(EF.Models.Automations.AutomationRunState.Waiting);

            await using var watcher = new ConditionWatcher(_environment, action.Condition, EvaluateAsync, _logger);
            using var timeout = action.TimeoutSeconds > 0
                ? time.CreateTimer(_ => completion.TrySetResult(false), null, TimeSpan.FromSeconds(action.TimeoutSeconds), Timeout.InfiniteTimeSpan)
                : null;
            await using var cancel = run.Token.Register(() => completion.TrySetCanceled(run.Token));

            try
            {
                await watcher.StartAsync().ConfigureAwait(false);
                await watcher.NotifyAsync().ConfigureAwait(false);

                var met = await completion.Task.ConfigureAwait(false);
                if (met)
                {
                    run.Update(() =>
                    {
                        step.State = RunStepState.Done;
                        step.Passed = true;
                    });
                    return;
                }

                run.Update(() =>
                {
                    step.State = RunStepState.Timeout;
                    step.Passed = false;
                });

                if (!action.ContinueOnTimeout)
                {
                    throw new StopRunException("Wait timed out");
                }
            }
            finally
            {
                lock (forLock)
                {
                    forTimer?.Dispose();
                    forTimer = null;
                }
                run.SetState(EF.Models.Automations.AutomationRunState.Running);
            }
        }

        private async Task NotifyAsync(NotifyAction action, RunStep step, AutomationRunContext run)
        {
            var subject = Render(action.Subject, run);
            var body = Render(action.Body, run);
            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = run.AutomationName;
            }

            run.Update(() => step.Value = subject);

            await _environment.NotifyAsync(run.AutomationId, subject, body ?? "", action.Severity, run.Token).ConfigureAwait(false);

            var recipients = (action.Email ?? new List<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToList();
            if (recipients.Count > 0 && !await _environment.SendEmailAsync(recipients, subject, body ?? "").ConfigureAwait(false))
            {
                throw new InvalidOperationException("Email could not be sent");
            }
        }

        private async Task IfAsync(IfAction action, string path, RunStep step, AutomationRunContext run)
        {
            var passed = _evaluator.Evaluate(action.Condition, $"{path}/condition", _evaluator.CreateContext(run.Trigger?.TriggerId), run.AddStep);
            run.Update(() => step.Passed = passed);

            if (passed)
            {
                await ExecuteListAsync(action.Then, $"{path}/then", run).ConfigureAwait(false);
            }
            else
            {
                await ExecuteListAsync(action.Else, $"{path}/else", run).ConfigureAwait(false);
            }
        }

        private async Task RunAutomationAsync(RunAutomationAction action, RunStep step, AutomationRunContext run)
        {
            if (run.Depth + 1 >= MaxChainDepth)
            {
                throw new InvalidOperationException($"More than {MaxChainDepth} automations started each other, stopped to prevent a loop");
            }

            var runId = await _runner.RunAsync(action.Automation, action.SkipConditions, run.Depth + 1, run.Token).ConfigureAwait(false);
            run.Update(() =>
            {
                step.Value = runId?.ToString();
                step.Message = runId == null ? "Not started (automation missing or already running)" : null;
            });
        }

        private Task EnableAutomationAsync(EnableAutomationAction action, RunStep step, AutomationRunContext run)
        {
            run.Update(() => step.Value = action.Enable);

            // in the background: disabling the automation of this run stops this run
            _ = Task.Run(async () =>
            {
                try
                {
                    await _runner.SetEnabledAsync(action.Automation, action.Enable, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Automation {run.AutomationName}: could not switch automation {action.Automation}");
                }
            });
            return Task.CompletedTask;
        }

        [GeneratedRegex(@"\{(automation|trigger|trigger\.value|value:([0-9a-fA-F-]{36}))\}")]
        private static partial Regex PlaceholderRegex();

        internal string Render(string template, AutomationRunContext run)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }

            return PlaceholderRegex().Replace(template, match =>
            {
                switch (match.Groups[1].Value)
                {
                    case "automation":
                        return run.AutomationName;
                    case "trigger":
                        return run.Trigger?.TriggerId ?? "manual";
                    case "trigger.value":
                        return AutomationValues.Format(run.Trigger?.Value);
                    default:
                        return Guid.TryParse(match.Groups[2].Value, out var id) ? AutomationValues.Format(_environment.GetValue(id)) : match.Value;
                }
            });
        }
    }

    internal sealed class AutomationActionException : Exception
    {
        public AutomationActionException(string path, string message, Exception inner) : base(message, inner)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
