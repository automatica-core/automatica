using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Tuya.Driver
{
    /// <summary>
    /// The Tuya devices on the LAN. The scan lists the devices that announce themselves (UDP 6666 / 6667) and are not configured yet.
    /// </summary>
    internal sealed class TuyaDriver : DriverNoneAttributeBase
    {
        public static readonly int[] DiscoveryPorts = { 6666, 6667 };

        private readonly List<TuyaPlug> _plugs = new();

        internal IReadOnlyList<TuyaPlug> Plugs => _plugs;

        /// <summary>
        /// How long the scan listens; the devices announce themselves about every 5 s
        /// </summary>
        internal TimeSpan ScanTime { get; set; } = TimeSpan.FromSeconds(12);

        public TuyaDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _plugs.Select(p => p.DeviceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<NodeInstance>();
            foreach (var found in (await DiscoverAsync(ScanTime, token)).Where(f => !known.Contains(f.Id)))
            {
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(TuyaDriverFactory.PlugTemplate);
                node.Name = $"Tuya {found.Ip}";
                node.SetProperty(TuyaDriverFactory.HostKey, found.Ip);
                node.SetProperty(TuyaDriverFactory.DeviceIdKey, found.Id);
                node.SetProperty(TuyaDriverFactory.VersionKey, found.Version);
                node.SetProperty(TuyaDriverFactory.PollKey, 10);
                foreach (var dp in TuyaDataPoints.Plug)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        internal sealed record Discovered(string Id, string Ip, string Version);

        /// <summary>
        /// Listens for the discovery broadcasts for <paramref name="time"/>
        /// </summary>
        internal async Task<IReadOnlyList<Discovered>> DiscoverAsync(TimeSpan time, CancellationToken token)
        {
            var found = new Dictionary<string, Discovered>(StringComparer.OrdinalIgnoreCase);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(time);
            var sockets = new List<UdpClient>();
            try
            {
                foreach (var port in DiscoveryPorts)
                {
                    try
                    {
                        var udp = new UdpClient { ExclusiveAddressUse = false };
                        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                        sockets.Add(udp);
                    }
                    catch (SocketException ex)
                    {
                        DriverContext.Logger.LogWarning($"Tuya: cannot listen on UDP {port}: {ex.Message}");
                    }
                }

                await Task.WhenAll(sockets.Select(async udp =>
                {
                    try
                    {
                        while (true)
                        {
                            var packet = await udp.ReceiveAsync(cts.Token);
                            if (Parse(packet.Buffer, packet.RemoteEndPoint.Address.ToString()) is { } d)
                            {
                                lock (found)
                                {
                                    found[d.Id] = d;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }));
            }
            finally
            {
                sockets.ForEach(s => s.Dispose());
            }
            token.ThrowIfCancellationRequested();
            return found.Values.OrderBy(d => d.Ip).ToList();
        }

        internal static Discovered Parse(byte[] packet, string sender)
        {
            if (TuyaProtocol.DecodeDiscovery(packet) is not { } json)
            {
                return null;
            }
            var id = Text(json, "gwId") ?? Text(json, "devId");
            return string.IsNullOrEmpty(id) ? null : new Discovered(id, Text(json, "ip") ?? sender, Text(json, "version") ?? "3.3");
        }

        private static string Text(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != TuyaDriverFactory.PlugTemplateKey)
            {
                return null;
            }
            var plug = new TuyaPlug(ctx);
            _plugs.Add(plug);
            return plug;
        }
    }

    /// <summary>
    /// One plug: a local session that is kept open, a status query every poll (which also keeps the session alive) and the
    /// status the plug sends by itself when it is switched in the app or with its button
    /// </summary>
    internal sealed class TuyaPlug : DriverNoneAttributeBase
    {
        private readonly List<TuyaValueNode> _values = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;
        private TuyaClient _client;
        private string _host;
        private string _localKey;
        private string _version;
        private bool _stopped;

        public TuyaPlug(IDriverContext driverContext) : base(driverContext)
        {
        }

        internal string DeviceId { get; private set; }
        internal int Port { get; set; } = TuyaClient.Port;
        internal IReadOnlyList<TuyaValueNode> Values => _values;
        internal bool IsConnected => _client?.IsConnected == true;

        public override Task<bool> Init(CancellationToken token = default)
        {
            _host = GetPropertyValueString(TuyaDriverFactory.HostKey)?.Trim();
            DeviceId = GetPropertyValueString(TuyaDriverFactory.DeviceIdKey)?.Trim();
            _localKey = GetPropertyValueString(TuyaDriverFactory.LocalKeyKey);
            _version = GetPropertyValueString(TuyaDriverFactory.VersionKey)?.Trim();
            if (string.IsNullOrEmpty(_version))
            {
                _version = "3.4";
            }
            _poll = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(TuyaDriverFactory.PollKey), 2, 3600));

            if (string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(DeviceId))
            {
                DriverContext.Logger.LogError($"{FullName}: address or device id missing");
            }
            if (_localKey?.Length != 16)
            {
                DriverContext.Logger.LogError($"{FullName}: the local key must have 16 characters (Tuya IoT platform → Query Device Details)");
            }
            if (!TuyaProtocol.IsSupported(_version))
            {
                DriverContext.Logger.LogError($"{FullName}: protocol version {_version} is not supported (3.3 and 3.4 are)");
            }
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _stopped = false;
            await PollSafeAsync(token);
            _timer?.Dispose();
            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            _stopped = true;
            _timer?.Dispose();
            _timer = null;
            await _lock.WaitAsync(token);
            try
            {
                await CloseAsync();
            }
            finally
            {
                _lock.Release();
            }
            SetConnected(false);
            return await base.Stop(token);
        }

        private async void TimerElapsed(object state) => await PollSafeAsync(CancellationToken.None);

        private async Task PollSafeAsync(CancellationToken token)
        {
            try
            {
                await PollAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
            {
                DriverContext.Logger.LogWarning($"{FullName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Connects when needed and asks for the status; the answer arrives in <see cref="Apply"/>
        /// </summary>
        internal async Task PollAsync(CancellationToken token = default)
        {
            var client = await ClientAsync(token);
            try
            {
                await client.QueryAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DropAsync(client);
                throw;
            }
        }

        internal async Task SetAsync(TuyaDataPoint dp, object value, CancellationToken token)
        {
            var client = await ClientAsync(token);
            try
            {
                await client.SetAsync(new Dictionary<string, object> { [dp.Dp] = dp.ToRaw(value) }, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DropAsync(client);
                throw;
            }
        }

        private async Task<TuyaClient> ClientAsync(CancellationToken token)
        {
            await _lock.WaitAsync(token);
            try
            {
                if (_client is { IsConnected: true })
                {
                    return _client;
                }
                await CloseAsync();
                if (_stopped)
                {
                    throw new OperationCanceledException("stopped");
                }
                if (string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(DeviceId) || _localKey?.Length != 16 || !TuyaProtocol.IsSupported(_version))
                {
                    throw new InvalidOperationException("not configured completely");
                }

                var client = new TuyaClient(_host, DeviceId, _localKey, _version, Port);
                client.DpsReceived += Apply;
                client.Disconnected += ex =>
                {
                    DriverContext.Logger.LogInformation($"{FullName}: session ended ({ex.Message})");
                    SetConnected(false);
                };
                try
                {
                    await client.ConnectAsync(token);
                }
                catch
                {
                    await client.DisposeAsync();
                    SetConnected(false);
                    throw;
                }
                _client = client;
                SetConnected(true);
                return client;
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DropAsync(TuyaClient client)
        {
            await _lock.WaitAsync();
            try
            {
                if (_client == client)
                {
                    await CloseAsync();
                }
            }
            finally
            {
                _lock.Release();
            }
            SetConnected(false);
        }

        private async Task CloseAsync()
        {
            if (_client != null)
            {
                var client = _client;
                _client = null;
                await client.DisposeAsync();
            }
        }

        internal void Apply(IReadOnlyDictionary<string, JsonElement> dps)
        {
            foreach (var value in _values)
            {
                if (value.DataPoint.Dp != null && dps.TryGetValue(value.DataPoint.Dp, out var raw) && value.DataPoint.Read(raw) is { } read)
                {
                    value.Set(read);
                }
            }
        }

        private void SetConnected(bool connected)
        {
            foreach (var value in _values.Where(v => v.DataPoint.Key == TuyaDataPoints.ConnectedKey))
            {
                value.Set(connected);
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollSafeAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = TuyaDataPoints.Plug.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new TuyaValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class TuyaValueNode : DriverBase
    {
        private readonly TuyaPlug _plug;
        private readonly object _lock = new();
        private bool _hasValue;

        public TuyaValueNode(IDriverContext driverContext, TuyaPlug plug, TuyaDataPoint dataPoint) : base(driverContext)
        {
            _plug = plug;
            DataPoint = dataPoint;
        }

        public TuyaDataPoint DataPoint { get; }

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

        /// <summary>
        /// Sends the value to the plug; the plug confirms with its new status, which updates the value
        /// </summary>
        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.Writable || value == null)
            {
                return;
            }
            try
            {
                await _plug.SetAsync(DataPoint, value, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{FullName}: could not set {DataPoint.Key} to {value}: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _plug.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
