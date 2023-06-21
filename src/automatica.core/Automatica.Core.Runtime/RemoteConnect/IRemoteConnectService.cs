using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.Runtime.RemoteConnect
{
    public interface IRemoteConnectService : IHostedService
    {
        Task InitAsync();
        Task<bool> IsRunning(CancellationToken token);
        
        Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string localIp, int localPort, int remotePort, CancellationToken token);
     
    }
}
