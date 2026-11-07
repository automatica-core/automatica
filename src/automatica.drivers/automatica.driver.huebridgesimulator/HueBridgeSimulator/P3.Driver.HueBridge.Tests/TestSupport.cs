using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using P3.Driver.HueBridge.Data;
using Xunit;

// HueDriver keeps static state (Instance, Logger, HueDiscoveryService): never run the tests in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Driver.HueBridge.Tests
{
    /// <summary>
    /// Settable clock for the link button window, starts at a fixed date
    /// </summary>
    internal sealed class FakeClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2030, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
    }

    internal static class HueTestFactory
    {
        public const string ValidUser = "testuser-0123456789";
        public const string OtherValidUser = "otheruser-9876543210";

        public static HueDriver CreateDriver(FakeClock clock = null)
        {
            var driver = new HueDriver(NullLogger.Instance, IPAddress.Loopback, HueDriver.DefaultPort);
            clock ??= new FakeClock();
            driver.UtcNow = () => clock.UtcNow;
            return driver;
        }

        public static HueDriver CreateDriverWithLights(int lightCount, FakeClock clock = null)
        {
            var driver = CreateDriver(clock);
            for (var i = 1; i <= lightCount; i++)
            {
                driver.AddLight(new HueLight($"Light {i}", $"unique-{i}"));
            }
            return driver;
        }

        public static HueDriver CreateAuthorizedDriverWithLights(int lightCount, FakeClock clock = null)
        {
            var driver = CreateDriverWithLights(lightCount, clock);
            driver.LoadUsers(new[] { ValidUser });
            return driver;
        }

        public static int GetFreeLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }

    internal static class HueStartupUnderTest
    {
        /// <summary>
        /// HueStartup.ConfigureServices plus the only change needed to start it: UseMvc() refuses to run with endpoint routing
        /// (see the skipped BUG tests in HueStartupTests). Everything else is the production pipeline.
        /// </summary>
        public static void ConfigureServices(IServiceCollection services, HueDriver driver)
        {
            services.AddSingleton(driver);
            new HueStartup().ConfigureServices(services);
            services.Configure<MvcOptions>(o => o.EnableEndpointRouting = false);
        }

        public static void Configure(IApplicationBuilder app)
        {
            new HueStartup().Configure(app);
        }
    }

    /// <summary>
    /// The HueStartup pipeline (SetLightMiddleware + MVC controllers) on an in-memory TestServer
    /// </summary>
    internal sealed class HueTestServer : IAsyncDisposable
    {
        private readonly IHost _host;

        private HueTestServer(IHost host, HueDriver driver)
        {
            _host = host;
            Driver = driver;
            Client = host.GetTestClient();
        }

        public HueDriver Driver { get; }
        public HttpClient Client { get; }

        public static async Task<HueTestServer> StartAsync(HueDriver driver)
        {
            var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services => HueStartupUnderTest.ConfigureServices(services, driver))
                    .Configure(HueStartupUnderTest.Configure))
                .Build();

            await host.StartAsync();
            return new HueTestServer(host, driver);
        }

        public async Task<(HttpResponseMessage Response, JToken Json)> PutAsync(string path, string body)
        {
            var response = await Client.PutAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            return (response, JToken.Parse(text));
        }

        public async Task<(HttpResponseMessage Response, JToken Json)> GetAsync(string path)
        {
            var response = await Client.GetAsync(path);
            var text = await response.Content.ReadAsStringAsync();
            return (response, JToken.Parse(text));
        }

        public async Task<(HttpResponseMessage Response, JToken Json)> PostAsync(string path, string body = "{\"devicetype\":\"test#unit\"}")
        {
            var response = await Client.PostAsync(path, new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            return (response, JToken.Parse(text));
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    internal static class HueAssert
    {
        /// <summary>
        /// Hue API error: [{"error":{"type":..,"address":..,"description":..}}]
        /// </summary>
        public static void Error(JToken json, int expectedType, string expectedAddress = null)
        {
            var array = Assert.IsType<JArray>(json);
            var entry = Assert.Single(array);
            var error = entry["error"];
            Assert.NotNull(error);
            Assert.Equal(expectedType, error.Value<int>("type"));
            if (expectedAddress != null)
            {
                Assert.Equal(expectedAddress, error.Value<string>("address"));
            }
            Assert.False(string.IsNullOrEmpty(error.Value<string>("description")));
        }
    }
}
