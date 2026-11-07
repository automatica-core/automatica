using System.ComponentModel;
using System.Globalization;
using Automatica.Core.Shading.Engine;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Automatica.Core.Mcp.Tools
{
    /// <summary>
    /// Automatic shading of roller and venetian blinds. Like the shading page for administrators only: JWT callers need
    /// the admin role, API key callers are configured by an administrator. The configuration itself is edited in the
    /// web UI, the tools explain and steer the automatic.
    /// </summary>
    [McpServerToolType]
    public class ShadingTools
    {
        private readonly IShadingEngine _engine;
        private readonly McpAccess _access;

        public ShadingTools(IShadingEngine engine, McpAccess access)
        {
            _engine = engine;
            _access = access;
        }

        private void EnsureCanSee()
        {
            if (!_access.IsApiKeyCaller && !_access.IsAdmin)
            {
                throw new McpException("Automatic shading is available to administrators only");
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

        [McpServerTool(Name = "get_shading_status", Title = "Get the automatic shading status", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the state of the automatic shading: sun position, wind, rain, frost and winter mode, the brightness and sun state of each facade, and for each blind its mode (shading, idle, manual, paused, wind, rain, frost, winter, door open, off, unavailable), the reason, its position and the target. Use it to explain why a blind is up or down.")]
        public ShadingStatus GetStatus()
        {
            EnsureCanSee();
            return _engine.GetStatus();
        }

        [McpServerTool(Name = "preview_shading", Title = "Preview the automatic shading", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Simulates the automatic shading at another time (calculated sun position, current sensor values, no delays or manual moves): which facades get sun and which blinds would shade, with position and slat angle.")]
        public ShadingPreview Preview(
            [Description("Time to simulate, ISO 8601 (e.g. 2026-07-15T16:00:00+02:00). Empty: now.")] string at = null)
        {
            EnsureCanSee();
            var time = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(at) && !DateTimeOffset.TryParse(at, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time))
            {
                throw new McpException($"Invalid time '{at}', use ISO 8601");
            }

            return _engine.Preview(time);
        }

        [McpServerTool(Name = "set_shading_automatic", Title = "Switch the automatic shading of a blind on or off", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Pauses (false) or resumes (true) the automatic shading of one blind. The pause is saved and survives a restart. Resuming can move the blind right away: confirm with the user first.")]
        public async Task<BlindStatus> SetAutomatic(
            [Description("Control id of the blind (controlId from get_shading_status).")] Guid controlId,
            [Description("true resumes the automatic, false pauses it.")] bool enabled,
            CancellationToken token = default)
        {
            EnsureCanChange();
            if (!await _engine.SetAutomaticAsync(controlId, enabled, token))
            {
                throw new McpException($"Blind {controlId} is not configured for automatic shading");
            }

            return BlindOrThrow(controlId);
        }

        [McpServerTool(Name = "resume_shading", Title = "End the manual override of a blind", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Ends the manual override of a blind that was moved by hand, so the automatic takes over at once (it may move the blind): confirm with the user first.")]
        public async Task<BlindStatus> Resume(
            [Description("Control id of the blind (controlId from get_shading_status).")] Guid controlId,
            CancellationToken token = default)
        {
            EnsureCanChange();
            if (!await _engine.ResumeAsync(controlId, token))
            {
                throw new McpException($"Blind {controlId} is not configured for automatic shading");
            }

            return BlindOrThrow(controlId);
        }

        private BlindStatus BlindOrThrow(Guid controlId) =>
            _engine.GetStatus().Blinds.FirstOrDefault(a => a.ControlId == controlId) ?? throw new McpException($"Blind {controlId} not found");
    }
}
