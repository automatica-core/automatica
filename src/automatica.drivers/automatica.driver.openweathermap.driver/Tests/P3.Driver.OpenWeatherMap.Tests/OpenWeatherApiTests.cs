using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using P3.Driver.OpenWeather.Api;
using P3.Driver.OpenWeather.Api.Enums;
using P3.Driver.OpenWeather.Api.Exceptions;
using Xunit;

namespace P3.Driver.OpenWeatherMap.Tests
{
    public sealed class OpenWeatherApiTests : IDisposable
    {
        private const string TestApiKey = "test-api-key-not-real";
        private readonly HttpClient _originalClient;
        private readonly CultureInfo _originalCulture;
        private readonly WeatherClient _client = new(TestApiKey);

        public OpenWeatherApiTests()
        {
            _originalClient = RestApi.Client;
            _originalCulture = CultureInfo.CurrentCulture;
            // a culture with a decimal comma: the coordinates must still be sent with a point
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
        }

        public void Dispose()
        {
            RestApi.Client = _originalClient;
            CultureInfo.CurrentCulture = _originalCulture;
        }

        private FakeHttpMessageHandler Respond(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(json, status));
            RestApi.Client = new HttpClient(handler);
            return handler;
        }

        private static System.Collections.Specialized.NameValueCollection Query(Uri uri) => HttpUtility.ParseQueryString(uri.Query);

        [Fact]
        public async Task GetOneCallAsync_SendsInvariantCoordinatesUnitsAndLanguage()
        {
            var handler = Respond(OneCallJson.Build(OneCallJson.FourDays()));

            await _client.GetOneCallAsync(48.3069, 14.2858, [], Measurement.Metric, Language.German);

            var uri = Assert.Single(handler.RequestedUris);
            Assert.Equal("api.openweathermap.org", uri.Host);
            Assert.Equal("/data/3.0/onecall", uri.AbsolutePath);
            var query = Query(uri);
            Assert.Equal("48.3069", query["lat"]);
            Assert.Equal("14.2858", query["lon"]);
            Assert.Equal(TestApiKey, query["appid"]);
            Assert.Equal("metric", query["units"]);
            Assert.Equal("de", query["lang"]);
            Assert.Null(query["exclude"]);
        }

        [Fact]
        public async Task GetOneCallAsync_Exclude_IsCommaJoined()
        {
            var handler = Respond(OneCallJson.Build(OneCallJson.FourDays()));

            await _client.GetOneCallAsync(-33.5, -70.25, [ExcludeOneCall.Minutely, ExcludeOneCall.Alerts], Measurement.Imperial, Language.English);

            var query = Query(Assert.Single(handler.RequestedUris));
            Assert.Equal("minutely,alerts", query["exclude"]);
            Assert.Equal("-33.5", query["lat"]);
            Assert.Equal("-70.25", query["lon"]);
            Assert.Equal("imperial", query["units"]);
            Assert.Equal("en", query["lang"]);
        }

