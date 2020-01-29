using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Driver
{
    public interface IDriverNodesStoreInternal
    {
        Task ReInitialize();
        void Clear();
        void Add(IDriverNode node);
    }
}
