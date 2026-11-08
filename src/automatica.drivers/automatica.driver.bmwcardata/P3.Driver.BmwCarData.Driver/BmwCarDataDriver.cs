using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.BmwCarData.Driver
{
    /// <summary>
    /// What survives a restart: the rotated refresh token, the gcid (MQTT user name) and the container the driver created
    /// </summary>
    internal sealed class BmwState
    {
        public string Source { get; set; }
        public string Refresh { get; set; }
        public string Gcid { get; set; }
        public string ContainerId { get; set; }
        public string ContainerHash { get; set; }

        public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? "")));

        public static BmwState Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<BmwState>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A BMW (or MINI) CarData client: device-code login, a container with the telematic keys of all vehicles,
    /// REST polling within 50 requests a day and optionally the MQTT stream
    /// </summary>
    internal sealed class BmwCarDataDriver : DriverNoneAttributeBase
    {
        public const string Scopes = "authenticate_user openid cardata:api:read cardata:streaming:read";
        public const string ContainerName = "Automatica";

        /// <summary>
        /// Requests a day the polling may use, the rest is left for login, container and scan calls
        /// </summary>
        public const int PollBudget = 40;

        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<BmwVehicleNode> _vehicles = new();
        private readonly List<ValueNode> _accountValues = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private string _clientId;
        private string _authUrl;
        private string _configuredContainer;
        private bool _streaming;
        private string _mqttHost;
        private BmwState _state = new();
        private int _configuredPoll;
        private TimeSpan _poll;
        private Timer _timer;
        private CancellationTokenSource _cts;
        private Task _loop;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal Func<IBmwStream> StreamFactory { get; set; } = () => new MqttNetStream();
        internal TimeSpan? DeviceLoginInterval { get; set; }

        /// <summary>
        /// The id token (MQTT password) lives 1 h: reconnect with a fresh one before BMW closes the connection
        /// </summary>
        internal TimeSpan StreamTokenLifetime { get; set; } = TimeSpan.FromMinutes(50);
        internal TimeSpan MinReconnectDelay { get; set; } = TimeSpan.FromMinutes(1);
        internal TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromMinutes(30);

        internal BmwCarDataApi Api { get; private set; }
        internal IReadOnlyList<BmwVehicleNode> Vehicles => _vehicles;
        internal string Status { get; private set; }

        public BmwCarDataDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        private DateTimeOffset Now() => DriverContext.TimeProvider.GetUtcNow();

        public override Task<bool> Init(CancellationToken token = default)
        {
            _clientId = GetPropertyValueString(BmwCarDataDriverFactory.ClientIdKey)?.Trim() ?? "";
            _authUrl = (GetPropertyValueString(BmwCarDataDriverFactory.AuthUrlKey) is { Length: > 0 } auth ? auth.Trim() : BmwCarDataDriverFactory.DefaultAuthUrl).TrimEnd('/');
            _configuredContainer = GetPropertyValueString(BmwCarDataDriverFactory.ContainerIdKey)?.Trim();
            _streaming = GetPropertyValueBool(BmwCarDataDriverFactory.StreamingKey) == true;
            _mqttHost = GetPropertyValueString(BmwCarDataDriverFactory.MqttHostKey) is { Length: > 0 } host ? host.Trim() : BmwCarDataDriverFactory.DefaultMqttHost;

            var source = BmwState.Hash(_clientId);
            var stored = BmwState.Parse(GetPropertyValueString(BmwCarDataDriverFactory.StateKey));
            _state = stored != null && stored.Source == source ? stored : new BmwState { Source = source };

            var apiUrl = GetPropertyValueString(BmwCarDataDriverFactory.ApiUrlKey);
            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, Now, _authUrl + "/token", _clientId, null, _state.Refresh);
            tokens.TokenReceived += () => OnTokenReceived(tokens);
            Api = new BmwCarDataApi(Http, DriverContext.Logger, Now, string.IsNullOrWhiteSpace(apiUrl) ? BmwCarDataDriverFactory.DefaultApiUrl : apiUrl.Trim(), tokens);

            _configuredPoll = Math.Max(900, GetPropertyValueInt(BmwCarDataDriverFactory.PollKey));
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                DriverContext.Logger.LogError("No BMW CarData client id configured");
            }
            return base.Init(token);
        }

        private void OnTokenReceived(OAuthTokenClient tokens)
        {
            if (tokens.LastResponse.ValueKind == JsonValueKind.Object && tokens.LastResponse.TryGetProperty("gcid", out var gcid) && gcid.ValueKind == JsonValueKind.String)
            {
                _state.Gcid = gcid.GetString();
            }
            _state.Refresh = tokens.RefreshToken;
            SaveState();
        }

        private void SaveState()
        {
            var json = JsonSerializer.Serialize(_state);
            var property = GetProperty(BmwCarDataDriverFactory.StateKey);
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

        private void SetStatus(string status)
        {
            Status = status;
            SetAccountValue(BmwCarDataDriverFactory.LoginStatusKey, status);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            _poll = CalculatePollInterval();
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

        /// <summary>
        /// Stays within 50 requests a day: one telematic request per vehicle and interval
        /// </summary>
        internal TimeSpan CalculatePollInterval()
        {
            var minimum = (int)Math.Ceiling(86400d * Math.Max(1, _vehicles.Count(v => !string.IsNullOrWhiteSpace(v.Vin))) / PollBudget);
            var poll = TimeSpan.FromSeconds(Math.Max(_configuredPoll, minimum));
            if (_configuredPoll < minimum)
            {
                DriverContext.Logger.LogWarning($"BMW CarData: polling every {poll.TotalMinutes:0} min instead of {_configuredPoll / 60} min to stay within 50 requests a day");
            }
            return poll;
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
                DriverContext.Logger.LogError($"BMW CarData poll failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Login → one read → stream (if enabled), the timer does the further reads
        /// </summary>
        private async Task RunAsync(CancellationToken token)
        {
            var delay = MinReconnectDelay;
            var initialRead = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Api.Tokens.HasCredentials)
                    {
                        await DeviceLoginAsync(token);
                    }
                    if (!initialRead)
                    {
                        initialRead = true;
                        try
                        {
                            await RefreshAllAsync(token);
                        }
                        catch (Exception ex) when (ex is RateLimitedException or BmwCarDataApiException or HttpRequestException)
                        {
                            DriverContext.Logger.LogError($"BMW CarData: {ex.Message}");
                        }
                    }
                    if (!_streaming)
                    {
                        return;
                    }
                    var opened = DateTimeOffset.UtcNow;
                    await StreamAsync(token);
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
                catch (OAuthException ex) when (ex.IsInvalidGrant && !string.IsNullOrWhiteSpace(_clientId))
                {
                    DriverContext.Logger.LogError($"BMW CarData login is no longer valid ({ex.Message}), starting a new login");
                    SetStatus($"Login expired: {ex.Message}");
                    ResetLogin();
                    continue;
                }
                catch (Exception ex)
                {
                    DriverContext.Logger.LogError($"BMW CarData: {ex.Message}, retrying in {delay.TotalSeconds:0}s");
                    SetStatus(ex is OAuthException ? $"Login failed: {ex.Message}" : ex.Message);
                }

                await DelayAsync(delay, token);
                delay = TimeSpan.FromTicks(Math.Min(MaxReconnectDelay.Ticks, delay.Ticks * 2));
            }
        }

        private void ResetLogin()
        {
            _state.Refresh = null;
            SaveState();
            var tokens = new OAuthTokenClient(Http, DriverContext.Logger, Now, _authUrl + "/token", _clientId, null, null);
            tokens.TokenReceived += () => OnTokenReceived(tokens);
            Api = new BmwCarDataApi(Http, DriverContext.Logger, Now, Api.BaseUrl, tokens);
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

        internal static string CodeChallenge(string verifier) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>
        /// Device code flow with PKCE: shows the link and code as login status until the user confirms
        /// </summary>
        internal async Task DeviceLoginAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                throw new OAuthException("No client id configured (create a CarData client in the My BMW portal)", "invalid_client");
            }
            var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var authorization = await Api.Tokens.StartDeviceAuthorizationAsync(_authUrl + "/device/code", new Dictionary<string, string>
            {
                ["response_type"] = "device_code",
                ["scope"] = Scopes,
                ["code_challenge"] = CodeChallenge(verifier),
                ["code_challenge_method"] = "S256"
            }, token);
            var url = authorization.VerificationUriComplete ??
                      $"{authorization.VerificationUri}{(authorization.VerificationUri?.Contains('?') == true ? "&" : "?")}user_code={Uri.EscapeDataString(authorization.UserCode ?? "")}";
            SetStatus($"Login required: open {url} and confirm the code {authorization.UserCode}");
            DriverContext.Logger.LogWarning($"BMW CarData login required: open {url} and confirm the code {authorization.UserCode}");

            var interval = DeviceLoginInterval ?? authorization.Interval;
            var deadline = Now() + authorization.ExpiresIn;
            var extra = new Dictionary<string, string> { ["code_verifier"] = verifier };
            while (Now() < deadline)
            {
                await Task.Delay(interval, token);
                if (await Api.Tokens.TryCompleteDeviceAuthorizationAsync(authorization.DeviceCode, extra, token))
                {
                    DriverContext.Logger.LogInformation("BMW CarData login completed");
                    SetStatus("OK");
                    return;
                }
            }
            throw new OAuthException("The login code expired before it was confirmed", "expired_token");
        }

        internal IReadOnlyList<string> Descriptors() =>
            BmwCarDataDataPoints.StandardDescriptors
                .Concat(_vehicles.SelectMany(v => v.CustomDescriptors))
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// The configured container, or one the driver created for the current set of keys (max. 10 per account)
        /// </summary>
        internal async Task<string> EnsureContainerAsync(CancellationToken token = default)
        {
            if (!string.IsNullOrWhiteSpace(_configuredContainer))
            {
                return _configuredContainer;
            }
            var descriptors = Descriptors();
            var hash = BmwState.Hash(string.Join("\n", descriptors));
            if (_state.ContainerId != null && _state.ContainerHash == hash)
            {
                return _state.ContainerId;
            }

            if (_state.ContainerId != null)
            {
                try
                {
                    await Api.DeleteContainerAsync(_state.ContainerId, token);
                }
                catch (BmwCarDataApiException ex)
                {
                    DriverContext.Logger.LogWarning($"BMW CarData: deleting the old container failed: {ex.Message}");
                }
                _state.ContainerId = null;
                SaveState();
            }

            string id;
            try
            {
                id = await Api.CreateContainerAsync(ContainerName, "Automatica smart home", descriptors, token);
            }
            catch (BmwCarDataApiException ex) when (ex.ErrorId == "CU-124")
            {
                // container limit: remove leftovers of earlier installations
                foreach (var old in (await Api.GetContainersAsync(token)).Where(c => c.Name == ContainerName && c.State != "DELETED"))
                {
                    await Api.DeleteContainerAsync(old.Id, token);
                }
                id = await Api.CreateContainerAsync(ContainerName, "Automatica smart home", descriptors, token);
            }
            DriverContext.Logger.LogInformation($"BMW CarData container {id} created with {descriptors.Count} keys");
            _state.ContainerId = id;
            _state.ContainerHash = hash;
            SaveState();
            return id;
        }

        /// <summary>
        /// One telematic request per vehicle
        /// </summary>
        internal async Task RefreshAllAsync(CancellationToken token = default)
        {
            await _refreshLock.WaitAsync(token);
            try
            {
                var containerId = await EnsureContainerAsync(token);
                foreach (var vehicle in _vehicles.Where(v => !string.IsNullOrWhiteSpace(v.Vin)))
                {
                    try
                    {
                        vehicle.Apply(await Api.GetTelematicDataAsync(vehicle.Vin, containerId, token));
                    }
                    catch (BmwCarDataApiException ex) when (ex.ErrorId is "CU-105" or "CU-121" && string.IsNullOrWhiteSpace(_configuredContainer))
                    {
                        // the container is gone (deleted in the portal), create a new one next time
                        _state.ContainerId = null;
                        SaveState();
                        throw;
                    }
                }
                SetStatus("OK");
            }
            catch (Exception ex) when (ex is RateLimitedException or BmwCarDataApiException)
            {
                SetStatus(ex.Message);
                throw;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// One MQTT connection (BMW allows one per account) until it closes or the id token is about to expire
        /// </summary>
        internal async Task StreamAsync(CancellationToken token = default)
        {
            await Api.Tokens.GetAccessTokenAsync(true, token); // a fresh id token, it is the password
            var idToken = Api.Tokens.LastResponse.ValueKind == JsonValueKind.Object && Api.Tokens.LastResponse.TryGetProperty("id_token", out var id) ? id.GetString() : null;
            if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(_state.Gcid))
            {
                throw new OAuthException("The login has no id token / gcid for the stream, log in again", "invalid_grant");
            }
            var (host, port, tls) = MqttNetStream.ParseHost(_mqttHost, 9000);
            var vins = _vehicles.Select(v => v.Vin).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
            var topics = vins.Count > 0 ? vins.Select(v => $"{_state.Gcid}/{v}").ToList() : new List<string> { $"{_state.Gcid}/+" };

            await using var stream = StreamFactory();
            await stream.ConnectAsync(new BmwStreamOptions(host, port, tls, "automatica-" + Guid.NewGuid().ToString("N")[..12], _state.Gcid, idToken, topics),
                HandleMessageAsync, token);
            SetAccountValue(BmwCarDataDriverFactory.StreamConnectedKey, true);
            DriverContext.Logger.LogInformation("BMW CarData stream connected");
            try
            {
                await Task.WhenAny(stream.Disconnected, Task.Delay(StreamTokenLifetime, token));
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                SetAccountValue(BmwCarDataDriverFactory.StreamConnectedKey, false);
                await stream.DisconnectAsync();
            }
        }

        /// <summary>
        /// {"vin":..,"entityId":..,"topic":..,"timestamp":..,"data":{"key":{"timestamp":..,"value":..,"unit":..}}}
        /// (shape not documented by BMW, taken from evcc)
        /// </summary>
        internal Task HandleMessageAsync(string topic, byte[] payload)
        {
            try
            {
                if (JsonNode.Parse(payload) is not JsonObject message)
                {
                    return Task.CompletedTask;
                }
                var vin = message["vin"]?.ToString() ?? topic?.Split('/').LastOrDefault();
                var values = BmwCarDataApi.ParseValues(message["data"] as JsonObject);
                foreach (var vehicle in _vehicles.Where(v => string.Equals(v.Vin, vin, StringComparison.OrdinalIgnoreCase)))
                {
                    vehicle.Apply(values);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                DriverContext.Logger.LogDebug($"Unreadable BMW CarData message: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var result = new List<NodeInstance>();
            var known = _vehicles.Select(v => v.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (vin, mappingType) in await Api.GetMappingsAsync(token))
            {
                if (known.Contains(vin))
                {
                    continue;
                }
                if (mappingType != null && !string.Equals(mappingType, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                {
                    DriverContext.Logger.LogWarning($"BMW CarData: {vin} is mapped as {mappingType}, only the primary user gets its data");
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(BmwCarDataDriverFactory.VehicleTemplate);
                node.Name = vin;
                try
                {
                    var basic = await Api.GetBasicDataAsync(vin, token);
                    var name = string.Join(" ", new[] { basic?["brand"]?.ToString(), basic?["modelName"]?.ToString() }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (name.Length > 0)
                    {
                        node.Name = name;
                    }
                }
                catch (BmwCarDataApiException ex)
                {
                    DriverContext.Logger.LogDebug($"BMW CarData basic data of {vin}: {ex.Message}");
                }
                node.SetProperty(BmwCarDataDriverFactory.VinKey, vin);
                foreach (var dp in BmwCarDataDataPoints.All.Where(d => d.DefaultCreated))
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
                case "vehicle":
                {
                    var vehicle = new BmwVehicleNode(ctx, this);
                    _vehicles.Add(vehicle);
                    return vehicle;
                }
                case BmwCarDataDriverFactory.LoginStatusKey:
                case BmwCarDataDriverFactory.StreamConnectedKey:
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

    internal sealed class BmwVehicleNode : DriverNoneAttributeBase
    {
        private readonly BmwCarDataDriver _account;
        private readonly List<(ValueNode Node, BmwDataPoint DataPoint)> _values = new();
        private readonly Dictionary<string, TelematicValue> _current = new(StringComparer.Ordinal);
        private readonly object _lock = new();

        public BmwVehicleNode(IDriverContext driverContext, BmwCarDataDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string Vin { get; private set; }
        internal IEnumerable<ValueNode> Values => _values.Select(v => v.Node);

        internal IEnumerable<string> CustomDescriptors => _values.Where(v => v.DataPoint.IsCustom && v.DataPoint.Key != BmwCarDataDataPoints.LastUpdateKey)
            .Select(v => v.Node.Parameter);

        public override Task<bool> Init(CancellationToken token = default)
        {
            Vin = GetPropertyValueString(BmwCarDataDriverFactory.VinKey)?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(Vin))
            {
                DriverContext.Logger.LogError($"{FullName}: no VIN configured, use the scan of the account");
            }
            return base.Init(token);
        }

        internal void Apply(IReadOnlyDictionary<string, TelematicValue> values)
        {
            lock (_lock)
            {
                foreach (var (key, value) in values)
                {
                    if (!_current.TryGetValue(key, out var existing) || existing.Timestamp == null || value.Timestamp == null || value.Timestamp >= existing.Timestamp)
                    {
                        _current[key] = value;
                    }
                }
            }
            foreach (var (node, dp) in _values)
            {
                object value = null;
                lock (_lock)
                {
                    if (dp.Key == BmwCarDataDataPoints.LastUpdateKey)
                    {
                        var newest = _current.Values.Where(v => v.Timestamp != null).Select(v => v.Timestamp.Value).DefaultIfEmpty().Max();
                        value = newest == default ? null : newest.UtcDateTime;
                    }
                    else
                    {
                        var descriptors = dp.IsCustom ? new[] { node.Parameter } : dp.Descriptors;
                        foreach (var descriptor in descriptors.Where(d => d != null))
                        {
                            if (_current.TryGetValue(descriptor, out var current) && BmwCarDataDataPoints.Convert(current.Value, dp.Type) is { } converted)
                            {
                                value = converted;
                                break;
                            }
                        }
                    }
                }
                if (value != null)
                {
                    node.Set(value);
                }
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _account.RefreshAllAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = BmwCarDataDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new ValueNode(ctx, key, null, null);
            if (BmwCarDataDataPoints.IsCustom(key))
            {
                node.Parameter = ctx.NodeInstance.GetProperty(BmwCarDataDriverFactory.CustomKeyKey)?.ValueString?.Trim();
            }
            _values.Add((node, dataPoint));
            return node;
        }
    }
}
