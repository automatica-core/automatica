using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Driver.Shelly.Common;
using Automatica.Driver.Shelly.Gen1.Clients;
using Automatica.Driver.Shelly.Gen1.Options;
using Automatica.Driver.Shelly.Gen2.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PureWebSockets;

namespace Automatica.Driver.Shelly.Gen2
{
    public class ShellyGen2Client: ShellyClientBase, IShellyClient
    {
        private readonly IShellyCommonOptions _shellyCommonOptions;
        private readonly ILogger _logger;
        private PureWebSocket _webSocket;
        private AuthModel? _authModel;


        public event NotifyEvent OnNotifyEvent;
        public event Opened OnOpened;
        public event Closed OnClosed;

        public delegate void NotifyEvent(object sender, NotifyStatusEvent eventMessage);

        public ShellyGen2Client(ITelegramMonitorInstance telegramMonitor, IShellyCommonOptions shellyCommonOptions, ILogger logger) : base(telegramMonitor, shellyCommonOptions)
        {
            _shellyCommonOptions = shellyCommonOptions;
            _logger = logger;

            CreateWebSocket();
        }

        private void CreateWebSocket()
        {
            if (_webSocket != null)
            {
                _webSocket.OnOpened -= _webSocket_OnOpened;
                _webSocket.OnMessage -= _webSocket_OnMessage;
                _webSocket.OnClosed -= _webSocket_OnClosed;
                _webSocket.OnError -= WebSocketOnOnError;
                _webSocket.Dispose();
            }

            _webSocket = new PureWebSocket($"ws://{_shellyCommonOptions.IpAddress}/rpc", new PureWebSocketOptions());
            _webSocket.OnOpened += _webSocket_OnOpened;
            _webSocket.OnMessage += _webSocket_OnMessage;
            _webSocket.OnClosed += _webSocket_OnClosed;
            _webSocket.OnError += WebSocketOnOnError;
        }

        private async void WebSocketOnOnError(object sender, Exception ex)
        {
            _logger.LogInformation($"Websocket closed...." + ex);

            await Restart();
            OnClosed?.Invoke(this, WebSocketCloseStatus.Empty);
        }

        private async Task Restart()
        {
            CreateWebSocket();
            await Connect();
        }
       

        private async void _webSocket_OnClosed(object sender, WebSocketCloseStatus reason)
        {
            _logger.LogInformation($"Websocket closed...." + reason);

            await Restart();
            OnClosed?.Invoke(this, reason);
        }

        private void _webSocket_OnMessage(object sender, string message)
        {
            _logger.LogInformation($"Websocket message...." + message);

            var response = JsonConvert.DeserializeObject<ResponseModel<object>>(message);

            if (response.Method == "NotifyStatus")
            {
                OnNotifyEvent?.Invoke(this, JsonConvert.DeserializeObject<NotifyStatusEvent>(JsonConvert.SerializeObject(response.Params)));
            }

            if (response.Error != null && response.Error.Code == 401)
            {
           
                _authModel = AuthModel.CreateAuthModel(ShellyCommonOptions.Password, ((AuthModel)response.Error.MessageObject).Realm,
                    ((AuthModel)response.Error.MessageObject).Nonce, new Random().NextInt64(long.MaxValue), "admin", "SHA-256");
                
            }
        }

        private async Task<ShellyResult<ResponseModel<T>>> SendPostMessage<T>(string method, Dictionary<string, object> param, CancellationToken token = default)
        {
            var rpcModel = new RpcModel
            {
                Id = $"{Guid.NewGuid()}",
                Method = method,
                Params = param,
                Auth = _authModel,
                Source = $"{ShellyCommonOptions.SourceId}"
            };
            var endpoint = ServerUri + "/rpc";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(rpcModel), Encoding.UTF8);

            return await ExecuteRequestAsync<ResponseModel<T>>(requestMessage, token, default);
        }

        private async void _webSocket_OnOpened(object sender)
        {
            _logger.LogInformation($"Websocket opened....");

            await SendWebSocketMessage(new RpcModel
            {
                Id = "1", Method = "Shelly.GetStatus", Source = $"{ShellyCommonOptions.SourceId}"
            });

            OnOpened?.Invoke(this);
        }

        private async Task SendWebSocketMessage(RpcModel message)
        {
            await _webSocket.SendAsync(JsonConvert.SerializeObject(message));
        }

        public async Task<bool> Connect(CancellationToken token = default)
        {
            try
            {
                return await _webSocket.ConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Disconnect(CancellationToken token = default)
        {
            _webSocket.OnOpened -= _webSocket_OnOpened;
            _webSocket.OnMessage -= _webSocket_OnMessage;
            _webSocket.OnClosed -= _webSocket_OnClosed;

            if (_webSocket is { State: WebSocketState.Open })
            {
                _webSocket?.Disconnect();
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> SetRelayState(int channelId, bool value, CancellationToken token = default)
        {
             await SendPostMessage<SwitchStatus>("Switch.Set",
                new Dictionary<string, object> { { "id", channelId}, { "on", value } }, token);
            return true;
        }

        public async Task<bool> GetRelayState(int channelId, CancellationToken token = default)
        {
            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);

            return switchState.Value.Result.Output;
        }

        public async Task<double> GetRelayVoltage(int channelId, CancellationToken token = default)
        {
            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);
            return switchState.Value.Result.Voltage;
        }

        public async Task<double> GetRelayPower(int channelId, CancellationToken token = default)
        {

            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);
            return switchState.Value.Result.Apower;
        }

        public async Task<double> GetRelayCurrent(int channelId, CancellationToken token = default)
        {
            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);
            return switchState.Value.Result.Current;
        }

        public async Task<double> GetRelayEnergy(int channelId, CancellationToken token = default)
        {
            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);
            return switchState.Value.Result.Aenergy.Total;
        }

        public async Task<long> GetRelayEnergyTimestamp(int channelId, CancellationToken token = default)
        {
            var switchState = await SendPostMessage<SwitchStatus>("Switch.GetStatus",
                new Dictionary<string, object> { { "id", channelId } }, token);
            return switchState.Value.Result.Aenergy.MinuteTs;
        }

        public Task<bool> GetHasUpdate(CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

    }
}
