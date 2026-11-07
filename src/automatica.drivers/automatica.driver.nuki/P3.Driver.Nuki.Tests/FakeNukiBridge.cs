using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Nuki.Tests
{
    internal sealed record BridgeRequest(string Method, string Path, IReadOnlyDictionary<string, string> Query, string RawQuery);

    /// <summary>
    /// A Nuki bridge HTTP API stand-in on Kestrel, 127.0.0.1 with an ephemeral port
    /// </summary>
    internal sealed class FakeNukiBridge : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly ConcurrentQueue<BridgeRequest> _requests = new();

        private FakeNukiBridge(IHost host)
        {
            _host = host;
        }

        public int Port { get; private set; }

        public IReadOnlyList<BridgeRequest> Requests => _requests.ToArray();

        /// <summary>
        /// Answers every request, defaults to 200 "[]"
        /// </summary>
        public Func<HttpContext, Task> Handler { get; set; } = ctx => Respond(ctx, 200, "[]");

        public static Task Respond(HttpContext context, int status, string body)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(body);
        }

        public void RespondWith(int status, string body)
        {
            Handler = ctx => Respond(ctx, status, body);
        }

        public static async Task<FakeNukiBridge> StartAsync()
        {
            FakeNukiBridge bridge = null;
            var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseKestrel(o => o.Listen(IPAddress.Loopback, 0))
                    .Configure(app => app.Run(async ctx =>
                    {
                        var query = ctx.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
                        bridge!._requests.Enqueue(new BridgeRequest(ctx.Request.Method, ctx.Request.Path.Value, query, ctx.Request.QueryString.Value?.TrimStart('?') ?? ""));
                        await bridge.Handler(ctx);
                    })))
                .Build();

            bridge = new FakeNukiBridge(host);
            await host.StartAsync();

            var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            bridge.Port = new Uri(address).Port;
            return bridge;
        }

        public async Task<bool> WaitForRequestsAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (_requests.Count >= count)
                {
                    return true;
                }
                await Task.Delay(20);
            }
            return _requests.Count >= count;
        }

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
