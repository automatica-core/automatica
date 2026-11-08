using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

namespace P3.Driver.SpotPrice.Tests
{
    internal sealed record RecordedRequest(string Method, string Path, string Query, IReadOnlyDictionary<string, string> Headers, string Body);

    /// <summary>
    /// Kestrel on 127.0.0.1 with an ephemeral port, answers with a handler
    /// </summary>
    internal sealed class FakeHttpServer : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly ConcurrentQueue<RecordedRequest> _requests = new();

        private FakeHttpServer(IHost host)
        {
            _host = host;
        }

        public string Url { get; private set; }

        public IReadOnlyList<RecordedRequest> Requests => _requests.ToArray();

        public Func<RecordedRequest, (int Status, string Body)> Handler { get; set; } = _ => (404, "");

        public static async Task<FakeHttpServer> StartAsync()
        {
            FakeHttpServer server = null;
            var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseKestrel(o => o.Listen(IPAddress.Loopback, 0))
                    .Configure(app => app.Run(async ctx =>
                    {
                        using var reader = new StreamReader(ctx.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        var request = new RecordedRequest(ctx.Request.Method, ctx.Request.Path.Value, ctx.Request.QueryString.Value ?? "",
                            ctx.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase), body);
                        server!._requests.Enqueue(request);
                        var (status, response) = server.Handler(request);
                        ctx.Response.StatusCode = status;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(response ?? "");
                    })))
                .Build();

            server = new FakeHttpServer(host);
            await host.StartAsync();
            server.Url = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            return server;
        }

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
