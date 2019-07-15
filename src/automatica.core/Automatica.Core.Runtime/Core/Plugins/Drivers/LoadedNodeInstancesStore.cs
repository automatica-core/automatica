using System;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Abstraction.Plugins.Drivers;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class LoadedNodeInstancesStore : GuidStoreBase<NodeInstance>, ILoadedNodeInstancesStore
    {
        public NodeInstanceState GetNodeInstanceState(Guid id)
        {
            if (Contains(id))
            {
                return Get(id).State;
            }
            return NodeInstanceState.Unknown;
        }
    }
}
