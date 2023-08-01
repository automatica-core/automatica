using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal interface IFrpService
    {
        Task<bool> InitConfigurationsAsync(CancellationToken cancellationToken = default);

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
