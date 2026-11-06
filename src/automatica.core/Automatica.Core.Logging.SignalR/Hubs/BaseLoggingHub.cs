using Automatica.Core.Logging.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Automatica.Core.Logging.SignalR.Hubs
{
    public abstract class BaseLoggingHub : Hub<ISerilogHub>
    {
        public virtual async Task SubscribeToGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }
    }
}
