using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Abstraction.Plugins
{
    internal interface IPluginLoader<T> 
    {
        Task Load(T factory);
    }
}
