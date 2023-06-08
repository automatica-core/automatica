using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Automatica.Core.Runtime.Tunneling
{
    public interface ITunnelingService : IHostedService
    {
        Task<bool> IsRunning(CancellationToken token);

        Task<bool> CreateTunnelAsync(Uri uri, string domain, CancellationToken token);
    }
}
