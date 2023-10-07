using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Driver
{
    public interface IReadContext
    {
        Task DispatchValue(object value, CancellationToken token = default);
    }
}
