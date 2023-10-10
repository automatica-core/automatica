using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Dtos;
using Automatica.Driver.Shelly.Options;

namespace Automatica.Driver.Shelly.Clients
{
    public class Shelly1PmClient : ShellyClientBase, IShelly1Pm
    {
        public Shelly1PmClient(ITelegramMonitorInstance telegramMonitor, HttpClient httpClient, Shelly1PmOptions shellyOptions) : base(telegramMonitor, httpClient, shellyOptions)
        {
        }
    
        public async Task<ShellyResult<Shelly1PmStatusDto>> GetStatus(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            var endpoint = ServerUri + "/status";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await ExecuteRequestAsync<Shelly1PmStatusDto>(requestMessage, cancellationToken, timeout);
        }
    }
}