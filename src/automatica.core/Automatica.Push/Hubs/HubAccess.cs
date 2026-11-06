using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.SignalR;

namespace Automatica.Push.Hubs
{
    /// <summary>
    /// Authorization helpers shared by the hubs.
    /// Every authenticated user may connect to every hub (the app connects to all of them after the login), but
    /// admin-only data (bus telegrams, logs, update progress) is only sent to the <see cref="AdministratorsGroup"/>,
    /// which only administrators join, and the admin-only hub methods carry an admin policy.
    /// </summary>
    public static class HubAccess
    {
        /// <summary>
        /// Group of the administrator connections (groups are per hub, the same name is used in every hub).
        /// </summary>
        public const string AdministratorsGroup = "automatica-administrators";

        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return user?.IsInRole(Role.AdminRole) ?? false;
        }

        /// <summary>
        /// Same rule as the MCP access (<c>McpAccess.CanSee</c>): no user group, an administrator or a member of the group.
        /// </summary>
        public static bool CanSee(ClaimsPrincipal user, Guid? userGroup)
        {
            if (userGroup == null || IsAdmin(user))
            {
                return true;
            }

            return user?.Claims.Any(a => a.Type == UserGroup.ClaimType && a.Value == userGroup.ToString()) ?? false;
        }

        public static Task JoinAdministratorsGroup(HubCallerContext context, IGroupManager groups)
        {
            if (IsAdmin(context.User))
            {
                return groups.AddToGroupAsync(context.ConnectionId, AdministratorsGroup);
            }

            return Task.CompletedTask;
        }
    }
}
