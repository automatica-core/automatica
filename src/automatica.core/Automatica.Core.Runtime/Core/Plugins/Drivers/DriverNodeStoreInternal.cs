using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverNodesStoreInternal : IDriverNodesStoreInternal
    {
        private readonly IDriverNodesStore _store;

        public DriverNodesStoreInternal(IDriverNodesStore store)
        {
            _store = store;
        }

        public async Task ReInitialize(CancellationToken token = default)
        {
            foreach (var item in _store.All())
            {
                await item.OnReInit(token);
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

        public void RemoveDriver(IDriver driver)
        {
            _store.Remove(driver);

            foreach (var child in driver.Children)
            {
                RemoveNode(child);
            }
        }

        private void RemoveNode(IDriverNode node)
        {
            _store.Remove(node);
            foreach (var child in node.Children)
            {
                RemoveNode(child);
            }
        }
    }
}
