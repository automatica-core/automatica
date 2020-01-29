using System.Threading.Tasks;
using Automatica.Core.Internals.Core;

namespace Automatica.Core.Runtime.Abstraction
{
    public interface IUpdateHandler : IAutoUpdateHandler
    {
        Task Init();
    }
}
