using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace P3.Driver.Automatica.Remote
{
    public class AutomaticaRemoteDriver : DriverBase
    {
        public IPAddress RemoteIpAddress { get; private set; }
        public int RemotePort { get; private set; }
        public HttpClient Client { get; } = new HttpClient();

        private HubConnection _hubConnection;

        private readonly Dictionary<Guid, AutomaticaRemoteNode> _nodeMap = new Dictionary<Guid, AutomaticaRemoteNode>();
    
        public Dictionary<Guid, AutomaticaRemoteNode> Nodes { get; } = new Dictionary<Guid, AutomaticaRemoteNode>();

        public AutomaticaRemoteDriver(IDriverContext driverContext) : base(driverContext)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var ipProp = GetProperty("automatica.remote.ip");
            RemoteIpAddress = IPAddress.Parse(ipProp.ValueString);

            RemotePort = GetPropertyValueInt("automatica.remote.port");
            return base.Init(token);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            var connectionBuilder = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder();
            _hubConnection = connectionBuilder.WithUrl($"http://{RemoteIpAddress}:{RemotePort}/signalr/dataHub").Build();

            _hubConnection.On<DispatchableType, Guid, object>("dispatchValue",  ValueDispatched);

            _hubConnection.StartAsync().ContinueWith(async (a) =>
            {
                if (a.IsCompletedSuccessfully)
                {
                    await _hubConnection.SendAsync("SubscribeAll");
                }
            });

            return base.Start(token);
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            _hubConnection?.SendCoreAsync("SetValue", new [] {source.Id, value});
            return base.WriteValue(source, value);
        }

        private void ValueDispatched(DispatchableType type, Guid nodeId, object value)
        {
            if (_nodeMap.ContainsKey(nodeId))
            {
                _nodeMap[nodeId].DispatchValue(value);
            }
        }

        public void AddRemoteNode(AutomaticaRemoteNode node, Guid guid)
        {
            _nodeMap.Add(guid, node);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = new AutomaticaRemoteNode(ctx, this);
            _nodeMap.Add(ctx.NodeInstance.ObjId, node);

            return node;
        }

        public override async Task OnSave(NodeInstance instance, CancellationToken token = default)
        {
            var json = JsonConvert.SerializeObject(instance.InverseThis2ParentNodeInstanceNavigation);
            var post = await Client.PostAsync($"http://{RemoteIpAddress}:{RemotePort}/webapi/nodeInstances",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!post.IsSuccessStatusCode)
            {
                DriverContext.Logger.LogError("Could not save data for remote node");
            }
        }

        public override async Task OnReInit(CancellationToken token = default)
        {
            var reload = await Client.GetAsync($"http://{RemoteIpAddress}:{RemotePort}/webapi/server");

            if (!reload.IsSuccessStatusCode)
            {
                DriverContext.Logger.LogError("Could not reinit remote node");
            }
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var nodeString = await Client.GetStringAsync($"http://{RemoteIpAddress}:{RemotePort}/webapi/nodeInstances");
            
            var data = JsonConvert.DeserializeObject<List<NodeInstance>>(nodeString);
            
            return data;
        }
    }
}
