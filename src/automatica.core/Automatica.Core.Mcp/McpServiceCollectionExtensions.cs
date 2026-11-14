using Automatica.Core.Base.Common;
using Automatica.Core.Mcp.Authentication;
using Automatica.Core.Mcp.Tools;
using Role = Automatica.Core.Model.Models.User.Role;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace Automatica.Core.Mcp
{
    public static class McpServiceCollectionExtensions
    {
        public const string PolicyName = "AutomaticaMcp";
        public const string EndpointPath = "/mcp";

        /// <summary>
        /// The JWT bearer scheme name used by the web host (JwtBearerDefaults.AuthenticationScheme).
        /// </summary>
        private const string JwtScheme = "Bearer";

        /// <summary>
        /// Registers the MCP server (streamable HTTP, stateless), its tools and the authorization policy.
        /// Call <see cref="AddMcpApiKey"/> on the authentication builder and <see cref="MapAutomaticaMcp"/> on the endpoints as well.
        /// </summary>
        public static IServiceCollection AddAutomaticaMcp(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<McpAccess>();
            services.AddScoped<NodeDescriber>();
            services.AddSingleton<IAuthorizationHandler, McpEnabledRequirementHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtScheme, McpApiKeyAuthenticationHandler.SchemeName);
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(Role.ViewerRole, Role.AdminRole, Role.VisuRole);
                    policy.AddRequirements(new McpEnabledRequirement());
                });
            });

            services.AddMcpServer(options =>
                {
                    options.ServerInfo = new Implementation
                    {
                        Name = "automatica-core",
                        Title = "Automatica.Core",
                        Version = ServerInfo.GetServerVersion()
                    };
                    options.ServerInstructions = McpInstructions.Text;
                })
                .WithHttpTransport(options =>
                {
                    // no server to client requests are used, so every request stands on its own
                    options.Stateless = true;
                })
                .WithTools<SystemTools>()
                .WithTools<StructureTools>()
                .WithTools<DataPointTools>()
                .WithTools<HistoryTools>()
                .WithTools<LogicTools>()
                .WithTools<AutomationTools>()
                .WithTools<ShadingTools>()
                .WithTools<LogTools>()
                .WithTools<LogicEditTools>()
                .WithTools<NodeConfigTools>()
                .WithTools<HabitTools>();

            return services;
        }

        public static AuthenticationBuilder AddMcpApiKey(this AuthenticationBuilder builder)
        {
            return builder.AddScheme<AuthenticationSchemeOptions, McpApiKeyAuthenticationHandler>(McpApiKeyAuthenticationHandler.SchemeName, null);
        }

        public static IEndpointConventionBuilder MapAutomaticaMcp(this IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapMcp(EndpointPath).RequireAuthorization(PolicyName);
        }
    }
}
