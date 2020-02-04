using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Common
{
    public interface ISettingsCache : IStore<long, Setting>
    {
        Setting GetByKey(string key);
    }
}
