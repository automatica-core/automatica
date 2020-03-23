using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public class RuleInstanceVisuNotifier : IRuleInstanceVisuNotify
    {
        private readonly IHubContext<DataHub> _hub;

        public RuleInstanceVisuNotifier(IHubContext<DataHub> hub)
        {
            this._hub = hub;
        }
        public async Task NotifyValueChanged(RuleInterfaceInstance instance, object value)
        {
            await _hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.ObjId, value);
        }
    }
}
