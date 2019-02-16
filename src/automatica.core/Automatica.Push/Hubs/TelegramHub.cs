using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class TelegramHub : Hub
    {
        public async Task Subscribe(string name)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, name);
        }
        public async Task Unsubscribe(string name)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
        }
    }
}
