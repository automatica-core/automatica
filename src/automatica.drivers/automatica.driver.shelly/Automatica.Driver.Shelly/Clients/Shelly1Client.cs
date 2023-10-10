using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Dtos;
using Automatica.Driver.Shelly.Options;

namespace Automatica.Driver.Shelly.Clients
{
    public class Shelly1Client : ShellyClientBase, IShelly1
    {
        public Shelly1Client(ITelegramMonitorInstance telegramMonitor, HttpClient httpClient, Shelly1Options shelly1Options) : base(telegramMonitor, httpClient, shelly1Options)
        {
        }


        public async Task<ShellyResult<Shelly1StatusDto>> GetStatus(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            var endpoint = ServerUri + "/status";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await ExecuteRequestAsync<Shelly1StatusDto>(requestMessage, cancellationToken, timeout);
        }
    }
}