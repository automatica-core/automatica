using Automatica.Core.Automation.Definition;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models.Automations;

namespace Automatica.Core.Automation.Engine
{
    public enum AutomationValueType
    {
        Unknown,
        Boolean,
        Number,
        String
    }

    /// <summary>
    /// A datapoint (node instance or logic interface) as the engine sees it
    /// </summary>
    public sealed record AutomationNode(Guid Id, string Name, bool IsReadable, bool IsWriteable, AutomationValueType Type);

    public sealed record AutomationValueChange(Guid Id, object Value);

    /// <summary>
    /// Who writes a value: the automation and its run
    /// </summary>
    public sealed record AutomationWriteSource(Guid AutomationId, string AutomationName);

    /// <summary>
    /// Everything the engine needs from the running core. Implemented by <see cref="CoreAutomationEnvironment"/>,
    /// replaced by a fake in the tests.
    /// </summary>
    public interface IAutomationEnvironment
    {
        TimeProvider Time { get; }

        /// <summary>
        /// Current value of a datapoint (null when unknown)
        /// </summary>
        object GetValue(Guid id);

        /// <summary>
        /// Values of <paramref name="id"/> read from the devices (or logic outputs). Disposing the result unsubscribes.
        /// </summary>
        Task<IDisposable> SubscribeAsync(Guid id, Func<AutomationValueChange, Task> callback);

        AutomationNode GetNode(Guid id);

        Task WriteAsync(Guid id, object value, AutomationWriteSource source, CancellationToken token);

        IControl GetControl(Guid id);

        /// <summary>
        /// Controls whose logic is in the area (and its sub areas) and/or in the category
        /// </summary>
        IReadOnlyList<IControl> GetControls(Guid? area, bool includeSubAreas, Guid? category);

        bool AreaExists(Guid id);
        bool CategoryExists(Guid id);

        bool HasLocation { get; }

        /// <summary>
        /// Local time of <paramref name="sunEvent"/> on <paramref name="date"/>, null when the sun does not rise/set that day
        /// </summary>
        DateTimeOffset? GetSunTime(DateOnly date, SunEvent sunEvent);

        Task NotifyAsync(Guid automationId, string subject, string body, NotifySeverity severity, CancellationToken token);

        /// <returns>False when the email could not be sent</returns>
        Task<bool> SendEmailAsync(IList<string> to, string subject, string body);
    }

    /// <summary>
    /// Persistence of the automations and their runs
    /// </summary>
    public interface IAutomationStore
    {
        Task<IReadOnlyList<EF.Models.Automations.Automation>> LoadAllAsync(CancellationToken token = default);
        Task<EF.Models.Automations.Automation> LoadAsync(Guid id, CancellationToken token = default);
        Task<IReadOnlyCollection<Guid>> GetIdsAsync(CancellationToken token = default);
        Task SetEnabledAsync(Guid id, bool enabled, CancellationToken token = default);

        /// <summary>
        /// Stores a finished run, updates the last run of the automation and removes runs beyond <paramref name="keep"/>
        /// </summary>
        Task SaveRunAsync(AutomationRun run, int keep, CancellationToken token = default);

        Task<IReadOnlyList<AutomationRun>> GetRunsAsync(Guid automationId, int take, CancellationToken token = default);
    }

    public interface IAutomationEngine
    {
        Task StartAsync(CancellationToken token = default);
        Task StopAsync();

        /// <summary>
        /// Reads the automation again and restarts it (or stops it when it was disabled or deleted)
        /// </summary>
        Task ReloadAsync(Guid automationId, CancellationToken token = default);

        Task RemoveAsync(Guid automationId);

        Task SetEnabledAsync(Guid automationId, bool enabled, CancellationToken token = default);

        /// <summary>
        /// Starts a run like a trigger would (the mode applies), also for a disabled automation
        /// </summary>
        /// <returns>The run id, null when the mode did not allow another run or the automation does not exist</returns>
        Task<Guid?> RunAsync(Guid automationId, bool skipConditions, CancellationToken token = default);

        bool StopRun(Guid automationId, Guid runId);

        int GetActiveRunCount(Guid automationId);

        /// <summary>
        /// Active and finished runs, newest first
        /// </summary>
        Task<IReadOnlyList<AutomationRunInfo>> GetRunsAsync(Guid automationId, int take = 50, CancellationToken token = default);

        Task<IReadOnlyList<ValidationProblem>> ValidateAsync(AutomationDefinition definition, Guid? automationId, CancellationToken token = default);

        /// <summary>
        /// Evaluates the conditions with the current values, nothing is written
        /// </summary>
        TestResult Test(AutomationDefinition definition, string triggerId);
    }

    public enum ValidationSeverity
    {
        Error,
        Warning
    }

    public sealed record ValidationProblem(string Path, string Code, ValidationSeverity Severity, string Detail = null);

    public sealed record TestResult(bool Passed, IReadOnlyList<RunStep> Steps);

    public sealed record AutomationRunInfo(
        Guid RunId,
        Guid AutomationId,
        DateTime StartedAt,
        DateTime? FinishedAt,
        AutomationRunState State,
        string TriggerId,
        string Error,
        IReadOnlyList<RunStep> Steps);
}
