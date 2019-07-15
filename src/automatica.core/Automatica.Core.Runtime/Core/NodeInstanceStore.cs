using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Abstraction;
using Automatica.Core.Runtime.Core.Plugins;

namespace Automatica.Core.Runtime.Core
{
    internal class NodeInstanceStore : GuidStoreBase<NodeInstance>, INodeInstanceStore
    {
    }
}
