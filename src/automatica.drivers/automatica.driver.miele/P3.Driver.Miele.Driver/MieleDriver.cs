using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Miele.Driver
{
    /// <summary>
    /// A Miele account: keeps the event stream of all appliances open (it sends the full state on every change) and reads
    /// all appliances again at the polling interval.
    /// </summary>
    internal sealed class MieleDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly HttpClient SharedStreamHttp = new() { Timeout = Timeout.InfiniteTimeSpan };

        private readonly List<MieleAppliance> _appliances = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private string _configuredToken;
        private TimeSpan _poll;
        private Timer _timer;
        private CancellationTokenSource _cts;
        private Task _loop;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal HttpClient StreamHttp { get; set; } = SharedStreamHttp;

        /// <summary>
        /// Miele sends a ping after 20 s without other events
        /// </summary>
        internal TimeSpan StreamIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);
        internal TimeSpan MinReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);
        internal TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(15);

        internal MieleApi Api { get; private set; }
        internal IReadOnlyList<MieleAppliance> Appliances => _appliances;
        internal string LoginStatus { get; private set; }

        public MieleDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(300, GetPropertyValueInt(MieleDriverFactory.PollKey)));
            var apiUrl = GetPropertyValueString(MieleDriverFactory.ApiUrlKey);
            var tokenUrl = GetPropertyValueString(MieleDriverFactory.TokenUrlKey);
            _configuredToken = GetPropertyValueString(MieleDriverFactory.RefreshTokenKey)?.Trim() ?? "";
            var refreshToken = TokenState.Resolve(GetPropertyValueString(MieleDriverFactory.TokenStateKey), _configuredToken);

            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(tokenUrl) ? MieleDriverFactory.DefaultTokenUrl : tokenUrl.Trim(),
                GetPropertyValueString(MieleDriverFactory.ClientIdKey)?.Trim(), GetPropertyValueString(MieleDriverFactory.ClientSecretKey)?.Trim(), refreshToken);
            tokens.RefreshTokenRotated += rotated => SaveTokenState(TokenState.Serialize(_configuredToken, rotated));

            Api = new MieleApi(Http, StreamHttp, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(apiUrl) ? MieleDriverFactory.DefaultApiUrl : apiUrl.Trim(), GetPropertyValueString(MieleDriverFactory.LanguageKey), tokens);

            if (!tokens.HasCredentials)
            {
                DriverContext.Logger.LogError("No Miele refresh token configured");
            }
            return base.Init(token);
        }

        private void SaveTokenState(string json)
        {
            var property = GetProperty(MieleDriverFactory.TokenStateKey);
            if (property == null)
            {
                return;
            }
            property.Value = json;
            DriverContext.NodeTemplateFactory.SetPropertyValue(property.ObjId, json);
        }

        private void SetAccountValue(string key, object value)
        {
            foreach (var node in _accountValues.Where(v => v.Key == key))
            {
                node.Set(value);
            }
        }

        private void SetLoginStatus(string status)
        {
            LoginStatus = status;
            SetAccountValue(MieleDriverFactory.LoginStatusKey, status);
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
                    await RefreshAllAsync();
                }
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError($"Miele poll failed: {ex.Message}");
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
                        SetLoginStatus("No refresh token configured");
                        return;
                    }
                    await RefreshAllAsync(token);
                    SetLoginStatus("OK");
                    var opened = DateTimeOffset.UtcNow;
                    await ReadEventStreamAsync(token);
                    if (DateTimeOffset.UtcNow - opened > TimeSpan.FromMinutes(5))
                    {
                        delay = MinReconnectDelay;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (RateLimitedException ex)
                {
                    SetLoginStatus(ex.Message);
                    await DelayAsync(ex.Until - DriverContext.TimeProvider.GetUtcNow(), token);
                    continue;
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError($"Miele: {ex.Message}, retrying in {delay.TotalSeconds:0}s");
                    if (ex is OAuthException oauth)
                    {
                        SetLoginStatus(oauth.IsInvalidGrant ? $"Login expired, enter a new refresh token: {ex.Message}" : $"Login failed: {ex.Message}");
                        if (oauth.IsInvalidGrant)
                        {
                            delay = MaxReconnectDelay; // never hammer the token endpoint, Miele deactivates such clients
                        }
                    }
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

        internal async Task RefreshAllAsync(CancellationToken token = default)
        {
            await _refreshLock.WaitAsync(token);
            try
            {
                Apply(await Api.GetDevicesAsync(token), true);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private void Apply(IReadOnlyList<MieleDevice> devices, bool complete)
        {
            foreach (var appliance in _appliances)
            {
                var device = devices.FirstOrDefault(d => d.Id == appliance.DeviceId);
                if (device != null)
                {
                    appliance.Apply(device.State);
                }
                else if (complete)
                {
                    appliance.SetMissing();
                }
            }
        }

        internal async Task ReadEventStreamAsync(CancellationToken token = default)
        {
            using var response = await Api.OpenEventStreamAsync(token);
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            SetAccountValue(MieleDriverFactory.StreamConnectedKey, true);
            try
            {
                await Sse.ReadAsync(stream, HandleEventAsync, StreamIdleTimeout, token);
            }
            finally
            {
                SetAccountValue(MieleDriverFactory.StreamConnectedKey, false);
            }
        }

        internal Task HandleEventAsync(SseEvent e)
        {
            // the documentation names the events IDENT / ACTION / PING, the stream sends devices / actions / ping
            if ((e.Event.Equals("devices", StringComparison.OrdinalIgnoreCase) || e.Event.Equals("ident", StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(e.Data))
            {
                try
                {
                    using var json = JsonDocument.Parse(e.Data);
                    Apply(MieleApi.ParseDevices(json.RootElement), false);
                }
                catch (JsonException)
                {
                    DriverContext.Logger.LogDebug("Unreadable Miele devices event");
                }
            }
            return Task.CompletedTask;
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _appliances.Select(a => a.DeviceId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var device in await Api.GetDevicesAsync(token))
            {
                if (known.Contains(device.Id))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(MieleDriverFactory.ApplianceTemplate);
                node.Name = device.Name ?? (device.Type != null ? $"{device.Type} {device.Id}" : device.Id);
                node.SetProperty(MieleDriverFactory.DeviceIdKey, device.Id);
                foreach (var dp in MieleDataPoints.All)
                {
                    // only what the appliance reports
                    if (device.State.ValueKind != JsonValueKind.Object || dp.Read(device.State) != null || dp.Key is "running" or "power")
                    {
                        node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                    }
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
                case "appliance":
                {
                    var appliance = new MieleAppliance(ctx, this);
                    _appliances.Add(appliance);
                    return appliance;
                }
                case MieleDriverFactory.LoginStatusKey:
                case MieleDriverFactory.StreamConnectedKey:
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

    internal sealed class MieleAppliance : DriverNoneAttributeBase
    {
        private readonly MieleDriver _account;
        private readonly List<ValueNode> _values = new();

        public MieleAppliance(IDriverContext driverContext, MieleDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string DeviceId { get; private set; }

        internal IReadOnlyList<ValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            DeviceId = GetPropertyValueString(MieleDriverFactory.DeviceIdKey)?.Trim();
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                DriverContext.Logger.LogError($"{FullName}: no device id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        internal void Apply(JsonElement state)
        {
            if (state.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            foreach (var node in _values)
            {
                var dataPoint = MieleDataPoints.All.First(d => d.Key == node.Key);
                var value = dataPoint.Read(state);
                if (value != null)
                {
                    node.Set(value);
                }
            }
        }

        /// <summary>
        /// The appliance is no longer shared with the application
        /// </summary>
        internal void SetMissing()
        {
            foreach (var node in _values.Where(v => v.Key == "connected"))
            {
                node.Set(false);
            }
        }

        private async Task WriteAsync(MieleDataPoint dataPoint, object value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                return;
            }
            var write = dataPoint.Write(value);
            DriverContext.Logger.LogInformation($"{DeviceId}: {write.Endpoint} {JsonSerializer.Serialize(write.Body)}");
            await _account.Api.PutAsync(DeviceId, write.Endpoint, write.Body, token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _account.RefreshAllAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = MieleDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new ValueNode(ctx, key, dataPoint.IsWriteable ? (v, t) => WriteAsync(dataPoint, v, t) : null, t => Read(t));
            _values.Add(node);
            return node;
        }
    }
}
