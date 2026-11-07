using System;
using System.Collections.Concurrent;
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
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.Shelly.Unit.Tests.Fakes
{
    internal sealed record RecordedRequest(string Method, string Path, string Query, string Body, string Authorization);

    internal sealed record FakeResponse(int Status, string Body, TimeSpan Delay = default)
    {
        public static FakeResponse Json(string body) => new(200, body);
    }

    /// <summary>
    /// A fake Shelly device: real Kestrel on 127.0.0.1 with an ephemeral port, serving HTTP (Gen1 REST, Gen2 RPC over POST /rpc)
    /// and the Gen2 websocket on /rpc.
    /// </summary>
    internal sealed class FakeShellyServer : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly Channel<string> _wsReceived = Channel.CreateUnbounded<string>();
        private readonly TaskCompletionSource<WebSocket> _socketConnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource _shutdown = new();

        public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

        /// <summary>
        /// Answers a plain HTTP request, default: 404.
        /// </summary>
        public Func<RecordedRequest, FakeResponse> Handler { get; set; } = _ => new FakeResponse(404, "");

        public int Port { get; private set; }

        /// <summary>
        /// What the clients get as "ip address" (they build http://{ip} and ws://{ip}/rpc from it).
        /// </summary>
        public string Address => $"127.0.0.1:{Port}";

        private FakeShellyServer()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            _app = builder.Build();
            _app.UseWebSockets();
            _app.Run(HandleAsync);
        }

        public static async Task<FakeShellyServer> StartAsync()
        {
            var server = new FakeShellyServer();
            await server._app.StartAsync();

            var address = server._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            server.Port = new Uri(address).Port;
            return server;
        }

        private async Task HandleAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await HandleWebSocketAsync(context);
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            var request = new RecordedRequest(context.Request.Method, context.Request.Path.Value ?? "",
                context.Request.QueryString.Value ?? "", body, context.Request.Headers.Authorization.ToString());
            Requests.Enqueue(request);

            var response = Handler(request);
            if (response.Delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(response.Delay, context.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            context.Response.StatusCode = response.Status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(response.Body);
        }

        private async Task HandleWebSocketAsync(HttpContext context)
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            _socketConnected.TrySetResult(socket);

            var buffer = new byte[8192];
            using var message = new MemoryStream();
            try
            {
                while (socket.State == WebSocketState.Open && !_shutdown.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(buffer, _shutdown.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        await _wsReceived.Writer.WriteAsync(Encoding.UTF8.GetString(message.ToArray()));
                        message.SetLength(0);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException)
            {
            }
        }

        public async Task<string> ReceiveWebSocketMessageAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return await _wsReceived.Reader.ReadAsync(cts.Token);
        }

        public async Task SendWebSocketMessageAsync(string message, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var socket = await _socketConnected.Task.WaitAsync(cts.Token);
            await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await _app.StopAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            await _app.DisposeAsync();
            _shutdown.Dispose();
        }

        /// <summary>
        /// A loopback endpoint of a device that is "offline": every connection is reset right away.
        /// (A closed port is not used on purpose, Windows retries a refused loopback connect for ~2 s.)
        /// </summary>
        public static OfflineDevice StartOfflineDevice() => new();
    }

    internal sealed class OfflineDevice : IDisposable
    {
        private readonly System.Net.Sockets.TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();

        public OfflineDevice()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoop);
        }

        public int Port { get; }

        public string Address => $"127.0.0.1:{Port}";

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    client.Client.LingerState = new System.Net.Sockets.LingerOption(true, 0);
                    client.Close(); // RST
                }
                catch
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }
    }
}
