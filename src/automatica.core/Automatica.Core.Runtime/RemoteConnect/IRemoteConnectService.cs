using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.Runtime.RemoteConnect
{
    public interface IRemoteConnectService : IHostedService
    {
        Task ReloadAsync(CancellationToken cancellationToken = default);
        Task InitAsync(CancellationToken cancellationToken = default);
        Task<bool> IsRunning(CancellationToken token);

        Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string localIp, int localPort, Guid driverGuid, CancellationToken token);
        Task<string> CreateWebTunnelAsync(TunnelingProtocol protocol, string name, string subDomain, string localIp, int localPort, Guid driverGuid, string? basicUser, string? basicPassword, CancellationToken token);

    }
}
