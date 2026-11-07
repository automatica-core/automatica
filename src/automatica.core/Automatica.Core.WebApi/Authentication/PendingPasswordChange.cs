using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Automatica.Core.WebApi.Authentication
{
    /// <summary>
    /// Marks the actions a user who must change the password may call with the pending token (only the password change).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    public sealed class AllowPendingPasswordChangeAttribute : Attribute
    {
    }

    public static class PendingPasswordChange
    {
        /// <summary>
        /// Default policy of the web api (the global AuthorizeFilter): an authenticated user, and a token with the
        /// must-change-password claim is rejected everywhere except on actions marked with
        /// <see cref="AllowPendingPasswordChangeAttribute"/>. The global filter is combined with the policies of the
        /// action, so without the exception the password change itself would be rejected too.
        /// </summary>
        public static AuthorizationPolicy BuildDefaultPolicy()
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx => !ctx.User.HasClaim(c => c.Type == UserClaimsProvider.MustChangePasswordClaimType)
                                         || IsAllowedEndpoint(ctx.Resource))
                .Build();
        }

        internal static bool IsAllowedEndpoint(object resource)
        {
            var httpContext = resource switch
            {
                HttpContext http => http,
                AuthorizationFilterContext filterContext => filterContext.HttpContext,
                _ => null
            };

            return httpContext?.GetEndpoint()?.Metadata.GetMetadata<AllowPendingPasswordChangeAttribute>() != null;
        }
    }
}
