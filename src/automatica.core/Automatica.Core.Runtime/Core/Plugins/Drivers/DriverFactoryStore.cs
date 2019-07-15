using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Drivers;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverFactoryStore : GuidStoreBase<IDriverFactory>, IDriverFactoryStore
    {
    }
}
