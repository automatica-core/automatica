using System.Threading.Tasks;

namespace Automatica.Core.Internals.Core
{
    public interface IAutoUpdateHandler
    {
        Task ReInitialize();
        Task Update();
    }
}
