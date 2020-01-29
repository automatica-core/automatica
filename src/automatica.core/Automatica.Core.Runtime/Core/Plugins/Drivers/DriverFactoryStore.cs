using Automatica.Core.Base.Cache;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverFactoryStore : GuidStoreBase<IDriverFactory>, IDriverFactoryStore
    {
    }
}
