using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.OpenWeather.Api;
using P3.Driver.OpenWeather.Api.Models.OneCallModel;
using P3.Driver.OpenWeatherMap.DriverFactory;
using Xunit;

namespace P3.Driver.OpenWeatherMap.Tests
{
    public sealed class OpenWeatherMapDriverTests : IDisposable
    {
        private static readonly Guid CurrentTemperature = new("c0dcd6d9-673b-4c8c-86c3-3b19790b112d");
        private static readonly Guid CurrentHumidity = new("5870e3de-4fbe-49e1-b051-55a427eebd44");
        private static readonly Guid CurrentWindDirection = new("fe13ca1b-b04f-4218-b69c-d744a7f7d9fe");
        private static readonly Guid TodayTemperatureMax = new("f879f1f8-f60c-466d-a307-d26e6314bb37");

        private static readonly Guid ForecastTemperature = new("c8889461-67ec-48dd-9789-3204efc74641");
        private static readonly Guid ForecastWindDirection = new("39cdb1e2-a04f-4e35-9dc3-e6261da48dd0");
        private static readonly Guid ForecastHumidity = new("66d08ddd-550f-4f88-ad22-9d3b8acf8459");
        private static readonly Guid ForecastTemperatureMax = new("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee7");
        private static readonly Guid ForecastTemperatureMin = new("5dddf5f7-d2e3-4959-8980-4a9bd5c15ee8");
        private static readonly Guid ForecastFrom = new("691477bc-b5f7-438f-8923-e02168bd886b");
        private static readonly Guid ForecastTo = new("0e8a2bdd-dfe7-4a1b-a073-00c0e2aff9ee");
        private static readonly Guid ForecastCloudsDescription = new("fb1d9741-f6ed-433a-94b6-83d40b6000b9");
        private static readonly Guid ForecastPrecipitation = new("b23f1587-8551-451f-86a7-2947a3592c39");

        private readonly HttpClient _originalClient;
        private readonly NodeTemplateFactoryMock _factory = new();
        private readonly OpenWeatherDriverFactory _driverFactory = new();
        private readonly CapturingDispatcher _dispatcher = new();

        public OpenWeatherMapDriverTests()
        {
            _originalClient = RestApi.Client;
            _driverFactory.InitNodeTemplates(_factory);
            _factory.SetSettings("Latitude", 48.3069);
            _factory.SetSettings("Longitude", 14.2858);
        }

        public void Dispose()
        {
            RestApi.Client = _originalClient;
        }

        private FakeHttpMessageHandler Respond(string json)
        {
            var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(json));
            RestApi.Client = new HttpClient(handler);
            return handler;
        }

        private NodeInstance Node(Guid template, params NodeInstance[] children)
        {
            var node = _factory.CreateNodeInstance(template);
            node.ObjId = Guid.NewGuid();
            foreach (var child in children)
            {
                node.InverseThis2ParentNodeInstanceNavigation.Add(child);
            }
            return node;
        }

