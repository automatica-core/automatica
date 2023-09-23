using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Abstraction.Remote;

namespace Automatica.Core.Runtime.RemoteNode
{
    internal class RemoteNodeInstance : IDriverNode
    {
        private readonly Guid _driverInstanceGuid;
        private readonly NodeInstance _node;
        private readonly IRemoteHandler _remoteHandler;
        public DispatchableSource Source => DispatchableSource.NodeInstance;
        public DispatchableType Type => DispatchableType.NodeInstance;
        public string Name => _node.Name;
        public Guid Id => _node.ObjId;
        public bool IsRemanent => _node.IsRemanent;
        public IDriverContext DriverContext => null;
        public IList<IDriverNode> Children => new List<IDriverNode>();

        public RemoteNodeInstance(Guid driverInstanceGuid, NodeInstance node, IRemoteHandler remoteHandler)
        {
            _driverInstanceGuid = driverInstanceGuid;
            _node = node;
            _remoteHandler = remoteHandler;
        }
        public Task<bool> Configure(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Init(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public IDriverNode Parent { get; set; }
        public Task<bool> Start(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Started(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Stop(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Stopped(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        //TODO: Implement
        public async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            await Task.CompletedTask;
            return new List<NodeInstance>();
        }

        //TODO: Implement
        public async Task<IList<NodeInstance>> Import(string fileName, CancellationToken token = default)
        {
            await Task.CompletedTask;
            return new List<NodeInstance>();
        }

        //TODO: Implement
        public async Task<IList<NodeInstance>> Import(ImportConfig config, CancellationToken token = default)
        {
            await Task.CompletedTask;
            return new List<NodeInstance>();
        }

        //TODO: Implement
        public async Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default)
        {
            await Task.CompletedTask;
            return new List<NodeInstance>();
        }

        //TODO: Implement
        public Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> Read(CancellationToken token = default)
        {
            _remoteHandler.SendAction(_driverInstanceGuid, DriverNodeRemoteAction.Read, this);
            return Task.FromResult(true);
        }

        //TODO: Implement
        public Task OnSave(NodeInstance instance, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        //TODO: Implement
        public Task OnDelete(NodeInstance instance, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        //TODO: Implement
        public Task OnReInit(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        //TODO: Implement
        public int ChildrensCreated => 0;
        public NodeInstanceState State => NodeInstanceState.InUse;

        public async Task<bool> EnableLearnMode(CancellationToken token = default)
        {
            await _remoteHandler.SendAction(_driverInstanceGuid, DriverNodeRemoteAction.StartLearnMode, this);

            return true;
        }

        public async Task<bool> DisableLearnMode(CancellationToken token = default)
        {
            await _remoteHandler.SendAction(_driverInstanceGuid, DriverNodeRemoteAction.StopLearnMode, this);
            return true;
        }
    }
}
