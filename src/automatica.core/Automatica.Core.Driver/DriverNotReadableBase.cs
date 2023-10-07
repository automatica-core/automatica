using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    public abstract class DriverNotReadableBase : DriverBase
    {
        protected DriverNotReadableBase(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning($"Read not implemented for {FullName}");
            return Task.FromResult(false);
        }
    }
}
