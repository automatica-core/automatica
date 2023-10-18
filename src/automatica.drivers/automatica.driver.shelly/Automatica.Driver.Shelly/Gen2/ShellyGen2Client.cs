﻿using System;
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
        private readonly ILogger _logger;
        private readonly PureWebSocket _webSocket;
        private AuthModel? _authModel;


        public event NotifyEvent OnNotifyEvent;
        public event Opened OnOpened;
        public event Closed OnClosed;

        public delegate void NotifyEvent(object sender, NotifyStatusEvent eventMessage);

        public ShellyGen2Client(ITelegramMonitorInstance telegramMonitor, IShellyCommonOptions shellyCommonOptions, ILogger logger) : base(telegramMonitor, shellyCommonOptions)
        {
            _logger = logger;
            _webSocket = new PureWebSocket($"ws://{shellyCommonOptions.IpAddress}/rpc", new PureWebSocketOptions());
            _webSocket.OnOpened += _webSocket_OnOpened;
            _webSocket.OnMessage += _webSocket_OnMessage;
            _webSocket.OnClosed += _webSocket_OnClosed;
          
        }

        private async Task Restart()
        {
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

            var response = JsonConvert.DeserializeObject<ResponseModel>(message);

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

        private async void _webSocket_OnOpened(object sender)
        {
            _logger.LogInformation($"Websocket opened....");

            await SendWebSocketMessage(new RpcModel
            {
                Id = 1, Method = "Shelly.GetStatus", Source = $"{ShellyCommonOptions.SourceId}"
            });

            OnOpened?.Invoke(this);
        }

        private async Task SendWebSocketMessage(RpcModel message)
        {
            await _webSocket.SendAsync(JsonConvert.SerializeObject(message));
        }

        public async Task<bool> Connect(CancellationToken token = default)
        {
            return await _webSocket.ConnectAsync();
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

        public Task<bool> SetRelayState(int channelId, bool value, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> GetRelayState(int channelId, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> GetHasUpdate(CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

    }
}