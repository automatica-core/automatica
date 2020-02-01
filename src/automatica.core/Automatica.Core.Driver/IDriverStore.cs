using Automatica.Core.Base.Cache;

namespace Automatica.Core.Driver
{
    public interface IDriverStore : IStore<IDriver>
    {
        void Remove(IDriver driver);
    }
}
