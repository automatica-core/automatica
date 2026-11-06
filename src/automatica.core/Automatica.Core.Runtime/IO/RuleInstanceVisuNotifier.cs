using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.IO
{
    /// <summary>
    /// Sends logic interface values only to the connections that subscribed the interface (DataHub.Subscribe checks the
    /// user group of the logic) and to the admins' "All" group, never to every client.
    /// </summary>
    public class RuleInstanceVisuNotifier(IHubContext<DataHub> hub, ILogger<RuleInstanceVisuNotifier> logger) : IRuleInstanceVisuNotify
    {
        private Task Send(Guid id, object value)
        {
            return hub.Clients.Groups("All", id.ToString()).SendAsync("RuleInstanceValueChanged", id, value);
        }

        public Task NotifyValueChanged(RuleInterfaceInstance instance, DispatchValue value)
        {
            try
            {
                return Send(instance.ObjId, value.Value);
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
                return Send(instance.Id, value.Value);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Error in {nameof(NotifyValueChanged)} via hub {e}");
            }

            return Task.CompletedTask;
        }
    }
}
