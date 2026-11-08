using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.AspNetCore.WebUtilities;
using P3.Driver.SpotPrice.Driver;
using Xunit;

namespace P3.Driver.SpotPrice.Tests
{
    /// <summary>
    /// The driver against a fake aWATTar / ENTSO-E server, on 8 Nov 2026 in Vienna (CET, local midnight = 23:00 UTC)
    /// </summary>
    public class SpotPriceDriverTests : DriverFactoryTestBase<SpotPriceDriverFactory>, IAsyncLifetime
    {
        private static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");
        private static readonly DateTimeOffset LocalMidnight = new(2026, 11, 8, 0, 0, 0, TimeSpan.FromHours(1));

        // EUR/MWh per local hour of today; tomorrow is the same + 5
        private static readonly double[] Today =
        {
            90, 80, 60, 50, 55, 70, 100, 120, 130, 110, 95, 85,
            80, 82, 90, 105, 140, 160, 150, 130, 115, 100, 95, 92
        };

        private FakeHttpServer _server;
        private readonly HttpClient _http = new();
        private DateTimeOffset _now = LocalMidnight.AddMinutes(30);

        /// <summary>
        /// Prices are published until this instant (tomorrow arrives in the afternoon)
        /// </summary>
        private DateTimeOffset _publishedUntil = LocalMidnight.AddDays(2);

        private int _status = 200;

        public async Task InitializeAsync()
        {
            _server = await FakeHttpServer.StartAsync();
            _server.Handler = Handle;
        }

        public async Task DisposeAsync()
        {
            await _server.DisposeAsync();
            _http.Dispose();
        }

        private static double PriceAt(int hourFromMidnight) => Today[hourFromMidnight % 24] + (hourFromMidnight >= 24 ? 5 : 0);

