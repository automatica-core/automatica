using System.Threading.Tasks;
using Automatica.Core.Base.Cache;
using Automatica.Core.Driver;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Drivers
{
    internal interface IDriverNodesStore : IStore<IDriverNode>
    {
        Task ReInitialize();
    }
}
