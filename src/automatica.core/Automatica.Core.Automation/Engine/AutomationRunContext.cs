using Automatica.Core.Automation.Triggers;
using Automatica.Core.EF.Models.Automations;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// The run ends early (as done), e.g. a stop action or a wait that timed out without "continue"
    /// </summary>
    internal sealed class StopRunException : Exception
    {
        public StopRunException(string reason) : base(reason)
        {
        }
    }

    /// <summary>
    /// State of one run, shared between the runtime (mode gate, stop) and the executing task
    /// </summary>
    internal sealed class AutomationRunContext : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<RunStep> _steps = new();
        private readonly CancellationTokenSource _cts = new();

        public AutomationRunContext(Guid automationId, string automationName, TriggerEvent trigger, bool skipConditions, int depth, DateTime startedAt)
        {
            AutomationId = automationId;
            AutomationName = automationName;
            Trigger = trigger;
            SkipConditions = skipConditions;
            Depth = depth;
            StartedAt = startedAt;
        }

        public Guid RunId { get; } = Guid.NewGuid();
        public Guid AutomationId { get; }
        public string AutomationName { get; }
        public TriggerEvent Trigger { get; }
        public bool SkipConditions { get; }

        /// <summary>
        /// Nesting level of runAutomation chains, 0 for a run started by a trigger
        /// </summary>
        public int Depth { get; }

        public DateTime StartedAt { get; }
        public DateTime? FinishedAt { get; private set; }
        public AutomationRunState State { get; private set; } = AutomationRunState.Running;
        public string Error { get; private set; }
        public string StopReason { get; private set; }

        public CancellationToken Token => _cts.Token;

        public void AddStep(RunStep step)
        {
            lock (_lock)
            {
                _steps.Add(step);
            }
        }

        /// <summary>
        /// Changes a step that was added before (state, duration), under the lock of the snapshots
        /// </summary>
        public void Update(Action change)
        {
            lock (_lock)
            {
                change();
            }
        }

        public void SetState(AutomationRunState state)
        {
            lock (_lock)
            {
                if (FinishedAt == null)
                {
                    State = state;
                }
            }
        }

        public void Finish(AutomationRunState state, DateTime finishedAt, string error = null)
        {
            lock (_lock)
            {
                State = state;
                FinishedAt = finishedAt;
                Error = error;
            }
        }

        public void Cancel(string reason)
        {
            lock (_lock)
            {
                StopReason ??= reason;
            }

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already finished
            }
        }

        public AutomationRunInfo Snapshot()
        {
            lock (_lock)
            {
                return new AutomationRunInfo(RunId, AutomationId, StartedAt, FinishedAt, State, Trigger?.TriggerId, Error,
                    _steps.Select(a => a.Clone()).ToList());
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