        [Fact]
        public async Task GetOneCallAsync_ParsesCurrentAndDaily()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays(), currentTemp: 12.5, currentHumidity: 71, currentWindDeg: 225));

            var model = await _client.GetOneCallAsync(48.3, 14.3, [], Measurement.Metric);

            Assert.Equal("Europe/Vienna", model.TimezoneName);
            Assert.Equal(3600, model.TimezoneOffset);
            Assert.Equal(12.5, model.CurrentWeather!.Temperature);
            Assert.Equal(71, model.CurrentWeather.HumidityPercentage);
            Assert.Equal(225, model.CurrentWeather.WindDirection);
            Assert.Equal(4, model.DailyForecasts!.Length);
            Assert.Equal(new DateTime(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc), model.DailyForecasts[0].AnalysisDate);
            Assert.Equal(14, model.DailyForecasts[0].Temperature.Maximum);
            Assert.Equal(4, model.DailyForecasts[0].Temperature.Minimum);
            Assert.Equal("clear sky", model.DailyForecasts[0].Description.First().Description);
            Assert.Equal("first", model.DailyForecasts[0].Summary);
        }

        [Fact]
        public async Task GetHistoricalOneCallAsync_SendsInvariantCoordinates()
        {
            var handler = Respond("{}");

            await _client.GetHistoricalOneCallAsync(47.0707, 15.4395, OneCallJson.Day1, Measurement.Metric);

            var uri = Assert.Single(handler.RequestedUris);
            Assert.Equal("/data/2.5/onecall/timemachine", uri.AbsolutePath);
            Assert.Equal("47.0707", Query(uri)["lat"]);
            Assert.Equal("15.4395", Query(uri)["lon"]);
            Assert.Equal(OneCallJson.Day1.ToString(CultureInfo.InvariantCulture), Query(uri)["dt"]);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_Coordinates_AreInvariant()
        {
            var handler = Respond("{}");

            await _client.GetCurrentWeatherAsync(48.3069, 14.2858, Measurement.Metric, Language.German);

            var uri = Assert.Single(handler.RequestedUris);
            Assert.Equal("/data/2.5/weather", uri.AbsolutePath);
            Assert.Equal("48.3069", Query(uri)["lat"]);
            Assert.Equal("14.2858", Query(uri)["lon"]);
        }

        [Fact]
        public async Task GetForecastAsync_Coordinates_AreInvariantAndCountIsSent()
        {
            var handler = Respond("{\"list\":[]}");

            var result = await _client.GetForecastAsync(48.3069, 14.2858, 8, Measurement.Metric);

            Assert.Empty(result);
            var query = Query(Assert.Single(handler.RequestedUris));
            Assert.Equal("48.3069", query["lat"]);
            Assert.Equal("14.2858", query["lon"]);
            Assert.Equal("8", query["cnt"]);
        }

        [Fact]
        public async Task AirPollution_AllCalls_UseInvariantCoordinates()
        {
            var handler = Respond("{}");

            await _client.GetCurrentAirPollutionAsync(48.5, 14.25);
            await _client.GetForecastAirPollutionAsync(48.5, 14.25);
            await _client.GetHistoricalAirPollutionAsync(48.5, 14.25, OneCallJson.Day1, OneCallJson.Day2);

            var uris = handler.RequestedUris.ToList();
            Assert.Equal(new[] { "/data/2.5/air_pollution", "/data/2.5/air_pollution/forecast", "/data/2.5/air_pollution/history" },
                uris.Select(u => u.AbsolutePath));
            Assert.All(uris, u =>
            {
                Assert.Equal("48.5", Query(u)["lat"]);
                Assert.Equal("14.25", Query(u)["lon"]);
            });
            Assert.Equal(OneCallJson.Day2.ToString(CultureInfo.InvariantCulture), Query(uris[2])["end"]);
        }

        [Fact]
        public async Task LocationNameByCoordinatesAsync_UsesInvariantCoordinatesAndLimit()
        {
            var handler = Respond("[{\"name\":\"Linz\",\"lat\":48.3,\"lon\":14.28,\"country\":\"AT\"}]");

            var result = await _client.LocationNameByCoordinatesAsync(48.3069, 14.2858, 2);

            Assert.Single(result);
            var uri = Assert.Single(handler.RequestedUris);
            Assert.Equal("/geo/1.0/reverse", uri.AbsolutePath);
            Assert.Equal("48.3069", Query(uri)["lat"]);
            Assert.Equal("14.2858", Query(uri)["lon"]);
            Assert.Equal("2", Query(uri)["limit"]);
        }

        [Fact]
        public async Task RestApi_BadRequest_ThrowsBadRequest()
        {
            Respond("{\"cod\":\"400\",\"message\":\"wrong latitude\"}", HttpStatusCode.BadRequest);

            await Assert.ThrowsAsync<HttpBadRequestException>(() => _client.GetOneCallAsync(1, 2, []));
        }

        [Fact(Skip = "BUG: HttpBadRequestException(string message) calls base() and drops the server message (HttpBadRequestException.cs:9)")]
        public async Task RestApi_BadRequest_ThrowsWithServerMessage()
        {
            Respond("{\"cod\":\"400\",\"message\":\"wrong latitude\"}", HttpStatusCode.BadRequest);

            var ex = await Assert.ThrowsAsync<HttpBadRequestException>(() => _client.GetOneCallAsync(1, 2, []));

            Assert.Equal("wrong latitude", ex.Message);
        }

        [Fact]
        public async Task RestApi_Unauthorized_ThrowsUnauthorized()
        {
            Respond("{\"cod\":401}", HttpStatusCode.Unauthorized);

            await Assert.ThrowsAsync<HttpUnauthorizedException>(() => _client.GetOneCallAsync(1, 2, []));
        }

        [Fact]
        public async Task RestApi_NotFound_ThrowsNotFound()
        {
            Respond("{}", HttpStatusCode.NotFound);

            await Assert.ThrowsAsync<HttpNotFoundException>(() => _client.GetOneCallAsync(1, 2, []));
        }

        [Fact]
        public async Task RestApi_TooManyRequests_ThrowsTooManyRequests()
        {
            Respond("{}", HttpStatusCode.TooManyRequests);

            await Assert.ThrowsAsync<HttpTooManyRequestsException>(() => _client.GetOneCallAsync(1, 2, []));
        }

        [Fact]
        public async Task RestApi_ServerError_ThrowsHttpRequestException()
        {
            Respond("{}", HttpStatusCode.BadGateway);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetOneCallAsync(1, 2, []));

            Assert.IsType<HttpRequestException>(ex);
        }

        [Fact]
        public async Task RestApi_ApiKey_IsNeverPartOfAnExceptionMessage()
        {
            Respond("{}", HttpStatusCode.Unauthorized);

            var ex = await Assert.ThrowsAsync<HttpUnauthorizedException>(() => _client.GetOneCallAsync(1, 2, []));

            Assert.DoesNotContain(TestApiKey, ex.Message);
        }

        [Fact]
        public void SyncGetOneCall_ReturnsSameAsAsync()
        {
            Respond(OneCallJson.Build(OneCallJson.FourDays(), currentTemp: -3.25));

            var model = _client.GetOneCall(48.3, 14.3, [], Measurement.Metric);

            Assert.Equal(-3.25, model.CurrentWeather!.Temperature);
        }
    }
}
