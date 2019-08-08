using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Driver
{
    internal interface ILoadedNodeInstancesStore : IStore<NodeInstance>, INodeInstanceStateHandler
    {
    }
}
