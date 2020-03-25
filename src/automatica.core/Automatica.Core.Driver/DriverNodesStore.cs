using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;

namespace Automatica.Core.Driver
{
    internal class DriverNodesStore : GuidStoreBase<IDriverNode>, IDriverNodesStore
    {
        private readonly Dictionary<Guid, IDriver> _childRootMap = new Dictionary<Guid, IDriver>();

        public void AddChild(IDriver driver, IDriverNode child)
        {
            Add(child.Id, child);

            if (!_childRootMap.ContainsKey(child.Id))
            {
                _childRootMap.Add(child.Id, driver);
            }
        }

        public IDriver GetDriver(Guid child)
        {
            if (_childRootMap.ContainsKey(child))
            {
                return _childRootMap[child];
            }

            return null;
        }

        public void Remove(IDriverNode driverNode)
        {
            RemoveChildRoot(driverNode);
            foreach (var child in driverNode.Children)
            {
                RemoveChildRoot(child);
            }
            Remove(driverNode.Id);
        }

        private void RemoveChildRoot(IDriverNode driverNode)
        {
            if (_childRootMap.ContainsKey(driverNode.Id))
            {
                _childRootMap.Remove(driverNode.Id);
            }

        }

        public override void Clear()
        {
            base.Clear();
            _childRootMap.Clear();
        }
    }
}