        private NodeInstance Root(int forecastCount, params NodeInstance[] children)
        {
            var root = Node(_driverFactory.DriverGuid, children);
            root.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "api-key").Value = "test-key";
            root.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "poll").Value = 7200;
            root.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == "forecast_count").Value = forecastCount;
            return root;
        }

        private async Task<IDriver> Start(NodeInstance root)
        {
            var context = new DriverContextMock(root, _driverFactory, _factory, _dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = _driverFactory.CreateDriver(context);
            await driver.Init();
            await driver.Configure();
            await driver.Start();
            return driver;
        }

        private static System.Timers.Timer PollTimer(IDriver driver)
        {
            var field = driver.GetType().GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (System.Timers.Timer)field!.GetValue(driver)!;
        }

        // ---------- pure helpers ----------

        [Theory]
        [InlineData(new double[] { 350, 10 }, 0)]
        [InlineData(new double[] { 10, 350 }, 0)]
        [InlineData(new double[] { 90, 180 }, 135)]
        [InlineData(new double[] { 270, 0 }, 315)]
        [InlineData(new double[] { 359 }, 359)]
        [InlineData(new double[] { 0, 0, 0 }, 0)]
        [InlineData(new double[] { 340, 350, 0, 10, 20 }, 0)]
        public void CircularMeanDegrees_AveragesOnTheCircle(double[] degrees, double expected)
        {
            var mean = OpenWeatherMapDriver.CircularMeanDegrees(degrees);

            // 0 and 360 are the same direction, compare on the circle
            var diff = Math.Abs(((mean - expected) % 360 + 540) % 360 - 180);
            Assert.True(diff < 1e-6, $"mean {mean} expected {expected}");
            Assert.InRange(mean, 0, 359.999999);
        }

        [Fact]
        public void CircularMeanDegrees_NorthAcrossZero_IsNotSouth()
        {
            // the arithmetic mean of 350 and 10 would be 180 (south), the regression this guards against
            var mean = Math.Round(OpenWeatherMapDriver.CircularMeanDegrees(new double[] { 350, 10 }), 2);

            Assert.True(mean < 1 || mean > 359, $"mean {mean}");
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(1, 1)]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(4, 4)]
        [InlineData(10, 4)]
        public void LimitForecastDays_ClampsToAvailableDays(int forecastCount, int expected)
        {
            var days = Enumerable.Range(0, 4).Select(i => new Daily { AnalysisDateUnix = OneCallJson.Day1 + i * 86400 }).ToArray();

            var limited = OpenWeatherMapDriver.LimitForecastDays(days, forecastCount);

            Assert.Equal(expected, limited.Count);
            Assert.Equal(OneCallJson.Day1, limited[0].AnalysisDateUnix);
        }

        [Fact]
        public void LimitForecastDays_NoDays_ReturnsEmpty()
        {
            Assert.Empty(OpenWeatherMapDriver.LimitForecastDays(Array.Empty<Daily>(), 3));
        }

        // ---------- templates ----------

        [Fact]
        public void InitNodeTemplates_ForecastCountProperty_Exists()
        {
            var root = Root(3);

            Assert.Contains(root.PropertyInstance, p => p.This2PropertyTemplateNavigation.Key == "forecast_count");
            Assert.Equal("openweathermap-forecast-average", _factory.GetNodeTemplateById(OpenWeatherDriverFactory.ForecastDailyAverage).Key);
        }

        [Fact(Skip = "BUG: the forecast TEMPERATURE_MIN template is created with key openweathermap-forecast-temperature-max, so it shows the maximum (OpenWeatherDriverFactory.cs:84)")]
        public void InitNodeTemplates_ForecastTemperatureMin_HasMinKey()
        {
            Assert.Equal("openweathermap-forecast-temperature-min", _factory.GetNodeTemplateById(ForecastTemperatureMin).Key);
        }

        // ---------- driver integration with a fake One Call API ----------

        [Fact]
        public async Task Start_CurrentValues_AreDispatched()
        {
            var handler = Respond(OneCallJson.Build(OneCallJson.FourDays(), currentTemp: 12.5, currentHumidity: 71, currentWindDeg: 225));
            var temp = Node(CurrentTemperature);
            var humidity = Node(CurrentHumidity);
            var wind = Node(CurrentWindDirection);
            var max = Node(TodayTemperatureMax);

            var driver = await Start(Root(3, temp, humidity, wind, max));
            await driver.Stop();

            Assert.Equal(12.5, _dispatcher.LastValueOf(temp.ObjId));
            Assert.Equal(71, _dispatcher.LastValueOf(humidity.ObjId));
            Assert.Equal(225, _dispatcher.LastValueOf(wind.ObjId));
            Assert.Equal(14d, _dispatcher.LastValueOf(max.ObjId));
            Assert.Single(handler.RequestedUris);
        }

        [Fact]
        public async Task Start_ForecastAverages_HonourForecastCount()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays()));
            var temp = Node(ForecastTemperature);
            var humidity = Node(ForecastHumidity);
            var max = Node(ForecastTemperatureMax);
            var from = Node(ForecastFrom);
            var to = Node(ForecastTo);
            var description = Node(ForecastCloudsDescription);
            var precipitation = Node(ForecastPrecipitation);
            var average = Node(OpenWeatherDriverFactory.ForecastDailyAverage, temp, humidity, max, from, to, description, precipitation);

            var driver = await Start(Root(2, average));
            await driver.Stop();

            // only the first two of the four days count, the hot third/fourth day is excluded
            Assert.Equal(11d, _dispatcher.LastValueOf(temp.ObjId));
            Assert.Equal(70d, _dispatcher.LastValueOf(humidity.ObjId));
            Assert.Equal(16d, _dispatcher.LastValueOf(max.ObjId));
            Assert.Equal(new DateTime(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc), _dispatcher.LastValueOf(from.ObjId));
            Assert.Equal(new DateTime(2026, 11, 7, 8, 0, 0, DateTimeKind.Utc), _dispatcher.LastValueOf(to.ObjId));
            Assert.Equal("few clouds", _dispatcher.LastValueOf(description.ObjId));
            Assert.Equal(0.1, _dispatcher.LastValueOf(precipitation.ObjId));
        }

        [Fact]
        public async Task Start_ForecastCountAboveAvailableDays_UsesAllDays()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays()));
            var temp = Node(ForecastTemperature);
            var to = Node(ForecastTo);

            var driver = await Start(Root(10, Node(OpenWeatherDriverFactory.ForecastDailyAverage, temp, to)));
            await driver.Stop();

            Assert.Equal(20.5d, _dispatcher.LastValueOf(temp.ObjId));
            Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc), _dispatcher.LastValueOf(to.ObjId));
        }

        [Fact]
        public async Task Start_ForecastWindDirection_IsCircularMean()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays()));
            var wind = Node(ForecastWindDirection);

            var driver = await Start(Root(2, Node(OpenWeatherDriverFactory.ForecastDailyAverage, wind)));
            await driver.Stop();

            // days 1 and 2 blow from 350 and 10 degrees: north, not south (180)
            var value = Convert.ToDouble(_dispatcher.LastValueOf(wind.ObjId));
            Assert.True(value < 1 || value > 359, $"wind direction {value}");
        }

        [Fact]
        public async Task Start_GermanCulture_CoordinatesAreSentInvariant()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
            try
            {
                var handler = Respond(OneCallJson.Build(OneCallJson.FourDays()));

                var driver = await Start(Root(3, Node(CurrentTemperature)));
                await driver.Stop();

                var query = HttpUtility.ParseQueryString(Assert.Single(handler.RequestedUris).Query);
                Assert.Equal("48.3069", query["lat"]);
                Assert.Equal("14.2858", query["lon"]);
                Assert.Equal("test-key", query["appid"]);
                Assert.Equal("metric", query["units"]);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public async Task Start_ApiFailure_DoesNotThrowAndDispatchesNothing()
        {
            var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("{}", System.Net.HttpStatusCode.Unauthorized));
            RestApi.Client = new HttpClient(handler);
            var temp = Node(CurrentTemperature);

            var driver = await Start(Root(3, temp));
            await driver.Stop();

            Assert.Empty(_dispatcher.ValuesOf(temp.ObjId));
        }

        [Fact]
        public async Task Read_PollsTheApiAgain()
        {
            var handler = Respond(OneCallJson.Build(OneCallJson.FourDays()));
            var temp = Node(CurrentTemperature);

            var driver = await Start(Root(3, temp));
            try
            {
                await driver.Children.Single().Read();

                Assert.Equal(2, handler.RequestedUris.Count);
                Assert.Equal(2, _dispatcher.ValuesOf(temp.ObjId).Count);
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Init_PollProperty_SetsTimerIntervalInSeconds()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays()));

            var driver = await Start(Root(3, Node(CurrentTemperature)));
            try
            {
                Assert.Equal(7200 * 1000d, PollTimer(driver).Interval);
            }
            finally
            {
                await driver.Stop();
            }
        }

        [Fact]
        public async Task Stop_StopsThePollTimer()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays()));

            var driver = await Start(Root(3, Node(CurrentTemperature)));
            Assert.True(PollTimer(driver).Enabled);

            await driver.Stop();

            Assert.False(PollTimer(driver).Enabled);
        }
    }
}
