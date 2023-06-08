using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.Tunneling
{
    public enum TunnelingProtocol
    {
        Http,
        Tcp
    }

    public interface ITunnelingProvider
    {
        Task<bool> IsAvailableAsync(CancellationToken token);

        Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string address, string targetDomain,
            CancellationToken token);
    }
}
