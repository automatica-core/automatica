#nullable enable annotations
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.Rpc
{
    /// <summary>
    /// A notification a device sent to the outbound websocket server: "src" is the device id (e.g. shellyhtg3-84fce63ad204).
    /// </summary>
    public delegate void ShellyOutboundMessageHandler(string source, string method, JObject parameters, IPAddress remoteAddress);

    /// <summary>
    /// Server for the Gen2+ "outbound websocket" (Ws.SetConfig server = ws://host:port): battery devices (Plus H&amp;T, H&amp;T Gen3, Smoke, Flood...)
    /// sleep and only connect to this server when they wake up, then send NotifyFullStatus / NotifyStatus / NotifyEvent.
    /// A plain TcpListener with the RFC 6455 handshake, so it needs no HTTP.sys URL reservation and no ASP.NET Core in the plugin.
    /// </summary>
    public sealed class ShellyOutboundWebSocketServer : IDisposable
    {
        private const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
        private Task? _acceptLoop;

        public event ShellyOutboundMessageHandler? MessageReceived;

        public int Port { get; private set; }

        public ShellyOutboundWebSocketServer(int port, ILogger logger, IPAddress? bindAddress = null)
        {
            _logger = logger;
            _listener = new TcpListener(bindAddress ?? IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _acceptLoop = Task.Run(AcceptLoop);
            _logger.LogInformation($"Shelly outbound websocket server listening on port {Port}");
        }

        private async Task AcceptLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    _logger.LogDebug($"Shelly outbound websocket accept failed: {e.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            var id = Guid.NewGuid();
            var remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                    handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(10));

                    var key = await ReadUpgradeRequest(stream, handshakeTimeout.Token);
                    if (key == null)
                    {
                        var bad = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(bad, handshakeTimeout.Token);
                        return;
                    }

                    var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + WebSocketGuid)));
                    var response = "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
                                   $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response), handshakeTimeout.Token);

                    using var socket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
                    {
                        IsServer = true,
                        KeepAliveInterval = TimeSpan.FromSeconds(30)
                    });
                    _sockets[id] = socket;
                    _logger.LogDebug($"Shelly outbound websocket connection from {remote}");

                    await ReceiveLoop(socket, remote);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e) when (e is IOException or WebSocketException or SocketException)
                {
                    _logger.LogDebug($"Shelly outbound websocket {remote} closed: {e.Message}");
                }
                finally
                {
                    _sockets.TryRemove(id, out _);
                }
            }
        }

        private static async Task<string?> ReadUpgradeRequest(NetworkStream stream, CancellationToken token)
        {
            // read byte by byte up to the empty line: nothing of the websocket frames may be consumed here
            var builder = new StringBuilder();
            var one = new byte[1];
            while (builder.Length < 8192)
            {
                var read = await stream.ReadAsync(one, token);
                if (read == 0)
                {
                    return null;
                }

                builder.Append((char)one[0]);
                if (builder.Length >= 4 && builder[^1] == '\n' && builder[^2] == '\r' && builder[^3] == '\n' && builder[^4] == '\r')
                {
                    break;
                }
            }

            string? key = null;
            var upgrade = false;
            foreach (var line in builder.ToString().Split("\r\n"))
            {
                var colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var name = line.Substring(0, colon).Trim();
                var value = line.Substring(colon + 1).Trim();
                if (name.Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                {
                    key = value;
                }
                else if (name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) && value.Equals("websocket", StringComparison.OrdinalIgnoreCase))
                {
                    upgrade = true;
                }
            }

            return upgrade ? key : null;
        }

        private async Task ReceiveLoop(WebSocket socket, IPAddress remote)
        {
            var buffer = new byte[16 * 1024];
            using var message = new MemoryStream();
            while (socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    try
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    catch
                    {
                        // the device is already gone
                    }
                    return;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.ToArray());
                message.SetLength(0);

                try
                {
                    var frame = JObject.Parse(text);
                    var source = frame.Value<string>("src");
                    var method = frame.Value<string>("method");
                    if (source == null || method == null)
                    {
                        continue;
                    }

                    MessageReceived?.Invoke(source, method, frame["params"] as JObject ?? new JObject(), remote);
                }
                catch (JsonException)
                {
                    _logger.LogDebug($"Shelly outbound websocket {remote}: ignoring a frame that is no JSON");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error handling an outbound websocket frame of {remote}");
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _listener.Stop();
            }
            catch
            {
                // ignored on dispose
            }

            foreach (var socket in _sockets.Values)
            {
                try
                {
                    socket.Abort();
                }
                catch
                {
                    // ignored on dispose
                }
            }

            try
            {
                _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignored on dispose
            }
            _cts.Dispose();
        }
    }
}
