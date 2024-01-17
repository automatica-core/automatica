using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyClient : IShellyCommonClient
    {
        Task<bool> Connect(CancellationToken token = default);
        Task<bool> Disconnect(CancellationToken token = default);


        Task<bool> SetRelayState(int channelId, bool value, CancellationToken token = default);
        Task<bool> GetRelayState(int channelId, CancellationToken token = default);
        Task<double> GetRelayVoltage(int channelId, CancellationToken token = default);
        Task<double> GetRelayPower(int channelId, CancellationToken token = default);
        Task<double> GetRelayCurrent(int channelId, CancellationToken token = default);
        Task<double> GetRelayEnergy(int channelId, CancellationToken token = default);
        Task<long> GetRelayEnergyTimestamp(int channelId, CancellationToken token = default);


        Task<bool> GetHasUpdate(CancellationToken token = default);
    }
}
