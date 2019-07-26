using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Drivers;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverNodesStoreInternal : IDriverNodesStoreInternal
    {
        private readonly IDriverNodesStore _store;

        public DriverNodesStoreInternal(IDriverNodesStore store)
        {
            _store = store;
        }

        public async Task ReInitialize()
        {
            foreach (var item in _store.All())
            {
                await item.OnReinit();
            }
        }

        public void Clear()
        {
            _store.Clear();
        }

        public void Add(IDriverNode node)
        {
            _store.Add(node.Id, node);
        }
    }
}
