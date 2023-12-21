using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public class RuleInstanceVisuNotifier(IHubContext<DataHub> hub) : IRuleInstanceVisuNotify
    {
        public Task NotifyValueChanged(RuleInterfaceInstance instance, object value)
        {
            return hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.ObjId, value);
        }
        public Task NotifyValueChanged(IDispatchable instance, DispatchValue value)
        {
            return hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.Id, value.Value);
        }
    }
}
