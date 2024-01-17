using System;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Push.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Automatica.Push.Hubs
{
    [Authorize]
    public class DataHub : Hub
    {
        private readonly IDispatcher _dispatcher;
        private readonly INotifyDriver _notify;
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly ILogicInterfaceInstanceCache _logicInterfaceInstanceCache;

        public DataHub(IDispatcher dispatcher, INotifyDriver notify, INodeInstanceCache nodeInstanceCache, ILogicInstanceCache logicInstanceCache, ILogicInterfaceInstanceCache logicInterfaceInstanceCache)
        {
            _dispatcher = dispatcher;
            _notify = notify;
            _nodeInstanceCache = nodeInstanceCache;
            _logicInstanceCache = logicInstanceCache;
            _logicInterfaceInstanceCache = logicInterfaceInstanceCache;
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

        public async Task EnableLearnMode(Guid nodeInstanceId)
        {
            var nodeInstance = _nodeInstanceCache.Get(nodeInstanceId);
            await _notify.EnableLearnMode(nodeInstance);
            await Subscribe(nodeInstanceId.ToString());
        }
        public async Task DisableLearnMode(Guid nodeInstanceId)
        {

            var nodeInstance = _nodeInstanceCache.Get(nodeInstanceId);
            await _notify.DisableLearnMode(nodeInstance);
            await Unsubscribe(nodeInstanceId.ToString());
        }

        public async Task Unsubscribe(string name)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
        }


        public void SetValue(Guid instanceId, JsonElement value)
        {
            object convertedValue = null;

            switch (value.ValueKind)
            {
                case JsonValueKind.Undefined:
                    break;
                case JsonValueKind.Object:
                    try
                    {
                        var dispatchValue = JsonConvert.DeserializeObject<DispatchValue>(value.GetRawText());
                        convertedValue = dispatchValue.Value;
                        break;
                    }
                    catch
                    {
                        //ignore
                    }
                    throw new NotImplementedException();
                case JsonValueKind.Array:
                    throw new NotImplementedException();
                case JsonValueKind.String:
                    convertedValue = value.GetString();
                    break;
                case JsonValueKind.Number:
                    convertedValue = value.GetDouble();
                    break;
                case JsonValueKind.True:
                    convertedValue = true;
                    break;
                case JsonValueKind.False:
                    convertedValue = false;
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var nodeInstanceValue = _nodeInstanceCache.Get(instanceId);

            if (nodeInstanceValue != null)
            {
                var dispatchable = new DispatchableInstance(DispatchableType.Visualization, $"Web", instanceId,
                    DispatchableSource.Visualization, nodeInstanceValue.IsRemanent);
                _dispatcher.DispatchValue(dispatchable,
                    new DispatchValue(instanceId, DispatchableType.Visualization, convertedValue, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, DispatchValueSource.User));
            }
            else
            {
                var dispatchable = new DispatchableInstance(DispatchableType.Visualization, $"Web", instanceId,
                    DispatchableSource.Visualization, false);
                _dispatcher.DispatchValue(dispatchable,
                    new DispatchValue(instanceId, DispatchableType.Visualization, convertedValue, DateTimeHelper.ProviderInstance.GetLocalNow().DateTime, DispatchValueSource.User));
            }
        }
    }
}
