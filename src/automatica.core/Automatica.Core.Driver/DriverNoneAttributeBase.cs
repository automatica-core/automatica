using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    public abstract class DriverNoneAttributeBase : DriverBase
    {
        protected DriverNoneAttributeBase(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task<bool> Read(IReadContext writeContext, CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning($"Read not implemented for {FullName}");
            return Task.FromResult(false);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning($"Write not implemented for {FullName}");
            return Task.CompletedTask;
        }
    }
}
