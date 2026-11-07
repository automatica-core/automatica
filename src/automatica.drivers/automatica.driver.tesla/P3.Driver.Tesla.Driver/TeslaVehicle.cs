using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Tesla.Driver
{
    internal sealed class TeslaVehicle : DriverNoneAttributeBase
    {
        private readonly TeslaDriver _account;
        private readonly List<TeslaValueNode> _values = new();

        public TeslaVehicle(IDriverContext driverContext, TeslaDriver account) : base(driverContext)
        {
            _account = account;
        }

        internal string Vin { get; private set; }

        internal string VehicleState { get; private set; }

        internal IReadOnlyList<TeslaValueNode> Values => _values;

        public override Task<bool> Init(CancellationToken token = default)
        {
            Vin = GetPropertyValueString(TeslaDriverFactory.VinKey)?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(Vin))
            {
                DriverContext.Logger.LogError($"{FullName}: no VIN configured");
            }
            return base.Init(token);
        }

        /// <summary>
        /// Reads the vehicle state, and the vehicle data when the vehicle is online
        /// </summary>
        internal async Task RefreshAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(Vin))
            {
                return;
            }

            SetState(await _account.Api.GetVehicleStateAsync(Vin, token));
            if (VehicleState != "online")
            {
                return;
            }

            try
            {
                using var data = await _account.Api.GetVehicleDataAsync(Vin, token);
                if (data.RootElement.TryGetProperty("response", out var response) && response.ValueKind == JsonValueKind.Object)
                {
                    Apply(response);
                }
            }
            catch (VehicleUnavailableException)
            {
                // fell asleep between the two requests
                SetState("asleep");
            }
        }

        internal void Apply(JsonElement response)
        {
            if (response.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String)
            {
                SetState(state.GetString());
            }
            foreach (var value in _values.Where(v => v.DataPoint.Read != null))
            {
                var read = value.DataPoint.Read(response);
                if (read != null)
                {
                    value.Set(read);
                }
            }
        }

        private void SetState(string state)
        {
            if (state == null)
            {
                return;
            }
            VehicleState = state;
            foreach (var value in _values.Where(v => v.DataPoint.Key == TeslaDataPoints.StateKey))
            {
                value.Set(state);
            }
        }

        /// <summary>
        /// Wakes the vehicle if needed, sends the command and reads the vehicle again
        /// </summary>
        internal async Task ExecuteAsync(TeslaDataPoint dataPoint, object value, CancellationToken token = default)
        {
            var (command, parameters) = dataPoint.Write(value);
            if (dataPoint.Key != TeslaDataPoints.WakeKey && command == null)
            {
                return; // a momentary button released
            }

            await EnsureAwakeAsync(token);
            if (command != null)
            {
                DriverContext.Logger.LogInformation($"{Vin}: {command}");
                await _account.Api.CommandAsync(Vin, command, parameters, token);
            }

            await RefreshAsync(token);
        }

        private async Task EnsureAwakeAsync(CancellationToken token)
        {
            var state = await _account.Api.GetVehicleStateAsync(Vin, token);
            SetState(state);
            if (state == "online")
            {
                return;
            }

            await _account.Api.WakeUpAsync(Vin, token);
            var deadline = DateTime.UtcNow + _account.WakeTimeout;
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(_account.WakePollInterval, token);
                state = await _account.Api.GetVehicleStateAsync(Vin, token);
                SetState(state);
                if (state == "online")
                {
                    return;
                }
            }
            throw new TeslaApiException($"{Vin} did not wake up within {_account.WakeTimeout.TotalSeconds}s");
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await RefreshAsync(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = TeslaDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new TeslaValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class TeslaValueNode : DriverBase
    {
        private readonly TeslaVehicle _vehicle;
        private readonly object _lock = new();
        private bool _hasValue;

        public TeslaValueNode(IDriverContext driverContext, TeslaVehicle vehicle, TeslaDataPoint dataPoint) : base(driverContext)
        {
            _vehicle = vehicle;
            DataPoint = dataPoint;
        }

        public TeslaDataPoint DataPoint { get; }

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
        /// The write path without the dispatcher queue (tests)
        /// </summary>
        internal Task WriteDirectAsync(object value, CancellationToken token = default) => Write(value, null, token);

        protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.IsWriteable)
            {
                return;
            }
            try
            {
                await _vehicle.ExecuteAsync(DataPoint, value, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"{_vehicle.Vin}: writing {DataPoint.Key} failed: {ex.Message}");
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return _vehicle.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
