using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Automatica.Core.Runtime.Core
{
    internal class DataBroadcastService : IDataBroadcast
    {
        private readonly IHubContext<DataHub> _dataHub;

        public DataBroadcastService(IHubContext<DataHub> dataHub)
        {
            _dataHub = dataHub;
        }
        public async Task DispatchValue(DispatchableType type, Guid id, object value)
        {
            await _dataHub.Clients.All.SendAsync("dispatchValue", type, id, value);
            await _dataHub.Clients.Group(id.ToString()).SendAsync("dispatchValue", type, id, value);
        }
    }
}
