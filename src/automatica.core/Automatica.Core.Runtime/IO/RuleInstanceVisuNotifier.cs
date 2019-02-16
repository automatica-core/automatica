using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public class RuleInstanceVisuNotifier : IRuleInstanceVisuNotify
    {
        private readonly IHubContext<DataHub> hub;

        public RuleInstanceVisuNotifier(IHubContext<DataHub> hub)
        {
            this.hub = hub;
        }
        public async Task NotifyValueChanged(RuleInstance instance, object value)
        {
            await hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.ObjId, value);
        }
    }
}
