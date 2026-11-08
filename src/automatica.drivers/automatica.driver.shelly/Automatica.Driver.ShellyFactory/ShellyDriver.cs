using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Driver.Shelly.CoIoT;
using Automatica.Driver.Shelly.Rpc;
using Automatica.Driver.ShellyFactory.Discovery;
using Automatica.Driver.ShellyFactory.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.ShellyFactory
{
    internal class ShellyDriver : DriverBase
    {
        public List<ShellyDevice> DiscoveredShellys { get; private set; } = new List<ShellyDevice>();
        public List<ShellyDiscoveredHost> DiscoveredHosts { get; private set; } = new List<ShellyDiscoveredHost>();
        public ShellyDiscoveryService DiscoveryService { get; }
        private readonly List<ShellyDriverDevice> _devices = new List<ShellyDriverDevice>();
        private readonly List<ShellyGenericDevice> _genericDevices = new List<ShellyGenericDevice>();

        /// <summary>
        /// Devices that woke up / connected but are not configured yet (sleeping devices are asleep during a scan), keyed by MAC.
        /// </summary>
        private readonly ConcurrentDictionary<string, NodeInstance> _learned = new ConcurrentDictionary<string, NodeInstance>();
        private readonly ConcurrentDictionary<string, byte> _probing = new ConcurrentDictionary<string, byte>();

        private CoIoTListener _coiot;
        private ShellyOutboundWebSocketServer _outbound;
        private volatile bool _learnMode;

        /// <summary>CoIoT port, the tests use an ephemeral one</summary>
        internal int CoIoTPort { get; set; } = CoIoTListener.DefaultPort;
        internal bool CoIoTJoinMulticast { get; set; } = true;
        internal int? OutboundWebSocketPortOverride { get; set; }
        internal TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public int? OutboundWebSocketPort => _outbound?.Port;
        public int? CoIoTListenerPort => _coiot?.Port;
        public IReadOnlyCollection<NodeInstance> LearnedDevices => _learned.Values.ToList();
        internal IReadOnlyList<ShellyGenericDevice> GenericDevices => _genericDevices;

        public ShellyDriver(IDriverContext driverContext) : base(driverContext)
        {
            DiscoveryService = new ShellyDiscoveryService(driverContext.ZeroconfDiscovery, driverContext.Logger);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(false);
        }

        protected override bool CreateTelegramMonitor()
        {
            return true;
        }

        private async Task Discover()
        {
            try
            {
                var legacy = DiscoveryService.SearchShellys();
                var all = DiscoveryService.SearchAllAsync();
                DiscoveredShellys = await legacy;
                DiscoveredHosts = await all;
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogWarning($"Shelly mDNS discovery failed: {e.Message}");
            }
        }

        public override async Task<bool> Init(CancellationToken token = new CancellationToken())
        {
            await Discover();
            return await base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            await Discover();
            StartListeners();
            return await base.Start(token);
        }

        internal void StartListeners()
        {
            StopListeners();

            if (GetPropertyValueBool(ShellyGenericTemplates.CoIoTEnabledKey) ?? true)
            {
                try
                {
                    _coiot = new CoIoTListener(DriverContext.Logger, CoIoTPort, CoIoTJoinMulticast);
                    _coiot.MessageReceived += OnCoIoT;
                    _coiot.Start();
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogWarning($"Could not listen for Gen1 CoIoT on udp {CoIoTPort}: {e.Message}; Gen1 devices are polled only");
                    _coiot = null;
                }
            }

            var port = OutboundWebSocketPortOverride ?? GetPropertyValueInt(ShellyGenericTemplates.OutboundWebSocketPortKey);
            if (port > 0 || OutboundWebSocketPortOverride.HasValue)
            {
                try
                {
                    _outbound = new ShellyOutboundWebSocketServer(port, DriverContext.Logger, OutboundWebSocketPortOverride.HasValue ? IPAddress.Loopback : null);
                    _outbound.MessageReceived += OnOutboundMessage;
                    _outbound.Start();
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError($"Could not start the shelly outbound websocket server on port {port}: {e.Message}");
                    _outbound = null;
                }
            }
        }

        private void StopListeners()
        {
            if (_coiot != null)
            {
                _coiot.MessageReceived -= OnCoIoT;
                _coiot.Dispose();
                _coiot = null;
            }

            if (_outbound != null)
            {
                _outbound.MessageReceived -= OnOutboundMessage;
                _outbound.Dispose();
                _outbound = null;
            }
        }

        public override async Task<bool> Stop(CancellationToken token = new CancellationToken())
        {
            StopListeners();
            return await base.Stop(token);
        }

        internal void OnCoIoT(CoIoTMessage message, IPAddress sender)
        {
            if (!message.IsStatus)
            {
                return;
            }

            var device = _genericDevices.OfType<ShellyGen1Device>().FirstOrDefault(a => a.Matches(message.DeviceId));
            if (device != null)
            {
                device.HandleCoIoT(message, sender);
                return;
            }

            if (IsConfigured(ShellyGenericDevice.NormalizeMac(message.DeviceId)))
            {
                return;
            }

            // an unknown device is awake right now: learn it while it can be read
            _ = LearnAsync(message.DeviceId, sender.ToString());
        }

        internal void OnOutboundMessage(string source, string method, JObject parameters, IPAddress remote)
        {
            var device = _genericDevices.OfType<ShellyRpcDevice>().FirstOrDefault(a => a.Matches(source));
            if (device != null)
            {
                device.HandleNotification(method, parameters, remote);
                return;
            }

            if (method == "NotifyFullStatus")
            {
                LearnFromFullStatus(source, parameters, remote);
            }
        }

        private void LearnFromFullStatus(string source, JObject status, IPAddress remote)
        {
            var mac = ShellyGenericDevice.NormalizeMac(status.SelectToken("sys.mac")?.Value<string>() ?? source.Split('-')[^1]);
            if (mac.Length == 0 || IsConfigured(mac))
            {
                return;
            }

            // the device sleeps again right away: build it from the pushed status, the model comes from the id ("shellyhtg3-...")
            var info = new JObject
            {
                ["id"] = source,
                ["mac"] = mac,
                ["app"] = source.Contains('-') ? source.Substring(0, source.LastIndexOf('-')) : source,
                ["gen"] = 2
            };
            var node = ShellyNodeBuilder.BuildRpc(DriverContext.NodeTemplateFactory, info, status, null, remote.ToString(),
                GetPropertyValueString(ShellyGenericTemplates.DefaultPasswordKey));
            node.SetProperty(ShellyGenericTemplates.SleepingKey, true);
            AddLearned(mac, node);
        }

        private async Task LearnAsync(string deviceId, string address)
        {
            var key = ShellyGenericDevice.NormalizeMac(deviceId);
            if (key.Length == 0 || _learned.Keys.Any(a => a.EndsWith(key) || key.EndsWith(a)) || !_probing.TryAdd(key, 0))
            {
                return;
            }

            try
            {
                var node = await ShellyNodeBuilder.ProbeAsync(DriverContext.NodeTemplateFactory, address, DefaultCredentials, DriverContext.Logger,
                    CancellationToken.None, ProbeTimeout);
                if (node != null)
                {
                    AddLearned(ShellyGenericDevice.NormalizeMac(node.GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey)), node);
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogDebug($"Could not learn shelly {deviceId} at {address}: {e.Message}");
            }
            finally
            {
                _probing.TryRemove(key, out _);
            }
        }

        private void AddLearned(string mac, NodeInstance node)
        {
            if (!_learned.TryAdd(mac, node))
            {
                return;
            }

            DriverContext.Logger.LogInformation($"Learned shelly {node.Name} ({mac}), the next scan adds it");
            if (_learnMode)
            {
                NotifyLearned(node);
            }
        }

        private void NotifyLearned(NodeInstance node)
        {
            try
            {
                DriverContext.LearnMode?.NotifyLearnNode(node.Name, node.GetPropertyValueString(ShellyGenericTemplates.ModelKey), DriverContext.NodeInstance,
                    new List<NodeTemplate> { node.This2NodeTemplateNavigation }, node.PropertyInstance.ToList());
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogWarning($"Could not report the learned shelly {node.Name}: {e.Message}");
            }
        }

        private ShellyCredentials DefaultCredentials => new ShellyCredentials(
            GetPropertyValueString(ShellyGenericTemplates.DefaultUserNameKey),
            GetPropertyValueString(ShellyGenericTemplates.DefaultPasswordKey));

        /// <summary>
        /// Whether a device with this MAC is a node of the driver already (also disabled or failed ones).
        /// </summary>
        private bool IsConfigured(string mac)
        {
            if (string.IsNullOrEmpty(mac))
            {
                return false;
            }

            foreach (var node in DriverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                var id = ShellyGenericDevice.NormalizeMac(node.GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey));
                if (id.Length > 0 && (id == mac || id.EndsWith(mac) || mac.EndsWith(id)))
                {
                    return true;
                }
            }

            return _genericDevices.Any(a => a.Matches(mac)) || _devices.Any(a => ShellyGenericDevice.NormalizeMac(a.ShellyId) == mac);
        }

        public override Task<bool> EnableLearnMode(CancellationToken token = new CancellationToken())
        {
            _learnMode = true;
            foreach (var node in _learned.Values)
            {
                NotifyLearned(node);
            }
            return Task.FromResult(true);
        }

        public override Task<bool> DisableLearnMode(CancellationToken token = new CancellationToken())
        {
            _learnMode = false;
            return Task.FromResult(true);
        }

        public override Task OnDelete(NodeInstance instance, CancellationToken token = new CancellationToken())
        {
            _devices.RemoveAll(a => a.DriverContext.NodeInstance.ObjId == instance.ObjId);
            _genericDevices.RemoveAll(a => a.DriverContext.NodeInstance.ObjId == instance.ObjId);

            return base.OnDelete(instance, token);
        }

        /// <summary>
        /// Finds every Shelly (mDNS, the addresses of the scan property, devices that woke up meanwhile), reads what it is
        /// and returns the device nodes with exactly the components and data points of the device.
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = new CancellationToken())
        {
            await Discover();

            var addresses = DiscoveredHosts.Select(a => a.IpAddress).ToList();
            var configured = GetPropertyValueString(ShellyGenericTemplates.ScanAddressesKey);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                addresses.AddRange(configured.Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()));
            }

            var ret = new List<NodeInstance>();
            var credentials = DefaultCredentials;
            using var limit = new SemaphoreSlim(8);
            var probes = addresses.Distinct().Select(async address =>
            {
                await limit.WaitAsync(token);
                try
                {
                    return await ShellyNodeBuilder.ProbeAsync(DriverContext.NodeTemplateFactory, address, credentials, DriverContext.Logger, token, ProbeTimeout);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // one address that misbehaves must not fail the whole scan
                    DriverContext.Logger.LogWarning(e, $"Could not probe {address}");
                    return null;
                }
                finally
                {
                    limit.Release();
                }
            }).ToList();

            foreach (var node in await Task.WhenAll(probes))
            {
                if (node == null)
                {
                    continue;
                }

                var mac = ShellyGenericDevice.NormalizeMac(node.GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey));
                _learned.TryRemove(mac, out _);
                if (IsConfigured(mac))
                {
                    DriverContext.Logger.LogInformation($"Shelly {mac} is configured already...");
                    continue;
                }

                if (ret.Any(a => ShellyGenericDevice.NormalizeMac(a.GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey)) == mac))
                {
                    continue;
                }

                ret.Add(node);
            }

            foreach (var learned in _learned.ToList())
            {
                if (!IsConfigured(learned.Key))
                {
                    ret.Add(learned.Value);
                }
                _learned.TryRemove(learned.Key, out _);
            }

            return ret;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;

            switch (key)
            {
                case ShellyGenericTemplates.Gen1DeviceKey:
                {
                    var device = new ShellyGen1Device(ctx, TelegramMonitor);
                    _genericDevices.Add(device);
                    return device;
                }
                case ShellyGenericTemplates.RpcDeviceKey:
                {
                    var device = new ShellyRpcDevice(ctx, TelegramMonitor);
                    _genericDevices.Add(device);
                    return device;
                }
                default:
                {
                    // the model templates of driver version 0.2 (Shelly 1, 1PM, 2.5, Plus 1PM, Plus 2PM, Plus Plug S)
                    var shellyDevice = new ShellyDriverDevice(ctx, TelegramMonitor);
                    _devices.Add(shellyDevice);
                    return shellyDevice;
                }
            }
        }
    }
}
