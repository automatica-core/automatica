using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Automatica.Core.Mcp.Authentication
{
    /// <summary>
    /// Authenticates MCP clients with the API key from the <c>mcpApiKey</c> setting, sent either as
    /// <c>Authorization: Bearer &lt;key&gt;</c> or as <c>X-Api-Key: &lt;key&gt;</c>. JWTs are left to the JWT handler,
    /// so a logged-in user's token works on the MCP endpoint as well.
    /// </summary>
    public class McpApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "McpApiKey";
        public const string ApiKeyUserName = "mcp-api-key";

        private readonly ISettingsCache _settingsCache;

        public McpApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISettingsCache settingsCache)
            : base(options, logger, encoder)
        {
            _settingsCache = settingsCache;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var presentedKey = ReadPresentedKey();
            if (string.IsNullOrEmpty(presentedKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!_settingsCache.IsEnabled())
            {
                return Task.FromResult(AuthenticateResult.Fail("MCP is disabled"));
            }

            var expectedKey = _settingsCache.GetApiKey();
            if (string.IsNullOrWhiteSpace(expectedKey) || !KeysMatch(presentedKey, expectedKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid MCP API key"));
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, ApiKeyUserName),
                new(ClaimTypes.Role, Role.ViewerRole)
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private string ReadPresentedKey()
        {
            if (Request.Headers.TryGetValue("X-Api-Key", out var headerKey) && !string.IsNullOrWhiteSpace(headerKey))
            {
                return headerKey.ToString().Trim();
            }

            var authorization = Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = authorization["Bearer ".Length..].Trim();

            // a JWT (header.payload.signature) belongs to the JWT bearer handler
            return token.Count(c => c == '.') == 2 ? null : token;
        }

        private static bool KeysMatch(string presented, string expected)
        {
            // hash first so the comparison is constant time regardless of the key lengths
            var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected.Trim()));
            return CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash);
        }
    }
}
