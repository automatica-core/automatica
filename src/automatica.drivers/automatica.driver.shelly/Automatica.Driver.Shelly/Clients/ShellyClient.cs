using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Dtos;
using Automatica.Driver.Shelly.Options;

namespace Automatica.Driver.Shelly.Clients
{
    public class ShellyClient : ShellyClientBase
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
    }
}