using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.Runtime.RemoteConnect
{
    public interface IRemoteConnectService : IHostedService
    {
        Task<bool> IsRunning(CancellationToken token);

        Task<string> CreateTunnelAsync(Uri uri, string domain, CancellationToken token);
    }
}
