using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Powerwall.Driver
{
    /// <summary>
    /// A Tesla Backup Gateway 2 on the LAN. Polls the meters, SoC, grid status and operation settings.
    /// </summary>
    internal sealed class PowerwallDriver : DriverNoneAttributeBase
    {
        private readonly List<PowerwallValueNode> _values = new();
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private Timer _timer;
        private TimeSpan _poll;
        private HttpClient _ownedHttp;
        private bool _operationMissing;

        internal HttpClient Http { get; set; }
        internal PowerwallApi Api { get; private set; }
        internal IReadOnlyList<PowerwallValueNode> Values => _values;
        internal bool AllowWrites { get; private set; }

        public PowerwallDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Clamp(GetPropertyValueInt(PowerwallDriverFactory.PollKey), 2, 3600));
            AllowWrites = GetPropertyValueBool(PowerwallDriverFactory.AllowWritesKey) ?? false;
            _operationMissing = false;

            var http = Http;
            if (http == null)
            {
                _ownedHttp?.Dispose();
                var handler = PowerwallApi.CreateHandler(() => GetPropertyValueString(PowerwallDriverFactory.FingerprintKey), SavePin);
                _ownedHttp = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
                http = _ownedHttp;
            }

            var host = GetPropertyValueString(PowerwallDriverFactory.HostKey);
            Api = new PowerwallApi(http, host, GetPropertyValueString(PowerwallDriverFactory.PasswordKey), GetPropertyValueString(PowerwallDriverFactory.EmailKey));
            if (string.IsNullOrWhiteSpace(host))
            {
                DriverContext.Logger.LogError("No Powerwall gateway address configured");
            }
            return base.Init(token);
        }

        /// <summary>
        /// Stores the certificate fingerprint seen on the first connection
        /// </summary>
        private void SavePin(string fingerprint)
        {
            var property = GetProperty(PowerwallDriverFactory.FingerprintKey);
            if (property == null || !string.IsNullOrWhiteSpace(Convert.ToString(property.Value)))
            {
                return;
            }
            property.Value = fingerprint;
            DriverContext.NodeTemplateFactory.SetPropertyValue(property.ObjId, fingerprint);
            DriverContext.Logger.LogInformation($"Pinned the Powerwall gateway certificate {fingerprint}");
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError($"Powerwall poll failed: {ex.Message}");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await PollSafeAsync(token);
            return true;
        }

        /// <summary>
        /// Reads all endpoints and dispatches the values that changed. The meters are required, the other endpoints are optional
        /// (a Powerwall 3 has no /api/operation).
        /// </summary>
        internal async Task PollAsync(CancellationToken token = default)
        {
            await _pollLock.WaitAsync(token);
            try
            {
                JsonDocument aggregates;
                try
                {
                    aggregates = await Api.GetAsync("/api/meters/aggregates", token);
                }
                catch (Exception ex) when (ex is PowerwallApiException or HttpRequestException or JsonException or TaskCanceledException && !token.IsCancellationRequested)
                {
                    SetConnected(false);
                    throw;
                }

                using (aggregates)
                using (var soe = await OptionalAsync("/api/system_status/soe", token))
                using (var grid = await OptionalAsync("/api/system_status/grid_status", token))
                using (var operation = _operationMissing ? null : await OptionalAsync("/api/operation", token, missing: () => _operationMissing = true))
                {
                    Apply(new PowerwallSnapshot
                    {
                        Aggregates = aggregates.RootElement,
                        Soe = soe?.RootElement,
                        GridStatus = grid?.RootElement,
                        Operation = operation?.RootElement
                    });
                }
                SetConnected(true);
            }
            finally
            {
                _pollLock.Release();
            }
        }

        private async Task<JsonDocument> OptionalAsync(string path, CancellationToken token, Action missing = null)
        {
            try
            {
                return await Api.GetAsync(path, token);
            }
            catch (PowerwallApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                missing?.Invoke();
                return null;
            }
            catch (Exception ex) when (ex is PowerwallApiException or JsonException)
            {
                DriverContext.Logger.LogWarning($"Powerwall: {path}: {ex.Message}");
                return null;
            }
        }

        internal void Apply(PowerwallSnapshot snapshot)
        {
            foreach (var value in _values.Where(v => v.DataPoint.Read != null))
            {
                var read = value.DataPoint.Read(snapshot);
                if (read != null)
                {
                    value.Set(read);
                }
            }
        }

        private void SetConnected(bool connected)
        {
            foreach (var value in _values.Where(v => v.DataPoint.Key == PowerwallDataPoints.ConnectedKey))
            {
                value.Set(connected);
            }
        }

        /// <summary>
        /// Changes the operation mode or the backup reserve. The local endpoint overwrites both, so the current values are read first.
        /// </summary>
        internal async Task WriteOperationAsync(PowerwallDataPoint dataPoint, object value, CancellationToken token = default)
        {
            if (!AllowWrites)
            {
                DriverContext.Logger.LogWarning($"Powerwall: writing {dataPoint.Key} is switched off in the driver settings");
                await PollSafeAsync(token);
                return;
            }

            string mode = null;
            double? reserve = null;
            if (dataPoint.Key == PowerwallDataPoints.ModeKey)
            {
                mode = PowerwallDataPoints.Modes.FirstOrDefault(m => string.Equals(m, Convert.ToString(value)?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (mode == null)
                {
                    DriverContext.Logger.LogError($"Powerwall: unknown operation mode '{value}', use one of {string.Join(", ", PowerwallDataPoints.Modes)}");
                    return;
                }
            }
            else
            {
                var percent = PowerwallDataPoints.ToDouble(value);
                if (double.IsNaN(percent))
                {
                    DriverContext.Logger.LogError($"Powerwall: invalid backup reserve '{value}'");
                    return;
                }
                reserve = PowerwallDataPoints.RawPercent(percent);
            }

            try
            {
                using (var current = await Api.GetAsync("/api/operation", token))
                {
                    mode ??= current.RootElement.TryGetProperty("real_mode", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                    reserve ??= PowerwallDataPoints.Number(current.RootElement, "backup_reserve_percent");
                }
                if (mode == null || !reserve.HasValue)
                {
                    DriverContext.Logger.LogError("Powerwall: the gateway did not report its current operation settings, nothing written");
                    return;
                }

                DriverContext.Logger.LogInformation($"Powerwall: operation {mode}, reserve {reserve.Value} (gateway scale)");
                using (await Api.PostAsync("/api/operation", new { real_mode = mode, backup_reserve_percent = reserve.Value }, token))
                {
                }
                // older firmware applies the change only after this call, newer firmware does not know it
                try
                {
                    using (await Api.GetAsync("/api/config/completed", token))
                    {
                    }
                }
                catch (PowerwallApiException)
                {
                }
            }
            catch (PowerwallApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                DriverContext.Logger.LogError("Powerwall: the gateway refuses operation changes for the customer login (current firmware). Change mode and reserve in the Tesla app.");
            }
            catch (Exception ex) when (ex is PowerwallApiException or HttpRequestException or JsonException)
            {
                DriverContext.Logger.LogError($"Powerwall: writing {dataPoint.Key} failed: {ex.Message}");
            }

            await PollSafeAsync(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            var dataPoint = PowerwallDataPoints.All.FirstOrDefault(d => d.Key == key);
            if (dataPoint == null)
            {
                return null;
            }
            var node = new PowerwallValueNode(ctx, this, dataPoint);
            _values.Add(node);
            return node;
        }
    }

    internal sealed class PowerwallValueNode : DriverBase
    {
        private readonly PowerwallDriver _gateway;
        private readonly object _lock = new();
        private bool _hasValue;

        public PowerwallValueNode(IDriverContext driverContext, PowerwallDriver gateway, PowerwallDataPoint dataPoint) : base(driverContext)
        {
            _gateway = gateway;
            DataPoint = dataPoint;
        }

        public PowerwallDataPoint DataPoint { get; }

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

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            if (!DataPoint.IsWriteable)
            {
                return Task.CompletedTask;
            }
            // the poll after the write sends the gateway's value again, also when the write was refused
            lock (_lock)
            {
                _hasValue = false;
            }
            return _gateway.WriteOperationAsync(DataPoint, value, token);
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return _gateway.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
