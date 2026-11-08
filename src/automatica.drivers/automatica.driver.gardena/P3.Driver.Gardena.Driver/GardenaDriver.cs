using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Gardena.Driver
{
    /// <summary>
    /// A GARDENA smart system account (one location). Reads the location once, then lives on the web socket: the API allows
    /// only 700 requests per week, so it is never polled.
    /// </summary>
    internal sealed class GardenaDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<GardenaService> _services = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly Dictionary<string, JsonObject> _state = new();
        private readonly Dictionary<string, string> _deviceOfService = new();
        private readonly object _stateLock = new();

        private string _configuredLocation;
        private string _locationId;
        private bool _snapshotLoaded;
        private CancellationTokenSource _cts;
        private Task _loop;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal string AuthUrl { get; set; }

        /// <summary>
        /// GARDENA closes idle sockets after about 10 minutes, protocol pings keep it open
        /// </summary>
        internal TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(150);

        /// <summary>
        /// Sockets live at most 2 hours, reconnect a bit earlier
        /// </summary>
        internal TimeSpan SocketLifetime { get; set; } = TimeSpan.FromMinutes(110);
        internal TimeSpan MinReconnectDelay { get; set; } = TimeSpan.FromMinutes(1);
        internal TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(30);

        internal GardenaApi Api { get; private set; }
        internal IReadOnlyList<GardenaService> Services => _services;
        internal string Status { get; private set; }

        public GardenaDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var authUrl = AuthUrl ?? GetPropertyValueString(GardenaDriverFactory.AuthUrlKey);
            var apiUrl = GetPropertyValueString(GardenaDriverFactory.ApiUrlKey);
            _configuredLocation = GetPropertyValueString(GardenaDriverFactory.LocationIdKey)?.Trim();

            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(authUrl) ? GardenaDriverFactory.DefaultAuthUrl : authUrl.Trim(),
                GetPropertyValueString(GardenaDriverFactory.ApplicationKeyKey)?.Trim(), GetPropertyValueString(GardenaDriverFactory.ApplicationSecretKey)?.Trim(), null)
            {
                ClientCredentials = true
            };
            Api = new GardenaApi(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                string.IsNullOrWhiteSpace(apiUrl) ? GardenaDriverFactory.DefaultApiUrl : apiUrl.Trim(), tokens);
            if (!tokens.HasCredentials)
            {
                DriverContext.Logger.LogError("No GARDENA application key / secret configured");
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
            SetAccountValue(GardenaDriverFactory.StatusKey, status);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_cts.Token));
            return base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
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
                    await LoadSnapshotAsync(token);
                    var opened = DateTimeOffset.UtcNow;
                    await ConnectWebSocketAsync(token);
                    // a socket that lived a while is a normal close (idle / 2 h limit / new device): reconnect right away
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
                    DriverContext.Logger.LogError($"GARDENA: {ex.Message}, retrying in {delay.TotalSeconds:0}s");
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

        /// <summary>
        /// Reads the location once per start (the web socket sends the full state on every connect anyway)
        /// </summary>
        internal async Task LoadSnapshotAsync(CancellationToken token = default)
        {
            _locationId ??= await Api.GetLocationIdAsync(_configuredLocation, token);
            if (_snapshotLoaded)
            {
                return;
            }
            var location = await Api.GetLocationAsync(_locationId, token);
            if (location?["included"] is JsonArray included)
            {
                foreach (var item in included)
                {
                    Apply(item as JsonObject);
                }
            }
            _snapshotLoaded = true;
        }

        internal async Task ConnectWebSocketAsync(CancellationToken token = default)
        {
            var url = await Api.CreateWebSocketAsync(_locationId, token);
            using var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = KeepAlive;
            await socket.ConnectAsync(new Uri(url), token);
            SetAccountValue(GardenaDriverFactory.SocketConnectedKey, true);
            SetStatus("OK");
            DriverContext.Logger.LogInformation("GARDENA web socket connected");

            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            lifetime.CancelAfter(SocketLifetime);
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
                SetAccountValue(GardenaDriverFactory.SocketConnectedKey, false);
                await WebSockets.CloseQuietlyAsync(socket);
            }
        }

        internal Task HandleMessageAsync(string text)
        {
            try
            {
                Apply(JsonNode.Parse(text) as JsonObject);
            }
            catch (JsonException)
            {
                DriverContext.Logger.LogDebug("Unreadable GARDENA message");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Merges a service document (JSON:API patch: present attributes overwrite, null removes) and updates the nodes
        /// </summary>
        internal void Apply(JsonObject document)
        {
            var id = document?["id"]?.GetValue<string>();
            var type = document?["type"]?.GetValue<string>();
            if (id == null || type is null or "LOCATION" or "DEVICE")
            {
                return;
            }

            lock (_stateLock)
            {
                if (document["relationships"]?["device"]?["data"]?["id"]?.GetValue<string>() is { } deviceId)
                {
                    _deviceOfService[id] = deviceId;
                }
                if (!_state.TryGetValue(StateKey(type, id), out var attributes))
                {
                    _state[StateKey(type, id)] = attributes = new JsonObject();
                }
                if (document["attributes"] is JsonObject patch)
                {
                    foreach (var (name, value) in patch.ToList())
                    {
                        if (value == null)
                        {
                            attributes.Remove(name);
                        }
                        else
                        {
                            attributes[name] = value.DeepClone();
                        }
                    }
                }
            }

            foreach (var service in _services.Where(s => type == "COMMON" ? DeviceOf(s.ServiceId) == id : s.ServiceId == id && s.Kind.ServiceType == type))
            {
                service.Update();
            }
        }

        internal string DeviceOf(string serviceId)
        {
            if (serviceId == null)
            {
                return null;
            }
            lock (_stateLock)
            {
                if (_deviceOfService.TryGetValue(serviceId, out var device))
                {
                    return device;
                }
            }
            var colon = serviceId.IndexOf(':');
            return colon > 0 ? serviceId[..colon] : serviceId;
        }

        /// <summary>
        /// The "value" of an attribute of a service (or the COMMON service of its device)
        /// </summary>
        private static string StateKey(string type, string id) => type + "|" + id;

        internal object GetValue(string serviceType, string serviceId, string attribute, bool common)
        {
            var id = common ? DeviceOf(serviceId) : serviceId;
            lock (_stateLock)
            {
                if (id == null || !_state.TryGetValue(StateKey(common ? "COMMON" : serviceType, id), out var attributes) || attributes[attribute] is not JsonObject wrapper ||
                    wrapper["value"] is not JsonValue value)
                {
                    return null;
                }
                return Conv.Scalar(value.Deserialize<JsonElement>());
            }
        }

        /// <summary>
        /// Lists the valves, mowers, power sockets and sensors of the location that are not configured yet
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            _locationId ??= await Api.GetLocationIdAsync(_configuredLocation, token);
            var location = await Api.GetLocationAsync(_locationId, token);
            var included = (location?["included"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();
            var known = _services.Select(s => s.ServiceId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var service in included)
            {
                var id = service["id"]?.GetValue<string>();
                var kind = GardenaDataPoints.Kinds.FirstOrDefault(k => k.ServiceType == service["type"]?.GetValue<string>());
                if (id == null || kind == null || known.Contains(id))
                {
                    continue;
                }
                var deviceId = service["relationships"]?["device"]?["data"]?["id"]?.GetValue<string>() ?? DeviceOf(id);
                var common = included.FirstOrDefault(i => i["type"]?.GetValue<string>() == "COMMON" && i["id"]?.GetValue<string>() == deviceId);
                var name = service["attributes"]?["name"]?["value"]?.GetValue<string>() ?? common?["attributes"]?["name"]?["value"]?.GetValue<string>() ?? id;

                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(kind.Template);
                node.Name = name;
                node.SetProperty(GardenaDriverFactory.ServiceIdKey, id);
                foreach (var dp in GardenaDataPoints.Of(kind.Key))
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
            if (key is GardenaDriverFactory.StatusKey or GardenaDriverFactory.SocketConnectedKey)
            {
                var node = new ValueNode(ctx, key, null, null);
                _accountValues.Add(node);
                return node;
            }
            var kind = GardenaDataPoints.Kind(key);
            if (kind == null)
            {
                return null;
            }
            var service = new GardenaService(ctx, this, kind);
            _services.Add(service);
            return service;
        }
    }

    /// <summary>
    /// A valve, mower, power socket or sensor service
    /// </summary>
    internal sealed class GardenaService : DriverNoneAttributeBase
    {
        private readonly GardenaDriver _account;
        private readonly List<(ValueNode Node, GardenaDataPoint DataPoint)> _values = new();

        public GardenaService(IDriverContext driverContext, GardenaDriver account, GardenaKind kind) : base(driverContext)
        {
            _account = account;
            Kind = kind;
        }

        internal GardenaKind Kind { get; }
        internal string ServiceId { get; private set; }
        internal int DefaultMinutes { get; private set; }
        internal IEnumerable<ValueNode> Values => _values.Select(v => v.Node);

        public override Task<bool> Init(CancellationToken token = default)
        {
            ServiceId = GetPropertyValueString(GardenaDriverFactory.ServiceIdKey)?.Trim();
            DefaultMinutes = Kind.DefaultMinutes > 0 ? GetPropertyValueInt(GardenaDriverFactory.DefaultMinutesKey) : 0;
            if (DefaultMinutes <= 0)
            {
                DefaultMinutes = Kind.DefaultMinutes;
            }
            if (string.IsNullOrWhiteSpace(ServiceId))
            {
                DriverContext.Logger.LogError($"{FullName}: no service id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        internal void Update()
        {
            foreach (var (node, dp) in _values)
            {
                if (dp.Attribute == null)
                {
                    continue;
                }
                var value = _account.GetValue(Kind.ServiceType, ServiceId, dp.Attribute, dp.Common);
                if (dp.Map != null)
                {
                    value = dp.Map(value);
                }
                if (value is int i && dp.Type == NodeDataType.Double)
                {
                    value = (double)i;
                }
                if (value != null)
                {
                    node.Set(value);
                }
            }
        }

        private async Task WriteAsync(GardenaDataPoint dataPoint, object value, CancellationToken token)
        {
            var command = dataPoint.Write(value, DefaultMinutes);
            if (command == null || string.IsNullOrWhiteSpace(ServiceId))
            {
                return; // momentary button released
            }
            DriverContext.Logger.LogInformation($"{ServiceId}: {command.ControlType} {command.Command} {command.Seconds}");
            await _account.Api.CommandAsync(ServiceId, command, token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = GardenaDataPoints.Of(Kind.Key).FirstOrDefault(d => d.Key == key);
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
