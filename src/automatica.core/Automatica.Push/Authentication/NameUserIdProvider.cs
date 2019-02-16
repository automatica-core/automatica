using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Automatica.Core.Push
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}
