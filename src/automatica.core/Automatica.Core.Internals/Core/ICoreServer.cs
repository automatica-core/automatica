using System.Threading.Tasks;

namespace Automatica.Core.Internals.Core
{
    public interface ICoreServer
    {
        void Restart();
        Task ReInit();
    }
}
