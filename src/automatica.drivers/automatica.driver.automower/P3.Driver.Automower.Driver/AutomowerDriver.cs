using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Automower.Driver
{
    /// <summary>
    /// A Husqvarna account (application key/secret). Reads all mowers at the polling interval and keeps the event web socket open.
    /// </summary>
    internal sealed class AutomowerDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<AutomowerMowerNode> _mowers = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);

        private string _webSocketUrl;
        private TimeSpan _poll;
        private Timer _timer;
        private CancellationTokenSource _cts;
        private Task _loop;

        internal HttpClient Http { get; set; } = SharedHttp;

        /// <summary>
        /// Husqvarna asks for a ping every 60 s (no pong comes back)
        /// </summary>
        internal TimeSpan PingInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Sockets live at most 2 hours
        /// </summary>
        internal TimeSpan SocketLifetime { get; set; } = TimeSpan.FromMinutes(110);
        internal TimeSpan MinReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);
        internal TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(30);

        internal AutomowerApi Api { get; private set; }
        internal IReadOnlyList<AutomowerMowerNode> Mowers => _mowers;
        internal string Status { get; private set; }

        public AutomowerDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(300, GetPropertyValueInt(AutomowerDriverFactory.PollKey)));
            var authUrl = GetPropertyValueString(AutomowerDriverFactory.AuthUrlKey);
            var apiUrl = GetPropertyValueString(AutomowerDriverFactory.ApiUrlKey);
            _webSocketUrl = GetPropertyValueString(AutomowerDriverFactory.WebSocketUrlKey);
            if (string.IsNullOrWhiteSpace(_webSocketUrl))
            {
                _webSocketUrl = AutomowerDriverFactory.DefaultWebSocketUrl;
            }

            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(authUrl) ? AutomowerDriverFactory.DefaultAuthUrl : authUrl.Trim(),
                GetPropertyValueString(AutomowerDriverFactory.ApplicationKeyKey)?.Trim(), GetPropertyValueString(AutomowerDriverFactory.ApplicationSecretKey)?.Trim(), null)
            {
                ClientCredentials = true
            };
            Api = new AutomowerApi(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(apiUrl) ? AutomowerDriverFactory.DefaultApiUrl : apiUrl.Trim(), tokens);
            if (!tokens.HasCredentials)
            {
                DriverContext.Logger.LogError("No Husqvarna application key / secret configured");
            }
            return base.Init(token);
        }

        private void SetAccountValue(string key, object value)
        {
            foreach (var node in _accountValues.Where(v => v.Key == key))
            {
                node.Set(value);
            }
        }

        private void SetStatus(string status)
        {
            Status = status;
            SetAccountValue(AutomowerDriverFactory.StatusKey, status);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_cts.Token));
            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            if (_cts != null)
            {
                await _cts.CancelAsync();
                try
                {
                    await (_loop ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(10), token);
                }
                catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
                {
                    // stopping anyway
                }
                _cts.Dispose();
                _cts = null;
            }
            return await base.Stop(token);
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                if (Api.Tokens.HasCredentials && !Api.IsBlocked)
                {
                    await PollAsync();
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError($"Automower poll failed: {ex.Message}");
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            var delay = MinReconnectDelay;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Api.Tokens.HasCredentials)
                    {
                        SetStatus("No application key / secret configured");
                        return;
                    }
                    await PollAsync(token);
                    SetStatus("OK");
                    var opened = DateTimeOffset.UtcNow;
                    await ConnectWebSocketAsync(token);
                    if (DateTimeOffset.UtcNow - opened > TimeSpan.FromMinutes(1))
                    {
                        delay = MinReconnectDelay;
                        continue;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (RateLimitedException ex)
                {
                    SetStatus(ex.Message);
                    await DelayAsync(ex.Until - DriverContext.TimeProvider.GetUtcNow(), token);
                    continue;
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError($"Automower: {ex.Message}, retrying in {delay.TotalSeconds:0}s");
                    SetStatus(ex.Message);
                }

                await DelayAsync(delay, token);
                delay = TimeSpan.FromTicks(Math.Min(MaxReconnectDelay.Ticks, delay.Ticks * 2));
            }
        }

        private static async Task DelayAsync(TimeSpan delay, CancellationToken token)
        {
            try
            {
                await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, token);
            }
            catch (OperationCanceledException)
            {
                // stopping
            }
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                var mowers = await Api.GetMowersAsync(token);
                foreach (var node in _mowers)
                {
                    var mower = mowers.FirstOrDefault(m => m.Id == node.MowerId);
                    if (mower != null)
                    {
                        node.Replace(mower.Attributes);
                    }
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        internal async Task ConnectWebSocketAsync(CancellationToken token = default)
        {
            var accessToken = await Api.Tokens.GetAccessTokenAsync(false, token);
            using var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
            await socket.ConnectAsync(new Uri(_webSocketUrl), token);
            SetAccountValue(AutomowerDriverFactory.SocketConnectedKey, true);
            DriverContext.Logger.LogInformation("Automower web socket connected");

            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            lifetime.CancelAfter(SocketLifetime);
            var ping = PingAsync(socket, lifetime.Token);
            try
            {
                await WebSockets.ReceiveAsync(socket, HandleMessageAsync, lifetime.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // planned reconnect before the 2 h limit
            }
            finally
            {
                await lifetime.CancelAsync();
                SetAccountValue(AutomowerDriverFactory.SocketConnectedKey, false);
                await WebSockets.CloseQuietlyAsync(socket);
                try
                {
                    await ping;
                }
                catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException)
                {
                    // socket gone
                }
            }
        }

        private async Task PingAsync(ClientWebSocket socket, CancellationToken token)
        {
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                await Task.Delay(PingInterval, token);
                await socket.SendAsync(Encoding.UTF8.GetBytes("ping"), WebSocketMessageType.Text, true, token);
            }
        }

        internal Task HandleMessageAsync(string text)
        {
            try
            {
                if (JsonNode.Parse(text) is not JsonObject message)
                {
                    return Task.CompletedTask;
                }
                var id = message["id"]?.GetValue<string>();
                var type = message["type"]?.GetValue<string>();
                if (id == null || type == null || message["attributes"] is not JsonObject attributes)
                {
                    return Task.CompletedTask; // {ready, connectionId} or an empty answer to a ping
                }
                foreach (var mower in _mowers.Where(m => m.MowerId == id))
                {
                    mower.Merge(type, attributes);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                DriverContext.Logger.LogDebug($"Unreadable Automower message: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _mowers.Select(m => m.MowerId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var mower in await Api.GetMowersAsync(token))
            {
                if (known.Contains(mower.Id))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(AutomowerDriverFactory.MowerTemplate);
                node.Name = mower.Attributes["system"]?["name"]?.GetValue<string>() ?? mower.Id;
                node.SetProperty(AutomowerDriverFactory.MowerIdKey, mower.Id);
                foreach (var dp in AutomowerDataPoints.All)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            switch (key)
            {
                case "mower":
                {
                    var mower = new AutomowerMowerNode(ctx, this);
                    _mowers.Add(mower);
                    return mower;
                }
                case AutomowerDriverFactory.StatusKey:
                case AutomowerDriverFactory.SocketConnectedKey:
                {
                    var node = new ValueNode(ctx, key, null, null);
                    _accountValues.Add(node);
                    return node;
                }
                default:
                    return null;
            }
        }
    }

    internal sealed class AutomowerMowerNode : DriverNoneAttributeBase
    {
        private readonly AutomowerDriver _account;
        private readonly List<(ValueNode Node, AutomowerDataPoint DataPoint)> _values = new();
        private readonly object _lock = new();
        private JsonObject _attributes = new();

        public AutomowerMowerNode(IDriverContext driverContext, AutomowerDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string MowerId { get; private set; }
        internal int DefaultMinutes { get; private set; }
        internal IEnumerable<ValueNode> Values => _values.Select(v => v.Node);

        public override Task<bool> Init(CancellationToken token = default)
        {
            MowerId = GetPropertyValueString(AutomowerDriverFactory.MowerIdKey)?.Trim();
            DefaultMinutes = GetPropertyValueInt(AutomowerDriverFactory.DefaultMinutesKey);
            if (DefaultMinutes <= 0)
            {
                DefaultMinutes = 60;
            }
            if (string.IsNullOrWhiteSpace(MowerId))
            {
                DriverContext.Logger.LogError($"{FullName}: no mower id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        internal void Replace(JsonObject attributes)
        {
            lock (_lock)
            {
                _attributes = attributes;
            }
            Update();
        }

        internal void Merge(string type, JsonObject eventAttributes)
        {
            lock (_lock)
            {
                if (!AutomowerDataPoints.Merge(_attributes, type, eventAttributes))
                {
                    return;
                }
            }
            Update();
        }

        private void Update()
        {
            foreach (var (node, dp) in _values)
            {
                if (dp.Read == null)
                {
                    continue;
                }
                object value;
                lock (_lock)
                {
                    value = dp.Read(_attributes);
                }
                if (value != null)
                {
                    node.Set(value);
                }
            }
        }

        private async Task WriteAsync(AutomowerDataPoint dataPoint, object value, CancellationToken token)
        {
            var write = dataPoint.Write(value, DefaultMinutes);
            if (write == null || string.IsNullOrWhiteSpace(MowerId))
            {
                return;
            }
            if (write.Settings != null)
            {
                DriverContext.Logger.LogInformation($"{MowerId}: settings {JsonSerializer.Serialize(write.Settings)}");
                await _account.Api.SettingsAsync(MowerId, write.Settings, token);
            }
            else
            {
                DriverContext.Logger.LogInformation($"{MowerId}: {write.Action} {write.Duration}");
                await _account.Api.ActionAsync(MowerId, write.Action, write.Duration, token);
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _account.PollAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = AutomowerDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new ValueNode(ctx, key, dataPoint.IsWriteable ? (v, t) => WriteAsync(dataPoint, v, t) : null, null);
            _values.Add((node, dataPoint));
            return node;
        }
    }
}
