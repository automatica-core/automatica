using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Push.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class DataHub : Hub
    {
        private readonly IDispatcher _dispatcher;
        private readonly INotifyDriver _notify;

        public DataHub(IDispatcher dispatcher, INotifyDriver notify)
        {
            _dispatcher = dispatcher;
            _notify = notify;
        }

        public async Task SubscribeAll()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "All");
        }
        public async Task UnsubscribeAll()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "All");
        }

        public async Task Subscribe(string name)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, name);
        }

        public async Task EnableLearnMode(NodeInstance node)
        {
            await _notify.EnableLearnMode(node);
            await Subscribe(node.ObjId.ToString());
        }
        public async Task DisalbeLearnMode(NodeInstance node)
        {
            await _notify.DisableLearnMode(node);
            await Unsubscribe(node.ObjId.ToString());
        }

        public async Task Unsubscribe(string name)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
        }

        public void SetValue(Guid nodeInstance, object value)
        {
            var dispatchable = new DispatchableInstance(DispatchableType.NodeInstance, $"Web", nodeInstance, DispatchableSource.Visualization);
            _dispatcher.DispatchValue(dispatchable, value);
        }
    }
}
