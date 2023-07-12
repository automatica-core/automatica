using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.Tunneling
{
    public enum TunnelingProtocol
    {
        Http,
        Https,
        Tcp,
        Udp
    }

    public interface ITunnelingProvider
    {

        Task<bool> IsAvailableAsync(CancellationToken token);
        
        Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort, CancellationToken token);

        Task<string> CreateWebTunnelAsync(TunnelingProtocol protocol, string name, string subDomain, string localIp,
            int localPort, string basicUser, string basicPassword, CancellationToken token);
    }
}
