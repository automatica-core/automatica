using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    internal class TunnelingProviderMock : ITunnelingProvider
    {
        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return Task.FromResult(false);
        }

        public Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort, int remotePort,
            CancellationToken token)
        {
            return Task.FromResult("");
        }
    }
}
