using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    public abstract class DriverAsyncBase : DriverBase
    {
        protected DriverAsyncBase(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning($"Driver is async {FullName}");
            return Task.FromResult(false);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning($"Driver is async {FullName}");
            return Task.CompletedTask;
        }
    }
}
