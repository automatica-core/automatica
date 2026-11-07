using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using P3.Driver.Mammotion.Driver.Protocol;

namespace P3.Driver.Mammotion.Driver
{
    internal sealed class MammotionDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(20) };

        private readonly List<MammotionMower> _mowers = new();
        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;
        private IMammotionMqtt _mqtt;
        private int _seq;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal string AuthUrl { get; set; }
        internal Func<IMammotionMqtt> MqttFactory { get; set; } = () => new MqttNetConnection();
        internal MammotionCloud Cloud { get; private set; }
        internal IReadOnlyList<MammotionMower> Mowers => _mowers;

        public MammotionDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        internal DateTimeOffset Now => DriverContext.TimeProvider.GetUtcNow();

        internal int NextSeq() => Interlocked.Increment(ref _seq);

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(30, GetPropertyValueInt(MammotionDriverFactory.PollKey)));
            Cloud = new MammotionCloud(Http, DriverContext.Logger, () => Now, AuthUrl,
                GetPropertyValueString(MammotionDriverFactory.UsernameKey), GetPropertyValueString(MammotionDriverFactory.PasswordKey));
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            try
            {
                await ConnectAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError(ex, "Could not connect to the Mammotion cloud, retrying with the next poll");
            }
            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            if (_mqtt != null)
            {
                await _mqtt.DisposeAsync();
                _mqtt = null;
            }
            return await base.Stop(token);
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                await PollAsync();
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Mammotion poll failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        /// <summary>
        /// Reconnects when the MQTT connection is gone, otherwise asks the mowers for fresh reports
        /// </summary>
        internal async Task PollAsync(CancellationToken token = default)
        {
            if (_mqtt?.IsConnected != true)
            {
                await ConnectAsync(token);
                return;
            }
            await RequestReportsAsync(token);
        }

        /// <summary>
        /// Login, resolve product key and device name of the configured mowers, connect to MQTT and subscribe their topics
        /// </summary>
        internal async Task ConnectAsync(CancellationToken token = default)
        {
            await _connectLock.WaitAsync(token);
            try
            {
                var devices = await Cloud.ListDevicesAsync(token);
                foreach (var mower in _mowers)
                {
                    var device = devices.FirstOrDefault(d => string.Equals(d.IotId, mower.IotId, StringComparison.Ordinal));
                    if (device == null)
                    {
                        DriverContext.Logger.LogWarning($"Mower {mower.IotId} is not part of the Mammotion account");
                        continue;
                    }
                    mower.ProductKey = device.ProductKey;
                    mower.DeviceName = device.DeviceName;
                }

                var topics = _mowers.Where(m => !string.IsNullOrEmpty(m.DeviceName) && !string.IsNullOrEmpty(m.ProductKey))
                    .SelectMany(m => new[]
                    {
                        $"/sys/{m.ProductKey}/{m.DeviceName}/thing/event/+/post",
                        $"/sys/proto/{m.ProductKey}/{m.DeviceName}/thing/event/+/post",
                        $"/sys/{m.ProductKey}/{m.DeviceName}/app/down/thing/status"
                    }).ToList();
                if (topics.Count == 0)
                {
                    DriverContext.Logger.LogInformation("No Mammotion mower configured, use the scan of the account");
                    return;
                }

                var credentials = await Cloud.GetMqttCredentialsAsync(token);
                if (_mqtt != null)
                {
                    await _mqtt.DisposeAsync();
                }
                _mqtt = MqttFactory();
                await _mqtt.ConnectAsync(credentials, topics, HandleMessageAsync, token);
                DriverContext.Logger.LogInformation($"Connected to the Mammotion MQTT broker {credentials.Host}");
            }
            finally
            {
                _connectLock.Release();
            }
            await RequestReportsAsync(token);
        }

        private async Task RequestReportsAsync(CancellationToken token)
        {
            foreach (var mower in _mowers.Where(m => !string.IsNullOrEmpty(m.IotId)))
            {
                try
                {
                    await mower.RequestReportsAsync(token);
                }
                catch (Exception ex) when (ex is MammotionApiException or HttpRequestException or JsonException)
                {
                    DriverContext.Logger.LogWarning($"Could not request reports from mower {mower.IotId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// /sys/{pk}/{dn}/... or /sys/proto/{pk}/{dn}/...
        /// </summary>
        internal static (string ProductKey, string DeviceName) ParseTopic(string topic)
        {
            var parts = topic?.Split('/') ?? Array.Empty<string>();
            if (parts.Length < 4 || parts[1] != "sys")
            {
                return (null, null);
            }
            return parts[2] == "proto" ? (parts.Length > 4 ? (parts[3], parts[4]) : (null, null)) : (parts[2], parts[3]);
        }

        internal Task HandleMessageAsync(string topic, byte[] payload)
        {
            try
            {
                HandleMessage(topic, payload);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or FormatException or InvalidOperationException)
            {
                DriverContext.Logger.LogDebug($"Ignoring Mammotion message on {topic}: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void HandleMessage(string topic, byte[] payload)
        {
            var (productKey, deviceName) = ParseTopic(topic);
            var mower = _mowers.FirstOrDefault(m => m.DeviceName == deviceName && (m.ProductKey == null || m.ProductKey == productKey));
            if (mower == null || payload == null || payload.Length == 0)
            {
                return;
            }

            var isJson = payload[0] == (byte)'{';
            if (topic.EndsWith("/thing/status", StringComparison.Ordinal))
            {
                if (isJson && ParseOnline(payload) is { } online)
                {
                    mower.SetOnline(online);
                }
                return;
            }

            mower.SetOnline(true);
            if (!isJson)
            {
                mower.Apply(LubaMessages.Decode(payload));
                return;
            }

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var content = Content(parameters) ?? (parameters.TryGetProperty("value", out var value) ? Content(value) : null);
            if (content != null)
            {
                mower.Apply(LubaMessages.Decode(Convert.FromBase64String(content)));
            }

            if (parameters.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object)
            {
                mower.Apply(new MowerStatus
                {
                    Battery = Item(items, "batteryPercentage"),
                    DeviceState = Item(items, "deviceState"),
                    KnifeHeight = Item(items, "knifeHeight")
                });
            }
        }

        private static string Content(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
        }

        private static int? Item(JsonElement items, string name)
        {
            if (!items.TryGetProperty(name, out var item))
            {
                return null;
            }
            var value = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("value", out var v) ? v : item;
            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(value.GetString(), out var i) => i,
                _ => null
            };
        }

        /// <summary>
        /// {"action":"online"|"offline"} (Mammotion) or {"params":{"status":{"value":"1"|"3"}}} (Aliyun)
        /// </summary>
        internal static bool? ParseOnline(byte[] payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var action = MammotionCloud.Str(root, "action");
            if (action != null)
            {
                return action.Equals("online", StringComparison.OrdinalIgnoreCase);
            }
            if (root.TryGetProperty("params", out var parameters) && parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("status", out var status))
            {
                var value = status.ValueKind == JsonValueKind.Object ? MammotionCloud.Str(status, "value") : status.ValueKind == JsonValueKind.String ? status.GetString() : status.GetRawText();
                return value is "1" or "online";
            }
            return null;
        }

        /// <summary>
        /// Lists the mowers of the account that are not configured yet (RTK base stations are left out)
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _mowers.Select(m => m.IotId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var device in await Cloud.ListDevicesAsync(token))
            {
                if (known.Contains(device.IotId) || (device.DeviceName?.StartsWith("RTK", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(MammotionDriverFactory.MowerTemplate);
                node.Name = string.IsNullOrWhiteSpace(device.NickName) ? device.DeviceName ?? device.IotId : device.NickName;
                node.SetProperty(MammotionDriverFactory.IotIdKey, device.IotId);
                foreach (var dp in MammotionDataPoints.All)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != "mower")
            {
                return null;
            }
            var mower = new MammotionMower(ctx, this);
            _mowers.Add(mower);
            return mower;
        }
    }

    internal sealed class MammotionMower : DriverNoneAttributeBase
    {
        private readonly MammotionDriver _account;
        private readonly List<MammotionValueNode> _values = new();

        public MammotionMower(IDriverContext driverContext, MammotionDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string IotId { get; private set; }
        internal string ProductKey { get; set; }
        internal string DeviceName { get; set; }
        internal IReadOnlyList<MammotionValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            IotId = GetPropertyValueString(MammotionDriverFactory.IotIdKey)?.Trim();
            if (string.IsNullOrWhiteSpace(IotId))
            {
                DriverContext.Logger.LogError($"{FullName}: no IoT id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        private void Set(MammotionDataPoint dataPoint, object value)
        {
            if (value == null)
            {
                return;
            }
            foreach (var node in _values.Where(v => v.DataPoint == dataPoint))
            {
                node.Set(value);
            }
        }

        internal void SetOnline(bool online) => Set(MammotionDataPoints.Online, online);

        internal void Apply(MowerStatus status)
        {
            Set(MammotionDataPoints.Battery, status.Battery);
            Set(MammotionDataPoints.DeviceState, status.DeviceState);
            if (status.DeviceState is { } state)
            {
                Set(MammotionDataPoints.DeviceStateText, LubaMessages.DeviceStates.TryGetValue(state, out var text) ? text : $"unknown_{state}");
            }
            Set(MammotionDataPoints.ChargeState, status.ChargeState);
            Set(MammotionDataPoints.Progress, status.Progress);
            Set(MammotionDataPoints.Area, status.Area);
            Set(MammotionDataPoints.RemainingTime, status.RemainingMinutes);
            Set(MammotionDataPoints.KnifeHeight, status.KnifeHeight);
        }

        internal Task RequestReportsAsync(CancellationToken token = default)
        {
            return _account.Cloud.InvokeAsync(IotId, LubaMessages.RequestReports(_account.NextSeq(), _account.Cloud.UserAccount, _account.Now.ToUnixTimeMilliseconds()), token);
        }

        internal async Task ExecuteAsync(MowerAction action, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(IotId))
            {
                return;
            }
            await _account.Cloud.EnsureTokenAsync(token);
            DriverContext.Logger.LogInformation($"{IotId}: {action}");
            await _account.Cloud.InvokeAsync(IotId, LubaMessages.TaskControl(action, _account.NextSeq(), _account.Cloud.UserAccount, _account.Now.ToUnixTimeMilliseconds()), token);
            await RequestReportsAsync(token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await RequestReportsAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = MammotionDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new MammotionValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class MammotionValueNode : DriverBase
    {
        private readonly MammotionMower _mower;
        private readonly object _lock = new();
        private bool _hasValue;

        public MammotionValueNode(IDriverContext driverContext, MammotionMower mower, MammotionDataPoint dataPoint) : base(driverContext)
        {
            _mower = mower;
            DataPoint = dataPoint;
        }

        public MammotionDataPoint DataPoint { get; }

        public object LastValue { get; private set; }

        public void Set(object value)
        {
            lock (_lock)
            {
                if (_hasValue && Equals(LastValue, value))
                {
                    return;
                }
                _hasValue = true;
                LastValue = value;
            }
            DispatchRead(value);
        }

        internal static bool ToBool(object value) => value switch
        {
            bool b => b,
            null => false,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1",
            IConvertible c => Convert.ToDouble(c, System.Globalization.CultureInfo.InvariantCulture) != 0,
            _ => false
        };

        internal Task WriteDirectAsync(object value, CancellationToken token = default) => Write(value, null, token);

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.IsAction || !ToBool(value))
            {
                return; // momentary button released
            }
            try
            {
                await _mower.ExecuteAsync(DataPoint.Action!.Value, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{_mower.IotId}: {DataPoint.Key} failed: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _mower.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
