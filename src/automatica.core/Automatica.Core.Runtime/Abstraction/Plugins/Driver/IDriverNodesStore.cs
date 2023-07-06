using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Driver
{
    public interface IDriverNodesStoreInternal
    {
        Task ReInitialize(CancellationToken token = default);
        void Clear();
        void Add(IDriverNode node);
        void RemoveDriver(IDriver driver);
    }
}
