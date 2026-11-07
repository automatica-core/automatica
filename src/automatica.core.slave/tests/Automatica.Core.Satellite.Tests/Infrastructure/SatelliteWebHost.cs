using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Automatica.Core.Satellite.Tests.Infrastructure
{
    /// <summary>
    /// Tests that use the satellite's static state (Startup's setup token and jwt key, SatelliteSecrets' directory)
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class SatelliteStaticsCollection
    {
        public const string Name = "satellite statics";
    }

    /// <summary>
    /// The satellite web app (the real Startup pipeline) on a TestServer, with in-memory config, secrets in a temp
    /// directory, a fake docker and a real loopback broker. Program.Main is not used: it writes logs and the
    /// writable appsettings.json next to the binaries.
    /// </summary>
    internal sealed class SatelliteWebHost : IAsyncDisposable
    {
        public const string AdminUser = "admin";
        public const string AdminPassword = "correct-horse-battery";

        public WebApplication App { get; private set; }
        public HttpClient Client { get; private set; }
        public CapturingLoggerProvider Logs { get; } = new();
        public FakeDocker Docker { get; } = new();
        public TestBroker Broker { get; private set; }
        public string ConfigDirectory { get; private set; }

        public IConfiguration Configuration => App.Configuration;
        public SatelliteRuntime Runtime => App.Services.GetRequiredService<SatelliteRuntime>();

        public static async Task<SatelliteWebHost> StartAsync(IDictionary<string, string> config = null)
        {
            var host = new SatelliteWebHost();
            host.ConfigDirectory = Path.Combine(Path.GetTempPath(), "automatica-satellite-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(host.ConfigDirectory);
            SatelliteSecrets.ConfigDirectoryOverride = host.ConfigDirectory;

            host.Broker = await TestBroker.StartAsync();

            var values = new Dictionary<string, string>
            {
                { "server:master", "" },
                { "server:masterPort", host.Broker.Port.ToString() },
                { "server:clientId", "" },
                { "server:clientKey", "" },
                { "server:dockerTag", "latest-develop" },
                { "docker:devices", "none" }
            };
            foreach (var kv in config ?? new Dictionary<string, string>())
            {
                values[kv.Key] = kv.Value;
            }

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Production",
                ApplicationName = typeof(Startup).Assembly.GetName().Name,
                ContentRootPath = host.ConfigDirectory
            });
            builder.WebHost.UseTestServer();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(values);
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(host.Logs);
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            Startup.ConfigureServices(builder.Services, builder.Environment);

            var docker = host.Docker.Client.Object;
            builder.Services.RemoveAll<SatelliteRuntime>();
            builder.Services.AddSingleton(sp => new SatelliteRuntime(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<SatelliteRuntime>>(), docker));

            host.App = builder.Build();
            Startup.Configure(host.App);
            await host.App.StartAsync();
            host.Client = host.App.GetTestClient();
            return host;
        }

        /// <summary>
        /// The setup token the satellite logged on start
        /// </summary>
        public string SetupToken
        {
            get
            {
                var entry = Logs.Entries.LastOrDefault(e => e.Message.Contains("Setup token for the first login"));
                return entry == null ? null : Regex.Match(entry.Message, @"first login: (\S+)").Groups[1].Value;
            }
        }

        public Task<HttpResponseMessage> PostJson(string path, object body, string bearer = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            if (bearer != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }
            return Client.SendAsync(request);
        }

        public Task<HttpResponseMessage> Get(string path, string bearer = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (bearer != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }
            return Client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> Setup(string user = AdminUser, string password = AdminPassword, string token = null)
        {
            return await PostJson("/webapi/setup", new { username = user, password, setupToken = token ?? SetupToken });
        }

        public Task<HttpResponseMessage> Login(string user = AdminUser, string password = AdminPassword) =>
            PostJson("/webapi/login", new { username = user, password });

        public async Task<string> SetupAndLogin()
        {
            var setup = await Setup();
            Assert.Equal(System.Net.HttpStatusCode.NoContent, setup.StatusCode);
            var login = await Login();
            Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
            return JsonSerializer.Deserialize<string>(await login.Content.ReadAsStringAsync());
        }

        public byte[] JwtKey => File.ReadAllBytes(Path.Combine(ConfigDirectory, SatelliteSecrets.JwtKeyFileName));

        public async ValueTask DisposeAsync()
        {
            Client?.Dispose();
            if (App != null)
            {
                await App.StopAsync();
                await App.DisposeAsync();
            }
            if (Broker != null)
            {
                await Broker.DisposeAsync();
            }
            SatelliteSecrets.ConfigDirectoryOverride = null;
            try
            {
                Directory.Delete(ConfigDirectory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
