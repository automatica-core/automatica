using Automatica.Core.Logging.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class LoggingHub : BaseLoggingHub
    {
        
    }
}
