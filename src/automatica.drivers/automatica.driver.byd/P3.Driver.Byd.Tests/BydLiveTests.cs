using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using P3.Driver.Byd.Driver;
using Xunit;
using Xunit.Abstractions;

namespace P3.Driver.Byd.Tests
{
    /// <summary>
    /// Talks to the real BYD cloud. Does nothing unless BYD_LIVE_USER and BYD_LIVE_PASSWORD are set, e.g.:
    /// $env:BYD_LIVE_USER="me@example.com"; $env:BYD_LIVE_PASSWORD="..."; $env:BYD_LIVE_COUNTRY="AT"
    /// dotnet test --filter FullyQualifiedName~BydLiveTests --logger "console;verbosity=detailed"
    /// Logs in once with the time zone of this machine and once with UTC (what a container without TZ sends)
    /// and reads the vehicle list and the realtime data of every car.
    /// </summary>
    public class BydLiveTests
    {
        private readonly ITestOutputHelper _output;

        public BydLiveTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private sealed class OutputLogger : ILogger
        {
            private readonly ITestOutputHelper _output;

            public OutputLogger(ITestOutputHelper output) => _output = output;

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _output.WriteLine($"  [{logLevel}] {formatter(state, exception)}");
            }
        }

        [Fact]
        public async Task Live_LoginListAndRealtime()
        {
            var user = Environment.GetEnvironmentVariable("BYD_LIVE_USER");
            var password = Environment.GetEnvironmentVariable("BYD_LIVE_PASSWORD");
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(password))
            {
                _output.WriteLine("BYD_LIVE_USER / BYD_LIVE_PASSWORD not set, skipped");
                return;
            }
            var country = Environment.GetEnvironmentVariable("BYD_LIVE_COUNTRY") ?? "AT";
            var vin = Environment.GetEnvironmentVariable("BYD_LIVE_VIN");

            foreach (var timeZone in new[] { BydCloud.TimeZone(), "UTC" })
            {
                _output.WriteLine($"=== country {country}, time zone {timeZone}");
                using var http = new HttpClient();
                var cloud = new BydCloud(http, new OutputLogger(_output), () => DateTimeOffset.UtcNow, null, user, password, country, "en")
                {
                    TimeZoneId = timeZone
                };
                try
                {
                    await cloud.LoginAsync();
                    _output.WriteLine("login: ok");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"login: {ex.Message}");
                    continue;
                }

                string energyType = null;
                try
                {
                    var cars = await cloud.ListVehiclesAsync();
                    _output.WriteLine($"vehicle list: {cars.Count} car(s)");
                    foreach (var car in cars)
                    {
                        _output.WriteLine($"  {car.Vin} alias={car.Alias} model={car.ModelName} energyType={car.EnergyType}");
                        vin ??= car.Vin;
                        if (car.Vin == vin)
                        {
                            energyType = car.EnergyType;
                        }
                    }
                }
                catch (BydApiException ex)
                {
                    _output.WriteLine($"vehicle list: {ex.Message}");
                    _output.WriteLine($"  raw: {ex.Response}");
                }

                if (string.IsNullOrEmpty(vin))
                {
                    _output.WriteLine("no VIN to read, set BYD_LIVE_VIN");
                    continue;
                }
                try
                {
                    var data = await cloud.GetRealtimeAsync(vin, energyType);
                    _output.WriteLine($"realtime {vin}: {(data.ValueKind == System.Text.Json.JsonValueKind.Undefined ? "(empty)" : data.GetRawText())}");
                }
                catch (BydApiException ex)
                {
                    _output.WriteLine($"realtime {vin}: {ex.Message}");
                    _output.WriteLine($"  raw: {ex.Response}");
                }
            }
        }
    }
}
