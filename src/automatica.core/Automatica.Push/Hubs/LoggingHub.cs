using System.Threading.Tasks;
using Automatica.Core.Logging.SignalR.Hubs;
using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;

namespace Automatica.Push.Hubs
{
    /// <summary>
    /// Server logs are admin-only (like the logging REST api): they are only sent to the
    /// <see cref="HubAccess.AdministratorsGroup"/>. Other users may connect, but receive nothing.
    /// </summary>
    [Authorize]
    public class LoggingHub : BaseLoggingHub
    {
        public override async Task OnConnectedAsync()
        {
            await HubAccess.JoinAdministratorsGroup(Context, Groups);
            await base.OnConnectedAsync();
        }

        [Authorize(Policy = Role.AdminRole)]
        public override Task SubscribeToGroup(string group)
        {
            return base.SubscribeToGroup(group);
        }
    }
}
