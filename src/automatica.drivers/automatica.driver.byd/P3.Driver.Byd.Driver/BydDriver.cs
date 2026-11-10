using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Byd.Driver
{
    internal sealed class BydDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly List<BydVehicle> _vehicles = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;
        private Dictionary<string, BydVehicleInfo> _infos;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal string BaseUrlOverride { get; set; }
        internal TimeSpan? PollIntervalOverride { get; set; }
        internal BydCloud Cloud { get; private set; }
        internal IReadOnlyList<BydVehicle> Vehicles => _vehicles;
        internal string Pin { get; private set; }

        public BydDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(120, GetPropertyValueInt(BydDriverFactory.PollKey)));
            Pin = GetPropertyValueString(BydDriverFactory.PinKey);
            Cloud = new BydCloud(Http, DriverContext.Logger, () => DriverContext.TimeProvider.GetUtcNow(),
                BaseUrlOverride ?? GetPropertyValueString(BydDriverFactory.ApiUrlKey),
                GetPropertyValueString(BydDriverFactory.UsernameKey), GetPropertyValueString(BydDriverFactory.PasswordKey),
                GetPropertyValueString(BydDriverFactory.CountryKey), "en");
            if (PollIntervalOverride.HasValue)
            {
                Cloud.PollInterval = PollIntervalOverride.Value;
            }
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
                DriverContext.Logger.LogError(ex, "First BYD poll failed, retrying with the next poll");
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
                DriverContext.Logger.LogError(ex, "BYD poll failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollAsync(token);
            return true;
        }

        /// <summary>
        /// Model data of the car from the vehicle list of the account. The list is only needed for the energy type, so a failing
        /// list (e.g. code 1007) does not stop the car from being read: the realtime request then goes out as an electric car, like pyBYD.
        /// A failed list is asked again with the next poll.
        /// </summary>
        internal async Task<BydVehicleInfo> InfoAsync(string vin, CancellationToken token)
        {
            if (_infos == null)
            {
                try
                {
                    _infos = (await Cloud.ListVehiclesAsync(token))
                        .GroupBy(v => v.Vin, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                }
                catch (BydApiException ex)
                {
                    DriverContext.Logger.LogWarning($"Could not list the cars of the BYD account ({ex.Message}), reading {vin} as an electric car");
                    return null;
                }
            }
            return _infos.TryGetValue(vin, out var info) ? info : null;
        }

        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                foreach (var vehicle in _vehicles)
                {
                    try
                    {
                        await vehicle.RefreshAsync(token);
                    }
                    catch (Exception ex) when (ex is BydApiException or HttpRequestException or JsonException or FormatException or System.Security.Cryptography.CryptographicException)
                    {
                        DriverContext.Logger.LogError($"Could not read BYD {vehicle.Vin}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _pollLock.Release();
            }
        }

        /// <summary>
        /// Lists the cars of the account that are not configured yet
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var known = _vehicles.Select(v => v.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var result = new List<NodeInstance>();
            foreach (var info in await Cloud.ListVehiclesAsync(token))
            {
                if (known.Contains(info.Vin))
                {
                    continue;
                }
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(BydDriverFactory.VehicleTemplate);
                node.Name = !string.IsNullOrWhiteSpace(info.Alias) ? info.Alias : !string.IsNullOrWhiteSpace(info.ModelName) ? $"{info.ModelName} ({info.Vin})" : info.Vin;
                node.SetProperty(BydDriverFactory.VinKey, info.Vin);
                foreach (var dp in BydDataPoints.All)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(dp.Template));
                }
                result.Add(node);
            }
            return result;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation?.Key != "vehicle")
            {
                return null;
            }
            var vehicle = new BydVehicle(ctx, this);
            _vehicles.Add(vehicle);
            return vehicle;
        }
    }

    internal sealed class BydVehicle : DriverNoneAttributeBase
    {
        private readonly BydDriver _account;
        private readonly List<BydValueNode> _values = new();
        private double _climateTemperature;
        private int _climateMinutes;

        public BydVehicle(IDriverContext driverContext, BydDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string Vin { get; private set; }
        internal IReadOnlyList<BydValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            Vin = GetPropertyValueString(BydDriverFactory.VinKey)?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(Vin))
            {
                DriverContext.Logger.LogError($"{FullName}: no VIN configured, use the scan of the account");
            }
            var temperature = GetPropertyValueInt(BydDriverFactory.ClimateTemperatureKey);
            _climateTemperature = temperature is >= 15 and <= 31 ? temperature : 21;
            var minutes = GetPropertyValueInt(BydDriverFactory.ClimateDurationKey);
            _climateMinutes = minutes is >= 10 and <= 30 ? minutes : 10;
            return base.Init(token);
        }

        private void Apply(BydSource source, JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Object)
            {
                return;
            }
            foreach (var node in _values.Where(v => v.DataPoint.Source == source && v.DataPoint.IsReadable))
            {
                var value = node.DataPoint.Read(data);
                if (value != null)
                {
                    node.Set(value);
                }
            }
        }

        internal async Task RefreshAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Vin))
            {
                return;
            }
            if (_values.Any(v => v.DataPoint.Source == BydSource.Realtime))
            {
                var info = await _account.InfoAsync(Vin, token);
                Apply(BydSource.Realtime, await _account.Cloud.GetRealtimeAsync(Vin, info?.EnergyType, token));
            }
            if (_values.Any(v => v.DataPoint.Source == BydSource.Gps))
            {
                try
                {
                    Apply(BydSource.Gps, await _account.Cloud.GetGpsAsync(Vin, token));
                }
                catch (BydApiException ex) when (ex.Code == "6051")
                {
                    DriverContext.Logger.LogDebug($"{Vin}: no GPS fix");
                }
            }
        }

        internal async Task ExecuteAsync(BydValueNode node, bool on, CancellationToken token = default)
        {
            var command = node.DataPoint.Command(on);
            if (command == null || string.IsNullOrWhiteSpace(Vin))
            {
                return;
            }
            var parameters = command == "OPENAIR" ? BydCloud.ClimateParams(_climateTemperature, _climateMinutes) : null;
            DriverContext.Logger.LogInformation($"{Vin}: {command}");
            await _account.Cloud.RemoteControlAsync(Vin, command, _account.Pin, parameters, token);

            if (node.DataPoint.Key is "locked" or "climate")
            {
                node.Set(on);
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await RefreshAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = BydDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new BydValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class BydValueNode : DriverBase
    {
        private readonly BydVehicle _vehicle;
        private readonly object _lock = new();

        public BydValueNode(IDriverContext driverContext, BydVehicle vehicle, BydDataPoint dataPoint) : base(driverContext)
        {
            _vehicle = vehicle;
            DataPoint = dataPoint;
        }

        public BydDataPoint DataPoint { get; }

        public object LastValue { get; private set; }

        /// <summary>
        /// Dispatches the value on every poll, also when it did not change: logics linked later (e.g. a formula over
        /// latitude/longitude of a parked car) only get a value when it is dispatched again
        /// </summary>
        public void Set(object value)
        {
            lock (_lock)
            {
                LastValue = value;
            }
            DispatchRead(value);
        }

        internal static bool ToBool(object value) => value switch
        {
            bool b => b,
            null => false,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1",
            IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture) != 0,
            _ => false
        };

        internal Task WriteDirectAsync(object value, CancellationToken token = default) => Write(value, null, token);

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.IsWritable)
            {
                return;
            }
            try
            {
                await _vehicle.ExecuteAsync(this, ToBool(value), token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{_vehicle.Vin}: {DataPoint.Key} failed: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => _vehicle.Read(token);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
