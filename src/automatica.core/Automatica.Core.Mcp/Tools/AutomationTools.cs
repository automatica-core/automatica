using System.ComponentModel;
using Automatica.Core.Automation.Engine;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Automations (if this then that rules). Like the web UI they are for administrators only: JWT callers need the
    /// admin role, API key callers are configured by an administrator.
    /// </summary>
    [McpServerToolType]
    public class AutomationTools
    {
        private readonly IAutomationStore _store;
        private readonly IAutomationEngine _engine;
        private readonly McpAccess _access;

        public AutomationTools(IAutomationStore store, IAutomationEngine engine, McpAccess access)
        {
            _store = store;
            _engine = engine;
            _access = access;
        }

        private void EnsureCanSee()
        {
            if (!_access.IsApiKeyCaller && !_access.IsAdmin)
            {
                throw new McpException("Automations are available to administrators only");
            }
        }

        private void EnsureCanChange()
        {
            EnsureCanSee();
            if (!_access.CanWrite)
            {
                throw new McpException("Writing is disabled for the MCP API key. Enable the setting 'mcpAllowWrite' or log in with a user token.");
            }
        }

        [McpServerTool(Name = "list_automations", Title = "List automations", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists the automations (if this then that rules: triggers, conditions, actions) with their state and last run.")]
        public async Task<IList<AutomationSummary>> ListAutomations(
            [Description("Only automations whose name or description contains this text (case insensitive).")] string query = null,
            CancellationToken token = default)
        {
            EnsureCanSee();
            var automations = await _store.LoadAllAsync(token);
            return automations
                .Where(a => string.IsNullOrWhiteSpace(query) || $"{a.Name} {a.Description}".Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Name)
                .Select(a => new AutomationSummary(a.ObjId, a.Name, a.Description, a.IsEnabled, a.Mode.ToString(),
                    _engine.GetActiveRunCount(a.ObjId), a.LastRunAt, a.LastRunState?.ToString(), a.LastError))
                .ToList();
        }

        [McpServerTool(Name = "get_automation_runs", Title = "Get automation runs", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the latest runs of an automation with every step (trigger, conditions with their values, actions), newest first. Use it to explain why an automation did or did not act.")]
        public async Task<IReadOnlyList<AutomationRunInfo>> GetRuns(
            [Description("Automation id from list_automations.")] Guid id,
            [Description("How many runs, 1 to 50.")] int take = 5,
            CancellationToken token = default)
        {
            EnsureCanSee();
            await LoadOrThrowAsync(id, token);
            return await _engine.GetRunsAsync(id, Math.Clamp(take, 1, 50), token);
        }

        [McpServerTool(Name = "run_automation", Title = "Run an automation", Destructive = true, Idempotent = false, OpenWorld = true)]
        [Description("Starts an automation now, as if a trigger fired. Its actions act on the real installation: confirm with the user first.")]
        public async Task<RunAutomationResult> RunAutomation(
            [Description("Automation id from list_automations.")] Guid id,
            [Description("Run the actions even when the conditions are not met.")] bool skipConditions = false,
            CancellationToken token = default)
        {
            EnsureCanChange();
            await LoadOrThrowAsync(id, token);

            var runId = await _engine.RunAsync(id, skipConditions, token);
            return runId switch
            {
                null => new RunAutomationResult(false, null, "Not started: the automation is already running (mode single) or at its run limit."),
                { } queued when queued == Guid.Empty => new RunAutomationResult(true, null, "Queued behind the running run."),
                { } started => new RunAutomationResult(true, started, "Started. Use get_automation_runs for the result.")
            };
        }

        [McpServerTool(Name = "set_automation_enabled", Title = "Switch an automation on or off", Destructive = true, Idempotent = true, OpenWorld = false)]
        [Description("Switches an automation on (its triggers fire) or off. Switching off stops its running runs.")]
        public async Task<AutomationSummary> SetEnabled(
            [Description("Automation id from list_automations.")] Guid id,
            [Description("true switches on, false switches off.")] bool enabled,
            CancellationToken token = default)
        {
            EnsureCanChange();
            await LoadOrThrowAsync(id, token);
            await _engine.SetEnabledAsync(id, enabled, token);

            var automation = await LoadOrThrowAsync(id, token);
            return new AutomationSummary(automation.ObjId, automation.Name, automation.Description, automation.IsEnabled, automation.Mode.ToString(),
                _engine.GetActiveRunCount(id), automation.LastRunAt, automation.LastRunState?.ToString(), automation.LastError);
        }

        private async Task<EF.Models.Automations.Automation> LoadOrThrowAsync(Guid id, CancellationToken token)
        {
            return await _store.LoadAsync(id, token) ?? throw new McpException($"Automation {id} not found");
        }
    }

    public record AutomationSummary(Guid Id, string Name, string Description, bool IsEnabled, string Mode, int ActiveRuns,
        DateTime? LastRunAt, string LastRunState, string LastError);

    public record RunAutomationResult(bool Accepted, Guid? RunId, string Message);
}
