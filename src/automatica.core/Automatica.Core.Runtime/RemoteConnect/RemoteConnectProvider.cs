using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.Common.Update;
using Automatica.Core.Driver;

namespace Automatica.Core.Runtime.RemoteConnect
{
    internal class RemoteConnectProvider : ITunnelingProvider
    {
        private readonly IRemoteConnectService _tunnelingService;
        private readonly IDriverContext _driverContext;

        public RemoteConnectProvider(IRemoteConnectService tunnelingService, IDriverContext driverContext)
        {
            _tunnelingService = tunnelingService;
            _driverContext = driverContext;
        }

        public Task<bool> IsAvailableAsync(CancellationToken token)
        {
            return _tunnelingService.IsRunning(token);
        }

        public Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string address, int targetPort,
            CancellationToken token)
        {
            return _tunnelingService.CreateTunnelAsync(protocol, name, address, targetPort, _driverContext.Factory.DriverGuid, token);
        }
    }
}
