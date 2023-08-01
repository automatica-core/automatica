using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    public interface IFrpProcess
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
