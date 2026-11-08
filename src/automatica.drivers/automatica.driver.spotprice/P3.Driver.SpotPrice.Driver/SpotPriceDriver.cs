using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.SpotPrice.Driver
{
    /// <summary>
    /// Day-ahead electricity prices. Fetches today and tomorrow, then evaluates the current values and the windows every few seconds.
    /// </summary>
    internal sealed class SpotPriceDriver : DriverNoneAttributeBase
    {
        private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

        internal static readonly TimeSpan EvaluateInterval = TimeSpan.FromSeconds(15);

        /// <summary>
        /// While tomorrow's prices are missing (published around 13:00-14:00), the fetch is repeated this often
        /// </summary>
        internal static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Prices are fetched early when they do not reach this far ahead
        /// </summary>
        internal static readonly TimeSpan WantedAhead = TimeSpan.FromHours(12);

        private readonly List<SpotValueNode> _values = new();
        private readonly List<SpotWindowNode> _windows = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private Timer _timer;
        private TimeSpan _refresh;
        private DateTimeOffset? _lastFetch;
        private bool _lastFetchFailed;

        internal HttpClient Http { get; set; } = SharedHttp;
        internal Func<DateTimeOffset> Clock { get; set; }
        internal TimeZoneInfo Zone { get; set; }

        internal ISpotPriceSource Source { get; private set; }
        internal SpotPriceProvider Provider { get; private set; }
        internal double Surcharge { get; private set; }
        internal double Vat { get; private set; }
        internal SpotPriceSchedule Schedule { get; private set; } = SpotPriceSchedule.Empty;
        internal long Version { get; private set; }
        internal IReadOnlyList<SpotValueNode> Values => _values;
        internal IReadOnlyList<SpotWindowNode> Windows => _windows;

        public SpotPriceDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        internal DateTimeOffset Now() => Clock?.Invoke() ?? DriverContext.TimeProvider.GetUtcNow();

        private TimeZoneInfo LocalZone => Zone ?? DriverContext.TimeProvider.LocalTimeZone;

        public override Task<bool> Init(CancellationToken token = default)
        {
            Provider = (SpotPriceProvider)Math.Clamp(GetPropertyValueInt(SpotPriceDriverFactory.ProviderKey), 0, 2);
            Surcharge = GetPropertyValueDouble(SpotPriceDriverFactory.SurchargeKey);
            Vat = Math.Clamp(GetPropertyValueDouble(SpotPriceDriverFactory.VatKey), 0, 100);
            _refresh = TimeSpan.FromMinutes(Math.Clamp(GetPropertyValueInt(SpotPriceDriverFactory.RefreshKey), 15, 1440));

            var url = GetPropertyValueString(SpotPriceDriverFactory.ApiUrlKey)?.Trim();
            Source = Provider switch
            {
                SpotPriceProvider.Entsoe => new EntsoeSource(Http, string.IsNullOrEmpty(url) ? EntsoeSource.DefaultUrl : url,
                    GetPropertyValueString(SpotPriceDriverFactory.TokenKey), GetPropertyValueString(SpotPriceDriverFactory.ZoneKey)),
                SpotPriceProvider.AwattarDe => new AwattarSource(Http, string.IsNullOrEmpty(url) ? AwattarSource.DefaultDeUrl : url),
                _ => new AwattarSource(Http, string.IsNullOrEmpty(url) ? AwattarSource.DefaultAtUrl : url)
            };
            _lastFetch = null;
            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            await TickAsync(token);
            _timer = new Timer(TimerElapsed, null, EvaluateInterval, EvaluateInterval);
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
                await TickAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Spot price update failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await TickAsync(token, forceFetch: true);
            return true;
        }

        /// <summary>
        /// Fetches when due, then updates all values and windows
        /// </summary>
        internal async Task TickAsync(CancellationToken token = default, bool forceFetch = false)
        {
            await _lock.WaitAsync(token);
            try
            {
                var now = Now();
                if (forceFetch || FetchDue(now))
                {
                    await FetchAsync(now, token);
                }
                Evaluate(now);
            }
            finally
            {
                _lock.Release();
            }
        }

        internal bool FetchDue(DateTimeOffset now)
        {
            if (!_lastFetch.HasValue)
            {
                return true;
            }
            var since = now - _lastFetch.Value;
            if (since >= _refresh)
            {
                return true;
            }
            var shortOfData = !Schedule.End.HasValue || Schedule.End.Value < now + WantedAhead;
            return (shortOfData || _lastFetchFailed) && since >= RetryInterval;
        }

        private async Task FetchAsync(DateTimeOffset now, CancellationToken token)
        {
            _lastFetch = now;
            var today = LocalMidnight(now, 0);
            try
            {
                var slots = await Source.FetchAsync(today, LocalMidnight(now, 2), token);
                var schedule = new SpotPriceSchedule(slots);
                if (!schedule.Slots.SequenceEqual(Schedule.Slots))
                {
                    Schedule = schedule;
                    Version++;
                }
                _lastFetchFailed = false;
                SetValue(SpotPriceDataPoints.ConnectedKey, true);
            }
            catch (Exception ex) when (ex is SpotPriceException or HttpRequestException or JsonException or TaskCanceledException && !token.IsCancellationRequested)
            {
                _lastFetchFailed = true;
                SetValue(SpotPriceDataPoints.ConnectedKey, false);
                DriverContext.Logger.LogError($"Spot prices ({Provider}) could not be fetched: {ex.Message}");
            }
        }

        /// <summary>
        /// Local midnight <paramref name="days"/> days after today, as an instant (DST safe)
        /// </summary>
        internal DateTimeOffset LocalMidnight(DateTimeOffset now, int days)
        {
            var zone = LocalZone;
            var date = TimeZoneInfo.ConvertTime(now, zone).Date.AddDays(days);
            return new DateTimeOffset(date, zone.GetUtcOffset(date));
        }

        /// <summary>
        /// End-user price in EUR/kWh: (market + surcharge) * (1 + VAT)
        /// </summary>
        internal double Price(double marketEurPerMwh) => Math.Round((marketEurPerMwh / 1000 + Surcharge) * (1 + Vat / 100), 5);

        private object Price(double? market) => market.HasValue ? Price(market.Value) : null;

        internal void Evaluate(DateTimeOffset now)
        {
            var schedule = Schedule;
            var slot = schedule.At(now);
            SetValue("price_now", slot == null ? null : Price(slot.Market));
            SetValue("market_price_now", slot == null ? null : Math.Round(slot.Market / 1000, 5));

            var local = TimeZoneInfo.ConvertTime(now, LocalZone);
            var nextHour = now + TimeSpan.FromMinutes(60 - local.Minute) - TimeSpan.FromSeconds(local.Second) - TimeSpan.FromMilliseconds(local.Millisecond);
            SetValue("price_next_hour", schedule.At(nextHour) == null ? null : Price(schedule.Average(nextHour, nextHour.AddHours(1))));

            var today = schedule.Between(LocalMidnight(now, 0), LocalMidnight(now, 1)).ToList();
            SetValue("price_min_today", today.Count == 0 ? null : Price(today.Min(s => s.Market)));
            SetValue("price_max_today", today.Count == 0 ? null : Price(today.Max(s => s.Market)));
            SetValue("price_avg_today", today.Count == 0 ? null : Price(schedule.Average(LocalMidnight(now, 0), LocalMidnight(now, 1))));
            SetValue("prices_until", schedule.End.HasValue ? TimeZoneInfo.ConvertTime(schedule.End.Value, LocalZone).DateTime : null);

            foreach (var window in _windows)
            {
                window.Evaluate(schedule, Version, now);
            }
        }

        private void SetValue(string key, object value)
        {
            if (value == null)
            {
                return;
            }
            foreach (var node in _values.Where(v => v.Key == key))
            {
                node.Set(value);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var key = ctx.NodeInstance.This2NodeTemplateNavigation?.Key;
            if (key == SpotPriceDriverFactory.WindowKey)
            {
                var window = new SpotWindowNode(ctx, this);
                _windows.Add(window);
                return window;
            }
            if (SpotPriceDataPoints.All.Any(d => d.Key == key))
            {
                var node = new SpotValueNode(ctx, key);
                _values.Add(node);
                return node;
            }
            return null;
        }
    }

    internal sealed class SpotValueNode : DriverBase
    {
        private readonly object _lock = new();
        private bool _hasValue;

        public SpotValueNode(IDriverContext driverContext, string key) : base(driverContext)
        {
            Key = key;
        }

        public string Key { get; }

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

        // values are refreshed by the driver's timer
        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => Task.FromResult(true);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }

    public enum SpotWindowMode
    {
        [EnumName("SPOT_PRICE.WINDOW.MODE.CHEAPEST")]
        Cheapest = 0,
        [EnumName("SPOT_PRICE.WINDOW.MODE.MOST_EXPENSIVE")]
        MostExpensive = 1
    }

    /// <summary>
    /// true while the current time is inside the planned cheapest (or most expensive) hours
    /// </summary>
    internal sealed class SpotWindowNode : DriverBase
    {
        private readonly SpotPriceDriver _driver;
        private bool? _last;

        public SpotWindowNode(IDriverContext driverContext, SpotPriceDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        internal PriceWindowPlanner Planner { get; } = new();

        public bool? LastValue => _last;

        public override Task<bool> Init(CancellationToken token = default)
        {
            var hours = GetPropertyValueDouble(SpotPriceDriverFactory.WindowHoursKey);
            var period = GetPropertyValueDouble(SpotPriceDriverFactory.WindowPeriodKey);
            Planner.Duration = TimeSpan.FromHours(Math.Clamp(double.IsNaN(hours) ? 3 : hours, 0.25, 48));
            Planner.Period = TimeSpan.FromHours(Math.Clamp(double.IsNaN(period) || period <= 0 ? 24 : period, 1, 48));
            if (Planner.Period < Planner.Duration)
            {
                Planner.Period = Planner.Duration;
            }
            Planner.Contiguous = GetPropertyValueBool(SpotPriceDriverFactory.WindowContiguousKey) ?? true;
            Planner.MostExpensive = GetPropertyValueInt(SpotPriceDriverFactory.WindowModeKey) == (int)SpotWindowMode.MostExpensive;
            Planner.Reset();
            _last = null;
            return base.Init(token);
        }

        public void Evaluate(SpotPriceSchedule schedule, long version, DateTimeOffset now)
        {
            var hadPlan = Planner.Plan;
            var active = Planner.Evaluate(schedule, version, now);
            if (!ReferenceEquals(hadPlan, Planner.Plan) && Planner.Plan != null)
            {
                DriverContext.Logger.LogInformation($"{FullName}: {string.Join(", ", Planner.Plan.Select(i => $"{i.Start:u} - {i.End:u}"))}");
            }
            if (_last == active)
            {
                return;
            }
            _last = active;
            DispatchRead(active);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default) => Task.CompletedTask;

        // values are refreshed by the driver's timer
        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default) => Task.FromResult(true);

        public override IDriverNode CreateDriverNode(IDriverContext ctx) => null;
    }
}
