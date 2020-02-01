using System;
using Automatica.Core.Base.Cache;

namespace Automatica.Core.Driver
{
    public interface IDriverNodesStore : IStore<IDriverNode>
    {
        void AddChild(IDriver driver, IDriverNode child);
        IDriver GetDriver(Guid child);

        void Remove(IDriverNode driverNode);
    }
}
