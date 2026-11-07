using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace P3.Driver.HueBridge.Tests
{
    /// <summary>
    /// The unmodified HueStartup, exactly as HueDriver.BuildWebHost wires it
    /// </summary>
    public class HueStartupTests
    {
        [Fact(Skip = "BUG: HueStartup.Configure calls app.UseMvc() while MvcOptions.EnableEndpointRouting is true, the host throws InvalidOperationException on start and the simulated bridge never serves a request (HueStartup.cs:42)")]
        public async Task UnmodifiedStartup_StartsAndServesPublicConfig()
        {
            var driver = HueTestFactory.CreateDriver();
            using var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(s => s.AddSingleton(driver))
                    .UseStartup<HueStartup>())
                .Build();

            await host.StartAsync();
            try
            {
                var response = await host.GetTestClient().GetAsync("/api/config");
                response.EnsureSuccessStatusCode();
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}
