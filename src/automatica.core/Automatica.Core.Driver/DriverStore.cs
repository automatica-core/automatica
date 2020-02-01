using System.Runtime.CompilerServices;
using Automatica.Core.Base.Cache;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Driver
{
    internal class DriverStore : GuidStoreBase<IDriver>, IDriverStore
    {
        public void Remove(IDriver driver)
        {
            Remove(driver.Id);
        }
    }
}
