using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Common
{
    public interface ISettingsCache : IStore<long, Setting>
    {
        void UpdateByKey(string key, Setting value);
        Setting GetByKey(string key);
    }
}
