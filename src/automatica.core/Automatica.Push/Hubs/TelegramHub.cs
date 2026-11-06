using Automatica.Core.Model.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Automatica.Push.Hubs
{
    /// <summary>
    /// Bus telegrams are admin-only (like the telegram monitor REST api): they are only sent to the
    /// <see cref="HubAccess.AdministratorsGroup"/>. Other users may connect, but receive nothing.
    /// </summary>
    [Authorize]
    public class TelegramHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await HubAccess.JoinAdministratorsGroup(Context, Groups);
            await base.OnConnectedAsync();
        }

        [Authorize(Policy = Role.AdminRole)]
        public async Task Subscribe(string name)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, name);
        }
        [Authorize(Policy = Role.AdminRole)]
        public async Task Unsubscribe(string name)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
        }
    }
}
