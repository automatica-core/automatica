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

namespace P3.Driver.HomeConnect.Driver
{
    /// <summary>
    /// A Home Connect account: logs in (device flow or refresh token), keeps the event stream of all appliances open and
    /// reads everything again at the polling interval.
    /// </summary>
    internal sealed class HomeConnectDriver : DriverNoneAttributeBase
    {
        public const string Scopes = "IdentifyAppliance Monitor Control Settings";

        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly HttpClient SharedStreamHttp = new() { Timeout = Timeout.InfiniteTimeSpan };

        private readonly List<HomeConnectAppliance> _appliances = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private string _configuredToken;
        private string _baseUrl;
        private string _clientId;
        private string _clientSecret;
        private TimeSpan _poll;
        private Timer _timer;
        private CancellationTokenSource _cts;
        private Task _loop;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal HttpClient StreamHttp { get; set; } = SharedStreamHttp;

        /// <summary>
        /// Home Connect sends a KEEP-ALIVE every 55 s; without any data for this long the stream is reopened
        /// </summary>
        internal TimeSpan StreamIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);
        internal TimeSpan MinReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);
        internal TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Overrides the polling interval of the device login (tests)
        /// </summary>
        internal TimeSpan? DeviceLoginInterval { get; set; }

        internal HomeConnectApi Api { get; private set; }
        internal IReadOnlyList<HomeConnectAppliance> Appliances => _appliances;
        internal string LoginStatus { get; private set; }

        public HomeConnectDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(600, GetPropertyValueInt(HomeConnectDriverFactory.PollKey)));
            _baseUrl = GetPropertyValueString(HomeConnectDriverFactory.ApiUrlKey);
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                _baseUrl = HomeConnectDriverFactory.DefaultApiUrl;
            }
            _baseUrl = _baseUrl.Trim().TrimEnd('/');
            _clientId = GetPropertyValueString(HomeConnectDriverFactory.ClientIdKey)?.Trim();
            _clientSecret = GetPropertyValueString(HomeConnectDriverFactory.ClientSecretKey)?.Trim();
            _configuredToken = GetPropertyValueString(HomeConnectDriverFactory.RefreshTokenKey)?.Trim() ?? "";

            var refreshToken = TokenState.Resolve(GetPropertyValueString(HomeConnectDriverFactory.TokenStateKey), _configuredToken);
            Api = CreateApi(refreshToken);

            if (string.IsNullOrWhiteSpace(_clientId))
            {
                DriverContext.Logger.LogError("No Home Connect client id configured");
            }
            return base.Init(token);
        }

        private HomeConnectApi CreateApi(string refreshToken)
        {
            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(), _baseUrl + "/security/oauth/token",
                _clientId, _clientSecret, refreshToken);
            tokens.RefreshTokenRotated += rotated => SaveTokenState(TokenState.Serialize(_configuredToken, rotated));
            return new HomeConnectApi(Http, StreamHttp, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(), _baseUrl, tokens);
        }

        private void SaveTokenState(string json)
        {
            var property = GetProperty(HomeConnectDriverFactory.TokenStateKey);
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
            SetAccountValue(HomeConnectDriverFactory.LoginStatusKey, status);
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
                DriverContext.Logger.LogError($"Home Connect poll failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Login → full read → event stream, reconnecting with a growing delay
        /// </summary>
        private async Task RunAsync(CancellationToken token)
        {
            var delay = MinReconnectDelay;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Api.Tokens.HasCredentials)
                    {
                        await DeviceLoginAsync(token);
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
                catch (OAuthException ex) when (ex.IsInvalidGrant && !string.IsNullOrWhiteSpace(_clientId))
                {
                    DriverContext.Logger.LogError($"Home Connect login is no longer valid ({ex.Message}), starting a new login");
                    SetLoginStatus($"Login expired: {ex.Message}");
                    Api = CreateApi(null);
                    continue;
                }
                catch (RateLimitedException ex)
                {
                    SetLoginStatus(ex.Message);
                    await DelayAsync(ex.Until - DriverContext.TimeProvider.GetUtcNow(), token);
                    continue;
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError($"Home Connect: {ex.Message}, retrying in {delay.TotalSeconds:0}s");
                    if (ex is OAuthException)
                    {
                        SetLoginStatus($"Login failed: {ex.Message}");
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

        /// <summary>
        /// Device flow: shows the verification URL and code as login status until the user confirms
        /// </summary>
        internal async Task DeviceLoginAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                throw new OAuthException("No client id configured", "invalid_client");
            }
            var authorization = await Api.Tokens.StartDeviceAuthorizationAsync(_baseUrl + "/security/oauth/device_authorization",
                new Dictionary<string, string> { ["scope"] = Scopes }, token);
            var url = authorization.VerificationUriComplete ?? authorization.VerificationUri;
            SetLoginStatus($"Login required: open {url} and confirm the code {authorization.UserCode}");
            DriverContext.Logger.LogWarning($"Home Connect login required: open {url} and confirm the code {authorization.UserCode}");

            var interval = DeviceLoginInterval ?? authorization.Interval;
            var deadline = DriverContext.TimeProvider.GetUtcNow() + authorization.ExpiresIn;
            while (DriverContext.TimeProvider.GetUtcNow() < deadline)
            {
                await Task.Delay(interval, token);
                if (await Api.Tokens.TryCompleteDeviceAuthorizationAsync(authorization.DeviceCode, null, token))
                {
                    DriverContext.Logger.LogInformation("Home Connect login completed");
                    SetLoginStatus("OK");
                    return;
                }
            }
            throw new OAuthException("The login code expired before it was confirmed", "expired_token");
        }

        /// <summary>
        /// Reads the appliance list and every configured, connected appliance
        /// </summary>
        internal async Task RefreshAllAsync(CancellationToken token = default)
        {
            await _refreshLock.WaitAsync(token);
            try
            {
                var appliances = await Api.GetAppliancesAsync(token);
                foreach (var appliance in _appliances)
                {
                    var info = appliances.FirstOrDefault(a => a.HaId == appliance.HaId);
                    appliance.SetConnected(info?.Connected ?? false);
                }
                foreach (var appliance in _appliances)
                {
                    try
                    {
                        await appliance.RefreshAsync(token);
                    }
                    catch (HomeConnectApiException ex)
                    {
                        DriverContext.Logger.LogError($"Could not read {appliance.HaId}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        internal async Task ReadEventStreamAsync(CancellationToken token = default)
        {
            using var response = await Api.OpenEventStreamAsync(token);
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            SetAccountValue(HomeConnectDriverFactory.StreamConnectedKey, true);
            try
            {
                await Sse.ReadAsync(stream, e => HandleEventAsync(e, token), StreamIdleTimeout, token);
            }
            finally
            {
                SetAccountValue(HomeConnectDriverFactory.StreamConnectedKey, false);
            }
        }

        internal async Task HandleEventAsync(SseEvent e, CancellationToken token = default)
        {
            if (e.Event == "KEEP-ALIVE")
            {
                return;
            }

            JsonElement root = default;
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                try
                {
                    root = JsonDocument.Parse(e.Data).RootElement;
                }
                catch (JsonException)
                {
                    DriverContext.Logger.LogDebug($"Unreadable Home Connect event {e.Event}");
                    return;
                }
            }
            var haId = e.Id ?? (root.ValueKind == JsonValueKind.Object ? Conv.Str(root, "haId") : null);

            switch (e.Event)
            {
                case "CONNECTED":
                case "DISCONNECTED":
                    foreach (var appliance in _appliances.Where(a => a.HaId == haId))
                    {
                        appliance.SetConnected(e.Event == "CONNECTED");
                        if (e.Event == "CONNECTED")
                        {
                            try
                            {
                                await appliance.RefreshThrottledAsync(token);
                            }
                            catch (Exception ex) when (ex is HomeConnectApiException or RateLimitedException or HttpRequestException)
                            {
                                DriverContext.Logger.LogError($"Could not read {haId}: {ex.Message}");
                            }
                        }
                    }
                    break;
                case "STATUS":
                case "NOTIFY":
                case "EVENT":
                    if (Conv.Path(root, "items") is not { ValueKind: JsonValueKind.Array } items)
                    {
                        return;
                    }
                    foreach (var item in items.EnumerateArray())
                    {
                        var key = Conv.Str(item, "key");
                        var itemHaId = Conv.Str(item, "haId") ?? haId;
                        if (key == null)
                        {
                            continue;
                        }
                        var value = item.TryGetProperty("value", out var v) ? v : default;
                        foreach (var appliance in _appliances.Where(a => a.HaId == itemHaId))
                        {
                            appliance.Apply(key, value, e.Event == "EVENT");
                        }
                    }
                    break;
                case "PAIRED":
                case "DEPAIRED":
                    DriverContext.Logger.LogInformation($"Home Connect appliance {haId} {e.Event.ToLowerInvariant()}");
                    break;
            }
        }

        /// <summary>
        /// Lists the appliances of the account that are not configured yet
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _appliances.Select(a => a.HaId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var appliance in await Api.GetAppliancesAsync(token))
            {
                if (known.Contains(appliance.HaId))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(HomeConnectDriverFactory.ApplianceTemplate);
                node.Name = string.Join(" ", new[] { appliance.Brand, appliance.Name ?? appliance.Type }.Where(s => !string.IsNullOrWhiteSpace(s)));
                node.SetProperty(HomeConnectDriverFactory.HaIdKey, appliance.HaId);
                foreach (var dp in HomeConnectDataPoints.Defaults)
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
                case "appliance":
                {
                    var appliance = new HomeConnectAppliance(ctx, this);
                    _appliances.Add(appliance);
                    return appliance;
                }
                case HomeConnectDriverFactory.LoginStatusKey:
                case HomeConnectDriverFactory.StreamConnectedKey:
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
}
