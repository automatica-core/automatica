using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;

namespace Automatica.Core.Runtime.RemoteConnect
{
    internal class RemoteConnectProvider : ITunnelingProvider
    {
        private readonly IRemoteConnectService _tunnelingService;

        public RemoteConnectProvider(IRemoteConnectService tunnelingService)
        {
            _tunnelingService = tunnelingService;
        }

        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return _tunnelingService.IsRunning(token);
        }

        public Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort, int remotePort,
            CancellationToken token)
        {
            return _tunnelingService.CreateTunnelAsync(protocol, name, address, targetPort, remotePort, token);
        }
    }
}
