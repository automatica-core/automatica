using Automatica.Core.Base.Cache;
using Automatica.Core.Common.Update;
using Automatica.Core.Runtime.Abstraction.Plugins;

namespace Automatica.Core.Runtime.Core.Plugins
{
    internal class LoadedStore : GuidStoreBase<PluginManifest>, ILoadedStore
    {
    }
}
