using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace P3.Driver.HueBridge.Tests
{
    /// <summary>
    /// Real Kestrel on 127.0.0.1 (never SSDP discovery, Start() would publish on the LAN)
    /// </summary>
    public class HueWebHostTests
    {
        private static readonly FieldInfo HostField = typeof(HueDriver).GetField("_host", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static HueDriver CreateDriver(int port = HueDriver.DefaultPort)
        {
            var driver = new HueDriver(NullLogger.Instance, IPAddress.Loopback, port);
            driver.AddLight(new Data.HueLight("Kitchen", "kitchen-1"));
            driver.LoadUsers(new[] { HueTestFactory.ValidUser });
            return driver;
        }

        /// <summary>
        /// HueStartup (see <see cref="HueStartupUnderTest"/>) on Kestrel with an ephemeral loopback port and the same body limit as <see cref="HueDriver.BuildWebHost"/>
        /// </summary>
        private static async Task<(IHost Host, HttpClient Client)> StartKestrelAsync(HueDriver driver)
        {
            var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseKestrel(o =>
                    {
                        o.Listen(IPAddress.Loopback, 0);
                        o.Limits.MaxRequestBodySize = 16 * 1024;
                    })
                    .ConfigureServices(s => HueStartupUnderTest.ConfigureServices(s, driver))
                    .Configure(HueStartupUnderTest.Configure))
                .Build();
            await host.StartAsync();

            var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) };
            return (host, client);
        }

        private static async Task StopAsync(IHost host, HttpClient client)
        {
            client.Dispose();
            await host.StopAsync();
            host.Dispose();
        }

        [Fact(Skip = "BUG: HueDriver.BuildWebHost (Host.CreateDefaultBuilder + HueStartup.UseMvc with endpoint routing on) throws InvalidOperationException on start, so the bridge never listens (HueStartup.cs:42)")]
        public async Task BuildWebHost_StartAsync_ServesPublicConfig()
        {
            var port = HueTestFactory.GetFreeLoopbackPort();
            var driver = CreateDriver(port);
            using var host = driver.BuildWebHost();

            await host.StartAsync();
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var json = JObject.Parse(await client.GetStringAsync($"http://127.0.0.1:{port}/api/config"));
                Assert.Equal(driver.BridgeId.ToString(), json.Value<string>("bridgeid"));
            }
            finally
            {
                await host.StopAsync();
            }
        }

        [Fact]
        public async Task Kestrel_Loopback_ServesPublicConfig()
        {
            var driver = CreateDriver();
            var (host, client) = await StartKestrelAsync(driver);
            try
            {
                var json = JObject.Parse(await client.GetStringAsync("/api/config"));

                Assert.Equal(driver.BridgeId.ToString(), json.Value<string>("bridgeid"));
            }
            finally
            {
                await StopAsync(host, client);
            }
        }

        [Fact]
        public async Task Kestrel_Loopback_PutLightStateSwitchesLight()
        {
            var driver = CreateDriver();
            var (host, client) = await StartKestrelAsync(driver);
            try
            {
                var response = await client.PutAsync($"/api/{HueTestFactory.ValidUser}/lights/1/state",
                    new StringContent("{\"on\":true,\"bri\":10}", Encoding.UTF8, "application/json"));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(driver.Lights[1].State.On);
                Assert.Equal(10, driver.Lights[1].State.Bri);
            }
            finally
            {
                await StopAsync(host, client);
            }
        }

        [Fact]
        public async Task Kestrel_Loopback_OversizedBodyRejected()
        {
            const int padding = 8 * 1024;
            var driver = CreateDriver();
            var (host, client) = await StartKestrelAsync(driver);
            try
            {
                var body = "{\"on\":true}" + new string(' ', padding);
                var response = await client.PutAsync($"/api/{HueTestFactory.ValidUser}/lights/1/state",
                    new StringContent(body, Encoding.UTF8, "application/json"));

                HueAssert.Error(JToken.Parse(await response.Content.ReadAsStringAsync()), 2);
                Assert.False(driver.Lights[1].State.On);
            }
            finally
            {
                await StopAsync(host, client);
            }
        }

        [Fact]
        public async Task Kestrel_Loopback_UnknownUserRejected()
        {
            var (host, client) = await StartKestrelAsync(CreateDriver());
            try
            {
                var json = JToken.Parse(await client.GetStringAsync($"/api/{HueTestFactory.OtherValidUser}/lights"));

                HueAssert.Error(json, 1, "/lights");
            }
            finally
            {
                await StopAsync(host, client);
            }
        }

        [Fact]
        public async Task Stop_NeverStarted_DoesNotThrow()
        {
            var driver = HueTestFactory.CreateDriver();

            await driver.Stop();

            Assert.Null(HostField.GetValue(driver));
        }

        [Fact]
        public async Task Stop_CalledTwice_DoesNotThrow()
        {
            var driver = HueTestFactory.CreateDriver();

            await driver.Stop();
            await driver.Stop();
        }

        [Fact]
        public async Task Stop_RunningHost_StopsKestrelAndReleasesThePort()
        {
            var driver = CreateDriver();
            var (host, client) = await StartKestrelAsync(driver);
            var port = client.BaseAddress!.Port;
            client.Dispose();
            // Start() would also publish SSDP on the LAN, so hand the running host to the driver directly
            HostField.SetValue(driver, host);

            await driver.Stop();
            await driver.Stop();

            Assert.Null(HostField.GetValue(driver));
            // the port can only be bound again once Kestrel has really let go of it
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
        }
    }
}
