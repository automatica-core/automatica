using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen1.Models;
using Automatica.Driver.Shelly.Gen1.Options;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.Shelly.Gen1.Clients
{
    public class ShellyGen1Client : ShellyClientBase, IShellyClient
    {
        public ILogger Logger { get; }

        public ShellyGen1Client(ITelegramMonitorInstance telegramMonitor, ShellyOptions shelly1Options, ILogger logger) : base(telegramMonitor, shelly1Options)
        {
            Logger = logger;
        }

        public async Task<ShellyResult<ShellyStatusDto>> GetStatus(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            var endpoint = ServerUri + "/status";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await ExecuteRequestAsync<ShellyStatusDto>(requestMessage, cancellationToken, timeout);
        }

        public async Task<RelayDto> SetStatus(int channelId, bool value, CancellationToken token)
        {
            var turnValue = value ? "on" : "off";
            var endpoint = ServerUri + $"/relay/{channelId}?turn={turnValue}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);


            await TelegramMonitor.NotifyTelegram(TelegramDirection.Output, "self", ShellyHttpClient!.BaseAddress!.AbsoluteUri, turnValue, endpoint);

            return await ExecuteRequestSetAsync<RelayDto>(requestMessage, token, default);
        }

        public Task<bool> Connect(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> Disconnect(CancellationToken token = default)
        {
            return Task.FromResult(true);
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