using System;
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

        public async Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string address, string targetDomain, CancellationToken token)
        {
            var uriPrefix = "http://";
            if (protocol == TunnelingProtocol.Tcp)
            {
                uriPrefix = "tcp://";
            }
            var uri = new Uri($"{uriPrefix}{address}");

            return await _tunnelingService.CreateTunnelAsync(uri, targetDomain, token);
        }

        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return _tunnelingService.IsRunning(token);
        }
    }
}
