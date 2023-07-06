using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;

namespace Automatica.Core.Plugin.Standalone.Tunneling
{
    internal class RemoteTunnelingProvider : ITunnelingProvider
    {
        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return Task.FromResult(false);
        }

        public Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort,
            CancellationToken token)
        {
            return Task.FromResult("");
        }
    }
}
