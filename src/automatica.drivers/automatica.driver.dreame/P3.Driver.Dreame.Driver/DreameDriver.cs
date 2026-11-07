using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Dreame.Driver
{
    internal sealed class DreameDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(20) };

        private readonly List<DreameVacuum> _vacuums = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal string BaseUrlOverride { get; set; }
        internal DreameCloud Cloud { get; private set; }
        internal IReadOnlyList<DreameVacuum> Vacuums => _vacuums;

        public DreameDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(15, GetPropertyValueInt(DreameDriverFactory.PollKey)));
            var baseUrl = BaseUrlOverride ?? DreameCloud.RegionUrl(GetPropertyValueString(DreameDriverFactory.RegionKey));
            Cloud = new DreameCloud(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(), baseUrl,
                GetPropertyValueString(DreameDriverFactory.UsernameKey), GetPropertyValueString(DreameDriverFactory.PasswordKey));
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            try
            {
                await PollAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError(ex, "First Dreame poll failed, retrying with the next poll");
            }
            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            return base.Stop(token);
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                await PollAsync();
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Dreame poll failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                foreach (var vacuum in _vacuums)
                {
                    try
                    {
                        await vacuum.RefreshAsync(token);
                    }
                    catch (Exception ex) when (ex is DreameApiException or HttpRequestException or JsonException)
                    {
                        DriverContext.Logger.LogError($"Could not read vacuum {vacuum.Did}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        /// <summary>
        /// Lists the vacuums of the account that are not configured yet
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _vacuums.Select(v => v.Did).ToHashSet();
            var result = new List<NodeInstance>();
            foreach (var device in await Cloud.ListDevicesAsync(token))
            {
                if (known.Contains(device.Did) || (device.Model != null && !device.Model.Contains(".vacuum.", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(DreameDriverFactory.VacuumTemplate);
                node.Name = string.IsNullOrWhiteSpace(device.Name) ? device.Model ?? device.Did : device.Name;
                node.SetProperty(DreameDriverFactory.DidKey, device.Did);
                foreach (var dp in DreameDataPoints.All)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != "vacuum")
            {
                return null;
            }
            var vacuum = new DreameVacuum(ctx, this);
            _vacuums.Add(vacuum);
            return vacuum;
        }
    }

    internal sealed class DreameVacuum : DriverNoneAttributeBase
    {
        private readonly DreameDriver _account;
        private readonly List<DreameValueNode> _values = new();
        private string _bindDomain;

        public DreameVacuum(IDriverContext driverContext, DreameDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string Did { get; private set; }
        internal IReadOnlyList<DreameValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            Did = GetPropertyValueString(DreameDriverFactory.DidKey)?.Trim();
            if (string.IsNullOrWhiteSpace(Did))
            {
                DriverContext.Logger.LogError($"{FullName}: no device id configured, use the scan of the account");
            }
            return base.Init(token);
        }

        private async Task<string> BindDomainAsync(CancellationToken token)
        {
            return _bindDomain ??= await _account.Cloud.GetBindDomainAsync(Did, token);
        }

        internal async Task RefreshAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Did))
            {
                return;
            }
            var properties = _values.Where(v => v.DataPoint.IsProperty).Select(v => (v.DataPoint.Siid, v.DataPoint.Id)).Distinct().ToList();
            if (properties.Count == 0)
            {
                return;
            }

            var values = await _account.Cloud.GetPropertiesAsync(Did, await BindDomainAsync(token), properties, token);
            foreach (var node in _values.Where(v => v.DataPoint.IsProperty))
            {
                if (values.TryGetValue((node.DataPoint.Siid, node.DataPoint.Id), out var raw))
                {
                    var value = Convert(raw);
                    node.Set(node.DataPoint.Map != null ? node.DataPoint.Map(value) : value);
                }
            }
        }

        private static object Convert(JsonElement raw)
        {
            return raw.ValueKind switch
            {
                JsonValueKind.Number when raw.TryGetInt32(out var i) => i,
                JsonValueKind.Number => raw.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => raw.GetString(),
                _ => raw.ToString()
            };
        }

        internal async Task ExecuteAsync(DreameDataPoint dataPoint, object value, CancellationToken token = default)
        {
            var bindDomain = await BindDomainAsync(token);
            switch (dataPoint.Access)
            {
                case DreameAccess.Action:
                    if (!DreameValueNode.ToBool(value))
                    {
                        return; // momentary button released
                    }
                    DriverContext.Logger.LogInformation($"{Did}: action {dataPoint.Key} ({dataPoint.Siid}.{dataPoint.Id})");
                    await _account.Cloud.ActionAsync(Did, bindDomain, dataPoint.Siid, dataPoint.Id, token);
                    break;
                case DreameAccess.ReadWrite:
                    await _account.Cloud.SetPropertyAsync(Did, bindDomain, dataPoint.Siid, dataPoint.Id, JsonValue.Create(System.Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)), token);
                    break;
                default:
                    return;
            }
            await RefreshAsync(token);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await RefreshAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = DreameDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new DreameValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class DreameValueNode : DriverBase
    {
        private readonly DreameVacuum _vacuum;
        private readonly object _lock = new();
        private bool _hasValue;

        public DreameValueNode(IDriverContext driverContext, DreameVacuum vacuum, DreameDataPoint dataPoint) : base(driverContext)
        {
            _vacuum = vacuum;
            DataPoint = dataPoint;
        }

        public DreameDataPoint DataPoint { get; }

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

        public object LastValue { get; private set; }

        internal static bool ToBool(object value) => value switch
        {
            bool b => b,
            null => false,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1",
            IConvertible c => System.Convert.ToDouble(c, System.Globalization.CultureInfo.InvariantCulture) != 0,
            _ => false
        };

        internal Task WriteDirectAsync(object value, CancellationToken token = default) => Write(value, null, token);

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            try
            {
                await _vacuum.ExecuteAsync(DataPoint, value, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{_vacuum.Did}: writing {DataPoint.Key} failed: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _vacuum.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
