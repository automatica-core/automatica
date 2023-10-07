using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Driver
{
    public interface IWriteContext
    {
        Task DispatchValue(object value, CancellationToken token = default);
    }
}
