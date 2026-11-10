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

namespace P3.Driver.Refoss.Driver
{
    /// <summary>
    /// A Refoss energy monitor on the LAN. One Em.Status.Get call per poll reads all channels.
    /// </summary>
    internal sealed class RefossDriver : DriverNoneAttributeBase
    {
        /// <summary>
        /// "all instances" in the Refoss API
        /// </summary>
        public const int AllChannels = 65535;

        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(10) };

        private readonly List<RefossChannel> _channels = new();
        private readonly List<RefossValueNode> _values = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;
        private bool _perChannel;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal RefossRpcClient Rpc { get; private set; }
        internal IReadOnlyList<RefossChannel> Channels => _channels;
        internal IReadOnlyList<RefossValueNode> Values => _values;

        /// <summary>
        /// The device refused id=65535, every channel is asked on its own
        /// </summary>
        internal bool PerChannel => _perChannel;

        public RefossDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(RefossDriverFactory.PollKey), 2, 3600));
            _perChannel = false;
            var host = GetPropertyValueString(RefossDriverFactory.HostKey);
            Rpc = new RefossRpcClient(Http, host, GetPropertyValueString(RefossDriverFactory.PasswordKey));
            if (string.IsNullOrWhiteSpace(host))
            {
                DriverContext.Logger.LogError("No address of the Refoss device configured");
            }
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            await PollSafeAsync(token);
            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            return base.Stop(token);
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
                DriverContext.Logger.LogError($"Refoss poll failed: {ex.Message}");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollSafeAsync(token);
            return true;
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                Dictionary<int, JsonElement> status;
                try
                {
                    status = await StatusAsync(_channels.Select(c => c.ChannelId).Where(id => id > 0).Distinct().ToList(), token);
                }
                catch (Exception ex) when (ex is RefossRpcException or HttpRequestException or JsonException or TaskCanceledException && !token.IsCancellationRequested)
                {
                    SetConnected(false);
                    throw;
                }

                foreach (var channel in _channels)
                {
                    if (status.TryGetValue(channel.ChannelId, out var entry))
                    {
                        channel.Apply(entry);
                    }
                }
                SetConnected(true);
            }
            finally
            {
                _pollLock.Release();
            }
        }

        /// <summary>
        /// Status entries by channel id: all channels in one call, or one call per channel when the device refuses id=65535
        /// </summary>
        internal async Task<Dictionary<int, JsonElement>> StatusAsync(IReadOnlyList<int> wanted, CancellationToken token)
        {
            if (!_perChannel)
            {
                try
                {
                    var all = RefossDataPoints.Entries(await Rpc.CallAsync("Em.Status.Get", ChannelParams(AllChannels), token), "status");
                    if (all.Count > 0 || wanted.Count == 0)
                    {
                        return all;
                    }
                }
                catch (RefossRpcException ex) when (ex.Code is not (0 or 401))
                {
                    DriverContext.Logger.LogInformation($"Refoss: Em.Status.Get for all channels refused ({ex.Message}), reading the channels one by one");
                }
                _perChannel = true;
            }

            var result = new Dictionary<int, JsonElement>();
            foreach (var id in wanted)
            {
                try
                {
                    foreach (var (key, value) in RefossDataPoints.Entries(await Rpc.CallAsync("Em.Status.Get", ChannelParams(id), token), "status"))
                    {
                        result[key] = value;
                    }
                }
                catch (RefossRpcException ex) when (ex.Code is not (0 or 401))
                {
                    DriverContext.Logger.LogWarning($"Refoss: channel {id}: {ex.Message}");
                }
            }
            return result;
        }

        private static Dictionary<string, object> ChannelParams(int id) => new() { ["id"] = id };

        private void SetConnected(bool connected)
        {
            foreach (var value in _values.Where(v => v.DataPoint.Key == RefossDataPoints.ConnectedKey))
            {
                value.Set(connected);
            }
        }

        /// <summary>
        /// Lists the channels of the device that are not configured yet, named after the names set in the Refoss app
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var names = new Dictionary<int, string>();
            try
            {
                foreach (var (id, entry) in RefossDataPoints.Entries(await Rpc.CallAsync("Em.Config.Get", ChannelParams(AllChannels), token), "config"))
                {
                    names[id] = RefossDataPoints.Text(entry, "name");
                }
            }
            catch (RefossRpcException ex) when (ex.Code is not (0 or 401))
            {
                DriverContext.Logger.LogWarning($"Refoss: could not read the channel names: {ex.Message}");
            }

            var ids = names.Keys.ToHashSet();
            try
            {
                ids.UnionWith((await StatusAsync(Array.Empty<int>(), token)).Keys);
            }
            catch (RefossRpcException ex) when (ex.Code is not (0 or 401))
            {
                DriverContext.Logger.LogWarning($"Refoss: could not read the channel list: {ex.Message}");
            }
            if (ids.Count == 0)
            {
                ids.UnionWith(Enumerable.Range(1, RefossDataPoints.ChannelCount));
            }

            var known = _channels.Select(c => c.ChannelId).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var id in ids.Where(i => !known.Contains(i)).OrderBy(i => i))
            {
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(RefossDriverFactory.ChannelTemplate);
                node.Name = ChannelLabel(id, names.GetValueOrDefault(id));
                node.SetProperty(RefossDriverFactory.ChannelKey, id);
                foreach (var dp in RefossDataPoints.Channel)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        /// <summary>
        /// "A1", or "A1 Heat pump" when the channel got a name in the Refoss app
        /// </summary>
        internal static string ChannelLabel(int id, string name)
        {
            var ct = RefossDataPoints.ChannelName(id);
            name = name?.Trim();
            return string.IsNullOrEmpty(name) || string.Equals(name, ct, StringComparison.OrdinalIgnoreCase) ? ct : $"{ct} {name}";
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            if (key == RefossDriverFactory.ChannelTemplateKey)
            {
                var channel = new RefossChannel(ctx, this);
                _channels.Add(channel);
                return channel;
            }

            var dataPoint = RefossDataPoints.Device.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new RefossValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    /// <summary>
    /// One CT channel (or merged channel) of the device
    /// </summary>
    internal sealed class RefossChannel : DriverNoneAttributeBase
    {
        private readonly RefossDriver _device;
        private readonly List<RefossValueNode> _values = new();

        public RefossChannel(IDriverContext driverContext, RefossDriver device) : base(driverContext)
        {
            _device = device;
        }

        internal int ChannelId { get; private set; }
        internal IReadOnlyList<RefossValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            ChannelId = GetPropertyValueInt(RefossDriverFactory.ChannelKey);
            if (ChannelId <= 0)
            {
                DriverContext.Logger.LogError($"{FullName}: no channel number configured");
            }
            return base.Init(token);
        }

        internal void Apply(JsonElement entry)
        {
            foreach (var value in _values)
            {
                var read = value.DataPoint.Read?.Invoke(entry);
                if (read != null)
                {
                    value.Set(read);
                }
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await _device.Read(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = RefossDataPoints.Channel.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new RefossValueNode(ctx, _device, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class RefossValueNode : DriverBase
    {
        private readonly RefossDriver _device;
        private readonly object _lock = new();
        private bool _hasValue;

        public RefossValueNode(IDriverContext driverContext, RefossDriver device, RefossDataPoint dataPoint) : base(driverContext)
        {
            _device = device;
            DataPoint = dataPoint;
        }

        public RefossDataPoint DataPoint { get; }

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

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default) => Task.CompletedTask;

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _device.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
