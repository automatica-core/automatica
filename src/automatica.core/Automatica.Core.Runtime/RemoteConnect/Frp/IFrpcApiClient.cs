using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    public interface IFrpcApiClient
    {
        Task<bool> IsReady(CancellationToken cancellationToken = default);
    }
}
