using System.Threading.Tasks;
using Automatica.Core.Base.BoardType;

namespace Automatica.Core.Runtime.Abstraction.Plugins
{
    internal interface IPluginLoader<T> 
    {
        Task Load(T factory, IBoardType boardType);
    }
}
