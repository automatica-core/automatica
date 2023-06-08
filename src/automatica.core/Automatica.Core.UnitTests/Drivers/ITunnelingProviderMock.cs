using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Tunneling;

namespace Automatica.Core.UnitTests.Base.Drivers
{
    internal class ITunnelingProviderMock : ITunnelingProvider
    {
        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CreateTunnelAsync(TunnelingProtocol protocol, string address, string targetDomain, CancellationToken token)
        {

            return Task.FromResult(false);
        }
    }
}
