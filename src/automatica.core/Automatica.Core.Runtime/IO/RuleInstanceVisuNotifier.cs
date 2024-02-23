using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.IO
{
    public class RuleInstanceVisuNotifier(IHubContext<DataHub> hub, ILogger<RuleInstanceVisuNotifier> logger) : IRuleInstanceVisuNotify
    {
        public Task NotifyValueChanged(RuleInterfaceInstance instance, DispatchValue value)
        {
            try
            {
                return hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.ObjId, value.Value);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error {nameof(NotifyValueChanged)} via hub {e}");
            }

            return Task.CompletedTask;
        }

        public Task NotifyValueChanged(IDispatchable instance, DispatchValue value)
        {
            try
            {
                return hub.Clients.All.SendAsync("RuleInstanceValueChanged", instance.Id, value.Value);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error in {nameof(NotifyValueChanged)} via hub {e}");
            }

            return Task.CompletedTask;
        }
    }
}
