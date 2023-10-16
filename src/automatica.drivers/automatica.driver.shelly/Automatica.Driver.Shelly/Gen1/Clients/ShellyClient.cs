using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen1.Dtos;
using Automatica.Driver.Shelly.Gen1.Options;

namespace Automatica.Driver.Shelly.Gen1.Clients
{
    public class ShellyClient : ShellyClientBase, IShellyClient
    {
        public ShellyClient(ITelegramMonitorInstance telegramMonitor, HttpClient httpClient, ShellyOptions shelly1Options) : base(telegramMonitor, httpClient, shelly1Options)
        {
        }

        public async Task<ShellyResult<ShellyStatusDto>> GetStatus(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            var endpoint = ServerUri + "/status";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await ExecuteRequestAsync<ShellyStatusDto>(requestMessage, cancellationToken, timeout);
        }

        public async Task<bool> SetRelayState(int channelId, bool value, CancellationToken token)
        {
            var result =  await SetStatus(channelId, value, token);
            return result.IsOn;
        }

        public async Task<bool> GetRelayState(int channelId, CancellationToken token)
        {
            var status = await GetStatus(token);

            return status.Value.Relays[channelId].IsOn;
        }

        public async Task<bool> GetHasUpdate(CancellationToken token)
        {
            var status = await GetStatus(token);

            return status.Value.HasUpdate;
        }
    }
}