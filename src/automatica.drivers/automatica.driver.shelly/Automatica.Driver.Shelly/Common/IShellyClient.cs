using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Driver.Shelly.Common
{
    public interface IShellyClient : IShellyCommonClient
    {
        Task<bool> SetRelayState(int channelId, bool value, CancellationToken token);
        Task<bool> GetRelayState(int channelId, CancellationToken token);



        Task<bool> GetHasUpdate(CancellationToken token);
    }
}
