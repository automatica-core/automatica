using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Abstraction;
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
        public IDriverContext DriverContext => null;
        public IList<IDriverNode> Children => new List<IDriverNode>();

        public RemoteNodeInstance(Guid driverInstanceGuid, NodeInstance node, IRemoteHandler remoteHandler)
        {
            _driverInstanceGuid = driverInstanceGuid;
            _node = node;
            _remoteHandler = remoteHandler;
        }
        public bool Configure()
        {
            return true;
        }

        public bool Init()
        {

            return true;
        }

        public IDriverNode Parent { get; set; }
        public Task<bool> Start()
        {
            return Task.FromResult(true);
        }

        public Task<bool> Stop()
        {

            return Task.FromResult(true);
        }

        public Task<IList<NodeInstance>> Scan()
        {
            return null;
        }

        public Task<IList<NodeInstance>> Import(string fileName)
        {

            return null;
        }

        public Task<IList<NodeInstance>> CustomAction(string actionName)
        {

            return null;
        }

        public Task WriteValue(IDispatchable source, object value)
        {
            return Task.CompletedTask;
        }

        public Task<bool> Read()
        {
            return Task.FromResult(true);
        }

        public Task OnSave(NodeInstance instance)
        {
            return Task.CompletedTask;
        }

        public Task OnDelete(NodeInstance instance)
        {
            return Task.CompletedTask;
        }

        public Task OnReinit()
        {
            return Task.CompletedTask;
        }

        public int ChildrensCreated => 0;
        public NodeInstanceState State => NodeInstanceState.InUse;

        public async Task<bool> EnableLearnMode()
        {
            await _remoteHandler.SendAction(_driverInstanceGuid, DriverNodeRemoteAction.StartLearnMode, this);

            return true;
        }

        public async Task<bool> DisableLearnMode()
        {
            await _remoteHandler.SendAction(_driverInstanceGuid, DriverNodeRemoteAction.StopLearnMode, this);
            return true;
        }
    }
}
