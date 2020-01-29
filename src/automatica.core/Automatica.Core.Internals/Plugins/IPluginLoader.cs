using System.Threading.Tasks;

namespace Automatica.Core.Internals.Plugins
{
    public interface IPluginLoader
    {
        Task LoadPlugin(EF.Models.Plugin plugin);
    }
}
