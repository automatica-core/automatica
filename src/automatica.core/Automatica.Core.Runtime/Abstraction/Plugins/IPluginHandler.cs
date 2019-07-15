using System.Threading.Tasks;
using Automatica.Core.Internals.Plugins;

namespace Automatica.Core.Runtime.Abstraction.Plugins
{
    internal interface IPluginHandler : IPluginLoader
    {
        Task CheckAndInstallPluginUpdates();
    }
}
