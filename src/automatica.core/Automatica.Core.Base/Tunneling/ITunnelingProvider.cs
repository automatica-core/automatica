using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Tunneling
{
    public enum TunnelingProtocol
    {
        Http,
        Tcp
    }

    public interface ITunnelingProvider
    {
        Task<bool> IsAvailableAsync(CancellationToken token);

        Task<bool> CreateTunnelAsync(TunnelingProtocol protocol, string address, string targetDomain,
            CancellationToken token);
    }
}
