using Automatica.Core.Base.Cache;
using Automatica.Core.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Drivers;

namespace Automatica.Core.Runtime.Core.Plugins.Drivers
{
    internal class DriverStore : GuidStoreBase<IDriver>, IDriverStore
    {
    }
}
