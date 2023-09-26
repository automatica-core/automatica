using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Driver
{
    public interface IWriteContext
    {
        public bool WriteOnlyIfChanged { get; }
        Task DispatchValue(object value, CancellationToken token = default);
    }
}