        private (int, string) Handle(RecordedRequest request)
        {
            if (_status != 200)
            {
                return (_status, "");
            }
            if (request.Path == "/v1/marketdata")
            {
                var query = QueryHelpers.ParseQuery(request.Query);
                var start = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(query["start"]));
                var end = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(query["end"]));
                var data = new List<object>();
                for (var h = 0; h < 48; h++)
                {
                    var s = LocalMidnight.AddHours(h);
                    if (s >= start && s.AddHours(1) <= end && s.AddHours(1) <= _publishedUntil)
                    {
                        data.Add(new { start_timestamp = s.ToUnixTimeMilliseconds(), end_timestamp = s.AddHours(1).ToUnixTimeMilliseconds(), marketprice = PriceAt(h), unit = "Eur/MWh" });
                    }
                }
                return (200, JsonSerializer.Serialize(new { @object = "list", data, url = "/at/v1/marketdata" }));
            }
            if (request.Path == "/api")
            {
                return (200, SpotPriceSourceTests.EntsoeXml);
            }
            return (404, "");
        }

        private NodeInstance CreateRoot(SpotPriceProvider provider = SpotPriceProvider.AwattarAt, double surcharge = 0.1, double vat = 20, string token = "")
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty(SpotPriceDriverFactory.ProviderKey, (int)provider);
            node.SetProperty(SpotPriceDriverFactory.ApiUrlKey, provider == SpotPriceProvider.Entsoe ? _server.Url + "/api" : _server.Url);
            node.SetProperty(SpotPriceDriverFactory.TokenKey, token);
            node.SetProperty(SpotPriceDriverFactory.ZoneKey, "10YAT-APG------L");
            node.SetProperty(SpotPriceDriverFactory.SurchargeKey, surcharge);
            node.SetProperty(SpotPriceDriverFactory.VatKey, vat);
            node.SetProperty(SpotPriceDriverFactory.RefreshKey, 60);
            foreach (var dp in SpotPriceDataPoints.All)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(dp.Template));
            }
            return node;
        }

        private void AddWindow(NodeInstance root, double hours, double period = 24, bool contiguous = true, SpotWindowMode mode = SpotWindowMode.Cheapest)
        {
            var window = CreateNodeInstance(SpotPriceDriverFactory.WindowTemplate);
            window.SetProperty(SpotPriceDriverFactory.WindowHoursKey, hours);
            window.SetProperty(SpotPriceDriverFactory.WindowPeriodKey, period);
            window.SetProperty(SpotPriceDriverFactory.WindowContiguousKey, contiguous);
            window.SetProperty(SpotPriceDriverFactory.WindowModeKey, (int)mode);
            root.InverseThis2ParentNodeInstanceNavigation.Add(window);
        }

        private async Task<SpotPriceDriver> CreateSpotAsync(NodeInstance root)
        {
            var driver = (SpotPriceDriver)await CreateDriver(root);
            driver.Http = _http;
            driver.Clock = () => _now;
            driver.Zone = Vienna;
            await driver.Init();
            foreach (var window in driver.Windows)
            {
                await window.Init();
            }
            return driver;
        }

        private static object Value(SpotPriceDriver driver, string key) => driver.Values.Single(v => v.Key == key).LastValue;

        private static double Expected(double market, double surcharge = 0.1, double vat = 20) => Math.Round((market / 1000 + surcharge) * (1 + vat / 100), 5);

        [Fact]
        public async Task Awattar_RequestsTodayAndTomorrow_InLocalTime()
        {
            var driver = await CreateSpotAsync(CreateRoot());

            await driver.TickAsync();

            var request = Assert.Single(_server.Requests);
            var query = QueryHelpers.ParseQuery(request.Query);
            Assert.Equal(LocalMidnight.ToUnixTimeMilliseconds().ToString(), query["start"].ToString());
            Assert.Equal(LocalMidnight.AddDays(2).ToUnixTimeMilliseconds().ToString(), query["end"].ToString());
        }

        [Fact]
        public async Task Awattar_DispatchesThePrices_WithSurchargeAndVat()
        {
            var driver = await CreateSpotAsync(CreateRoot());

            await driver.TickAsync();

            Assert.Equal(true, Value(driver, "connected"));
            Assert.Equal(Expected(90), Value(driver, "price_now"));
            Assert.Equal(0.09, Value(driver, "market_price_now"));
            Assert.Equal(Expected(80), Value(driver, "price_next_hour"));
            Assert.Equal(Expected(50), Value(driver, "price_min_today"));
            Assert.Equal(Expected(160), Value(driver, "price_max_today"));
            Assert.Equal(Expected(Today.Average()), Value(driver, "price_avg_today"));
            Assert.Equal(new DateTime(2026, 11, 10, 0, 0, 0), Value(driver, "prices_until"));
        }

        [Fact]
        public async Task PricesFollowTheClock()
        {
            var driver = await CreateSpotAsync(CreateRoot(surcharge: 0, vat: 0));
            await driver.TickAsync();

            _now = LocalMidnight.AddHours(17).AddMinutes(10);
            driver.Evaluate(_now);

            Assert.Equal(0.16, Value(driver, "price_now"));
            Assert.Equal(0.15, Value(driver, "price_next_hour"));
        }

        [Fact]
        public async Task Fetch_IsNotRepeated_WhileThePricesReachFarEnough()
        {
            var driver = await CreateSpotAsync(CreateRoot());
            await driver.TickAsync();

            _now = _now.AddMinutes(30);
            await driver.TickAsync();
            Assert.Single(_server.Requests);

            _now = _now.AddMinutes(30);
            await driver.TickAsync();
            Assert.Equal(2, _server.Requests.Count);
        }

        [Fact]
        public async Task Fetch_TomorrowMissing_AsksEveryQuarterHour()
        {
            _now = LocalMidnight.AddHours(13);
            _publishedUntil = LocalMidnight.AddDays(1);
            var driver = await CreateSpotAsync(CreateRoot());
            await driver.TickAsync();

            _now = _now.AddMinutes(10);
            await driver.TickAsync();
            Assert.Single(_server.Requests);

            _publishedUntil = LocalMidnight.AddDays(2);
            _now = _now.AddMinutes(5);
            await driver.TickAsync();
            Assert.Equal(2, _server.Requests.Count);
            Assert.Equal(new DateTime(2026, 11, 10, 0, 0, 0), Value(driver, "prices_until"));

            // complete now: back to the refresh interval
            _now = _now.AddMinutes(15);
            await driver.TickAsync();
            Assert.Equal(2, _server.Requests.Count);
        }

        [Fact]
        public async Task Fetch_Failure_IsNotConnected_KeepsThePrices_AndRetries()
        {
            var driver = await CreateSpotAsync(CreateRoot());
            await driver.TickAsync();

            _status = 503;
            _now = _now.AddHours(1);
            await driver.TickAsync();
            Assert.Equal(false, Value(driver, "connected"));
            Assert.Equal(Expected(80), Value(driver, "price_now"));

            _status = 200;
            _now = _now.AddMinutes(15);
            await driver.TickAsync();
            Assert.Equal(true, Value(driver, "connected"));
            Assert.Equal(3, _server.Requests.Count);
        }

        [Fact]
        public async Task Entsoe_SendsTheA44Query()
        {
            var driver = await CreateSpotAsync(CreateRoot(SpotPriceProvider.Entsoe, token: "tok"));

            await driver.TickAsync();

            var query = QueryHelpers.ParseQuery(Assert.Single(_server.Requests).Query);
            Assert.Equal("tok", query["securityToken"].ToString());
            Assert.Equal("A44", query["documentType"].ToString());
            Assert.Equal("A01", query["contract_MarketAgreement.type"].ToString());
            Assert.Equal("10YAT-APG------L", query["in_Domain"].ToString());
            Assert.Equal("10YAT-APG------L", query["out_Domain"].ToString());
            Assert.Equal("202611072300", query["periodStart"].ToString());
            Assert.Equal("202611092300", query["periodEnd"].ToString());
            Assert.Equal(true, Value(driver, "connected"));
        }

        [Fact]
        public async Task Window_IsOnDuringTheCheapestHours()
        {
            var root = CreateRoot();
            AddWindow(root, 3);
            var driver = await CreateSpotAsync(root);
            await driver.TickAsync();
            var window = Assert.Single(driver.Windows);

            Assert.Equal(false, window.LastValue);
            var plan = Assert.Single(window.Planner.Plan);
            Assert.Equal(LocalMidnight.AddHours(2), plan.Start);
            Assert.Equal(LocalMidnight.AddHours(5), plan.End);

            driver.Evaluate(LocalMidnight.AddHours(2));
            Assert.Equal(true, window.LastValue);
            driver.Evaluate(LocalMidnight.AddHours(5).AddSeconds(-1));
            Assert.Equal(true, window.LastValue);
            driver.Evaluate(LocalMidnight.AddHours(5));
            Assert.Equal(false, window.LastValue);
        }

        [Fact]
        public async Task Window_MostExpensiveSingleHours()
        {
            var root = CreateRoot();
            AddWindow(root, 2, period: 24, contiguous: false, mode: SpotWindowMode.MostExpensive);
            var driver = await CreateSpotAsync(root);
            await driver.TickAsync();

            var plan = Assert.Single(driver.Windows).Planner.Plan;
            // 17:00 (160) and 18:00 (150) of today are higher than any hour up to 00:30 tomorrow
            Assert.Equal(new[] { (LocalMidnight.AddHours(17), LocalMidnight.AddHours(19)) }, plan.Select(i => (i.Start, i.End)));
        }

        [Fact]
        public async Task Template_HasTheValuesAndTheWindow()
        {
            var root = CreateRoot();
            AddWindow(root, 1);
            AddWindow(root, 2);
            var driver = await CreateSpotAsync(root);

            Assert.Equal(SpotPriceDataPoints.All.Count, driver.Values.Count);
            Assert.Equal(2, driver.Windows.Count);
        }
    }
}
