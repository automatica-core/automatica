using Automatica.Core.Base.Cache;
using Automatica.Core.Control.Base;

namespace Automatica.Core.Control.Cache
{
    public interface IControlCache : IStore<Guid, IControl>
    {
    }
}
