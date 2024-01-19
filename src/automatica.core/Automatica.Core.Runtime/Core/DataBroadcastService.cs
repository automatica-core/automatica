using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Core
{
    internal class DataBroadcastService(IHubContext<DataHub> dataHub, ILogger<DataBroadcastService> logger)
        : IDataBroadcast
    {
        private readonly ILogger<DataBroadcastService> _logger = logger;

        public async Task DispatchValue(DispatchableType type, Guid id, object value)
        {
            try
            {
                await dataHub.Clients.Group("All").SendAsync("dispatchValue", type, id, value);
                await dataHub.Clients.Group(id.ToString()).SendAsync("dispatchValue", type, id, value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error dispatch value via hub {e}");
            }
        }
    }
}
