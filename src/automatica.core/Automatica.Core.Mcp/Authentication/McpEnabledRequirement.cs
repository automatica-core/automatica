using Automatica.Core.Internals.Cache.Common;
using Microsoft.AspNetCore.Authorization;

namespace Automatica.Core.Mcp.Authentication
{
    /// <summary>
    /// Fails authorization for the MCP endpoint while the <c>mcpEnabled</c> setting is off,
    /// also for logged-in users with a valid JWT.
    /// </summary>
    public class McpEnabledRequirement : IAuthorizationRequirement
    {
    }

    public class McpEnabledRequirementHandler : AuthorizationHandler<McpEnabledRequirement>
    {
        private readonly ISettingsCache _settingsCache;

        public McpEnabledRequirementHandler(ISettingsCache settingsCache)
        {
            _settingsCache = settingsCache;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, McpEnabledRequirement requirement)
        {
            if (_settingsCache.IsEnabled())
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
