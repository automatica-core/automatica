using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Automower.Tests
{
    internal sealed record RecordedRequest(string Method, string Path, string Query, IReadOnlyDictionary<string, string> Headers, string Body);

    /// <summary>
    /// Kestrel on 127.0.0.1 with an ephemeral port. Answers with <see cref="Handler"/>, streams server-sent events from
    /// <see cref="StreamHandler"/> and accepts web sockets through <see cref="WebSocketHandler"/>.
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

        /// <summary>
        /// Return a channel to answer the request as text/event-stream: every string written is sent as is, completing the channel ends the response
        /// </summary>
        public Func<RecordedRequest, ChannelReader<string>> StreamHandler { get; set; } = _ => null;

        /// <summary>
        /// Handles an accepted web socket until the returned task ends
        /// </summary>
        public Func<RecordedRequest, WebSocket, Task> WebSocketHandler { get; set; }

        public static async Task<FakeHttpServer> StartAsync()
        {
            FakeHttpServer server = null;
            var host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(web => web
                    .UseKestrel(o => o.Listen(IPAddress.Loopback, 0))
                    .Configure(app =>
                    {
                        app.UseWebSockets();
                        app.Run(async ctx => await server!.HandleAsync(ctx));
                    }))
                .Build();

            server = new FakeHttpServer(host);
            await host.StartAsync();
            server.Url = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            return server;
        }

        private async Task HandleAsync(HttpContext ctx)
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();
            var request = new RecordedRequest(ctx.Request.Method, ctx.Request.Path.Value, ctx.Request.QueryString.Value ?? "",
                ctx.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase), body);
            _requests.Enqueue(request);

            if (ctx.WebSockets.IsWebSocketRequest && WebSocketHandler != null)
            {
                using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
                await WebSocketHandler(request, socket);
                return;
            }

            var stream = StreamHandler(request);
            if (stream != null)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/event-stream";
                await ctx.Response.Body.FlushAsync();
                try
                {
                    await foreach (var chunk in stream.ReadAllAsync(ctx.RequestAborted))
                    {
                        await ctx.Response.WriteAsync(chunk, ctx.RequestAborted);
                        await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    // client went away
                }
                return;
            }

            var (status, response) = Handler(request);
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(response ?? "");
        }

        public static Task SendTextAsync(WebSocket socket, string text, CancellationToken token = default)
        {
            return socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, token);
        }

        public static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken token = default)
        {
            var buffer = new byte[16 * 1024];
            using var ms = new MemoryStream();
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }
                ms.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Waits until <paramref name="condition"/> holds (event-driven tests)
        /// </summary>
        public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Condition not met in time");
                }
                await Task.Delay(20);
            }
        }

        public async ValueTask DisposeAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            try
            {
                await _host.StopAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // open streams
            }
            _host.Dispose();
        }
    }
}
