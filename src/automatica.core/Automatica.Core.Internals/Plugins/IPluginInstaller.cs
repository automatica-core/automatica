using System.Threading.Tasks;
using Automatica.Core.Common.Update;
using Plugin = Automatica.Core.EF.Models.Plugin;

namespace Automatica.Core.Internals.Plugins
{
    public interface IPluginInstaller
    {
        Task<bool> InstallPlugin(Plugin plugin, string acPkgFilePath);
        Task<bool> InstallPlugin(PluginManifest plugin, string acPkgFilePath);
    }
}
