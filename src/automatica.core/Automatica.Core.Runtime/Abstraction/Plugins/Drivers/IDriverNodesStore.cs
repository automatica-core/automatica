using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.RemoteNode;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Drivers
{
    public interface IDriverNodesStoreInternal
    {
        Task ReInitialize();
        void Clear();
        void Add(IDriverNode node);
    }
}
