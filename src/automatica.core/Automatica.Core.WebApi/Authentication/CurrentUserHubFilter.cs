using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// A SignalR connection keeps the claims of its connect for its whole life. Before every hub call this filter checks the
    /// user again (deleted, token revoked), replaces the roles and user groups with the current ones and evaluates the
    /// policies of the hub method again: a user who lost the admin role cannot call admin methods on an open connection.
    /// </summary>
    public sealed class CurrentUserHubFilter : IHubFilter
    {
        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            var principal = invocationContext.Context.User;
            if (principal?.Identity is ClaimsIdentity { IsAuthenticated: true } identity)
            {
                var provider = invocationContext.ServiceProvider.GetRequiredService<UserClaimsProvider>();
                var wasAdmin = HubAccess.IsAdmin(principal);

                var error = provider.Refresh(identity);
                if (error != null)
                {
                    invocationContext.Context.Abort();
                    throw new HubException(error);
                }

                if (wasAdmin && !HubAccess.IsAdmin(principal))
                {
                    await invocationContext.Hub.Groups.RemoveFromGroupAsync(invocationContext.Context.ConnectionId, HubAccess.AdministratorsGroup);
                }

                var authorization = invocationContext.ServiceProvider.GetRequiredService<IAuthorizationService>();
                foreach (var policy in GetPolicies(invocationContext))
                {
                    var result = await authorization.AuthorizeAsync(principal, policy);
                    if (!result.Succeeded)
                    {
                        throw new HubException($"Not allowed to call {invocationContext.HubMethodName}");
                    }
                }
            }

            return await next(invocationContext);
        }

        private static string[] GetPolicies(HubInvocationContext context)
        {
            return context.HubMethod.GetCustomAttributes<AuthorizeAttribute>(true)
                .Concat(context.Hub.GetType().GetCustomAttributes<AuthorizeAttribute>(true))
                .Select(a => a.Policy)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .ToArray();
        }
    }
}
