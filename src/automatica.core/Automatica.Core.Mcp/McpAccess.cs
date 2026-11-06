using System.Security.Claims;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Mcp.Authentication;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Http;

namespace Automatica.Core.Mcp
{
    /// <summary>
    /// What the caller of the current MCP request may see and do.
    /// API key callers see everything (they are configured by an administrator) and may write only when
    /// <c>mcpAllowWrite</c> is on. JWT callers follow the same rules as the web UI: user groups restrict
    /// visibility and every authenticated user may set values.
    /// </summary>
    public class McpAccess
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISettingsCache _settingsCache;

        public McpAccess(IHttpContextAccessor httpContextAccessor, ISettingsCache settingsCache)
        {
            _httpContextAccessor = httpContextAccessor;
            _settingsCache = settingsCache;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

        public bool IsApiKeyCaller => User.Identities.Any(a => a.IsAuthenticated && a.AuthenticationType == McpApiKeyAuthenticationHandler.SchemeName);

        public bool IsAdmin => User.IsInRole(Role.AdminRole);

        public string CallerName => IsApiKeyCaller ? "MCP" : $"MCP ({User.Identity?.Name})";

        public bool CanSee(Guid? userGroup)
        {
            if (userGroup == null || IsApiKeyCaller || IsAdmin)
            {
                return true;
            }

            return User.Claims.Any(a => a.Type == UserGroup.ClaimType && a.Value == userGroup.ToString());
        }

        public bool CanWrite => !IsApiKeyCaller || _settingsCache.IsWriteAllowedForApiKey();
    }
}
