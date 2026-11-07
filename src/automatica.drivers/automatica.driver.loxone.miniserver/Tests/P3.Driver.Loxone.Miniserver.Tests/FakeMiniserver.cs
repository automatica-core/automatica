using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    internal sealed class RecordedRequest
    {
        public RecordedRequest(string method, string rawTarget, string authorization)
        {
            Method = method;
            RawTarget = rawTarget;
            Authorization = authorization;
        }

        public string Method { get; }
        public string RawTarget { get; }
        public string Authorization { get; }
    }

    /// <summary>
    /// In-process miniserver on 127.0.0.1 (ephemeral port): LoxAPP3.json and getPublicKey over http, the key exchange,
    /// token acquisition and encrypted commands over the websocket, like a Gen1 miniserver does it.
    /// </summary>
    internal sealed class FakeMiniserver : IAsyncDisposable
    {
        // generating a RSA key takes a while, share it between the tests
        private static readonly Lazy<RSA> SharedRsa = new Lazy<RSA>(() => RSA.Create(2048));
        // the CNG key handle fails sporadically when several servers (parallel test classes) use it at the same time
        private static readonly object RsaLock = new object();
        private static readonly Lazy<string> SharedPublicKey = new Lazy<string>(() =>
        {
            lock (RsaLock)
            {
                return Convert.ToBase64String(SharedRsa.Value.ExportSubjectPublicKeyInfo());
            }
        });

        private static byte[] DecryptSessionKey(byte[] data)
        {
            lock (RsaLock)
            {
                return SharedRsa.Value.Decrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }

        public const string KeyHex = "6B65792D666F722D7468652D686D6163";
        public const string SaltHex = "73616C7431323334";

        private readonly WebApplication _app;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private WebSocket _socket;
        private byte[] _aesKey;
        private byte[] _aesIv;

        public ConcurrentQueue<RecordedRequest> HttpRequests { get; } = new ConcurrentQueue<RecordedRequest>();
        public ConcurrentQueue<string> WebSocketMessages { get; } = new ConcurrentQueue<string>();

        /// <summary>commands that were sent encrypted (fenc/enc), without the salt prefix</summary>
        public ConcurrentQueue<string> DecryptedCommands { get; } = new ConcurrentQueue<string>();

        /// <summary>exceptions of the websocket handler</summary>
        public ConcurrentQueue<string> Errors { get; } = new ConcurrentQueue<string>();

        public string User { get; set; } = "admin";
        public string Password { get; set; } = "made-up-test-password";
        public int LoxAppStatusCode { get; set; } = 200;
        public string LoxAppJson { get; set; } = "{\"lastModified\":\"2026-01-01 10:00:00\",\"msInfo\":{},\"rooms\":{},\"cats\":{},\"controls\":{}}";
        public int WriteResponseCode { get; set; } = 200;
        public bool AcceptWebSockets { get; set; } = true;

        public bool? TokenHashValid { get; private set; }
        public int Port { get; private set; }
        public bool WebSocketConnected => _socket != null && _socket.State == WebSocketState.Open;

        private FakeMiniserver()
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                EnvironmentName = "Testing"
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));

            _app = builder.Build();
            _app.UseWebSockets();
            _app.Run(HandleAsync);
        }

        public static async Task<FakeMiniserver> StartAsync()
        {
            var server = new FakeMiniserver();
            await server._app.StartAsync();

            var address = server._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();
            server.Port = new Uri(address).Port;
            return server;
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _sendLock.Dispose();
        }

        private async Task HandleAsync(HttpContext context)
        {
            if (context.Request.Path == "/ws/rfc6455" && context.WebSockets.IsWebSocketRequest)
            {
                if (!AcceptWebSockets)
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                await RunWebSocketAsync(context);
                return;
            }

            // read before the request is visible to the test, so a test can switch the code once it saw the request
            var writeResponseCode = WriteResponseCode;
            var rawTarget = context.Features.Get<IHttpRequestFeature>().RawTarget;
            HttpRequests.Enqueue(new RecordedRequest(context.Request.Method, rawTarget, context.Request.Headers.Authorization.ToString()));

            var path = context.Request.Path.Value ?? "";
            context.Response.ContentType = "application/json";

            if (path == "/data/LoxAPP3.json")
            {
                context.Response.StatusCode = LoxAppStatusCode;
                if (LoxAppStatusCode == 200)
                {
                    await context.Response.WriteAsync(LoxAppJson);
                }
                return;
            }

            if (path == "/jdev/sys/getPublicKey")
            {
                var spki = SharedPublicKey.Value;
                await context.Response.WriteAsync(Response("dev/sys/getPublicKey", 200, JsonSerializer.Serialize($"-----BEGIN CERTIFICATE-----{spki}-----END CERTIFICATE-----")));
                return;
            }

            if (path.StartsWith("/jdev/sps/io/", StringComparison.Ordinal))
            {
                await context.Response.WriteAsync(Response(rawTarget.TrimStart('/'), writeResponseCode, "\"1\""));
                return;
            }

            context.Response.StatusCode = 404;
        }

        private async Task RunWebSocketAsync(HttpContext context)
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            _socket = socket;

            var buffer = new byte[64 * 1024];
            var message = new StringBuilder();

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.State == WebSocketState.CloseReceived)
                        {
                            await SendLocked(() => socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None));
                        }
                        break;
                    }

                    message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (!result.EndOfMessage)
                    {
                        continue;
                    }

                    var text = message.ToString();
                    message.Clear();
                    await HandleTextAsync(text);
                }
            }
            catch (WebSocketException)
            {
                // client went away
            }
            catch (OperationCanceledException)
            {
                // server shutdown
            }
            catch (Exception e)
            {
                Errors.Enqueue(e.ToString());
                throw;
            }
        }

        private async Task HandleTextAsync(string message)
        {
            WebSocketMessages.Enqueue(message);

            const string keyExchange = "jdev/sys/keyexchange/";
            const string fenc = "jdev/sys/fenc/";
            const string enc = "jdev/sys/enc/";

            if (message.StartsWith(keyExchange, StringComparison.Ordinal))
            {
                var sessionKey = DecryptSessionKey(Convert.FromBase64String(message.Substring(keyExchange.Length)));
                var parts = Encoding.UTF8.GetString(sessionKey).Split(':');
                _aesKey = Convert.FromHexString(parts[0]);
                _aesIv = Convert.FromHexString(parts[1]);

                await SendTextAsync(Response("jdev/sys/keyexchange", 200, JsonSerializer.Serialize(Convert.ToBase64String(Encrypt("session-key-ok")))));
                return;
            }

            var isFenc = message.StartsWith(fenc, StringComparison.Ordinal);
            var isEnc = message.StartsWith(enc, StringComparison.Ordinal);
            if (!isFenc && !isEnc)
            {
                // keepalive and other plain commands
                return;
            }

            var cipher = Convert.FromBase64String(WebUtility.UrlDecode(message.Substring(isFenc ? fenc.Length : enc.Length)));
            var command = Decrypt(cipher).TrimEnd('\0');
            if (command.StartsWith("salt/", StringComparison.Ordinal))
            {
                command = command.Substring(command.IndexOf('/', 5) + 1);
            }
            DecryptedCommands.Enqueue(command);

            string response;
            if (command.StartsWith("jdev/sys/getkey2/", StringComparison.Ordinal))
            {
                response = Response(command, 200, $"{{\"key\":\"{KeyHex}\",\"salt\":\"{SaltHex}\",\"hashAlg\":\"SHA1\"}}");
            }
            else if (command.StartsWith("jdev/sys/gettoken/", StringComparison.Ordinal))
            {
                var hash = command.Split('/')[3];
                TokenHashValid = string.Equals(hash, ExpectedTokenHash(), StringComparison.OrdinalIgnoreCase);
                response = Response(command, 200, "{\"token\":\"made-up-token\",\"key\":\"6B6579\",\"validUntil\":1000,\"tokenRights\":4,\"unsecurePass\":false}");
            }
            else if (command.StartsWith("jdev/sps/enablebinstatusupdate", StringComparison.Ordinal))
            {
                response = Response(command, 200, "\"1\"");
            }
            else if (command.StartsWith("jdev/sps/io/", StringComparison.Ordinal))
            {
                response = Response(command, WriteResponseCode, "\"1\"");
            }
            else
            {
                response = Response(command, 404, "\"\"");
            }

            // fenc = the response is encrypted as well, enc = plain response
            await SendTextAsync(isFenc ? Convert.ToBase64String(Encrypt(response)) : response);
        }

        public string ExpectedTokenHash()
        {
            var pwHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes($"{Password}:{SaltHex}")));
            using var hmac = new HMACSHA1(Convert.FromHexString(KeyHex));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{User}:{pwHash}")));
        }

        private static string Response(string control, int code, string valueJson)
        {
            return $"{{\"LL\":{{\"control\":{JsonSerializer.Serialize(control)},\"value\":{valueJson},\"Code\":\"{code}\"}}}}";
        }

        private byte[] Encrypt(string plain)
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var padded = new byte[(bytes.Length + 15) / 16 * 16];
            padded.AsSpan().Fill((byte)' ');
            bytes.CopyTo(padded, 0);

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            return aes.EncryptCbc(padded, _aesIv, PaddingMode.None);
        }

        private string Decrypt(byte[] cipher)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            return Encoding.UTF8.GetString(aes.DecryptCbc(cipher, _aesIv, PaddingMode.None));
        }

        private Task SendTextAsync(string text)
        {
            var socket = _socket;
            return SendLocked(() => socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancellationToken.None));
        }

        /// <summary>sends a binary websocket frame (header or payload) to the driver</summary>
        public Task SendBinaryAsync(byte[] data)
        {
            var socket = _socket ?? throw new InvalidOperationException("no websocket connected");
            return SendLocked(() => socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None));
        }

        public Task CloseWebSocketAsync()
        {
            var socket = _socket ?? throw new InvalidOperationException("no websocket connected");
            return SendLocked(() => socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "miniserver restart", CancellationToken.None));
        }

        private async Task SendLocked(Func<Task> send)
        {
            await _sendLock.WaitAsync();
            try
            {
                await send();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>what the miniserver saw, for assertion messages</summary>
        public string Diagnostics()
        {
            return $"http: [{string.Join(", ", HttpRequests.Select(a => a.RawTarget))}] ws: [{string.Join(", ", WebSocketMessages.Select(a => a.Length > 40 ? a.Substring(0, 40) : a))}] commands: [{string.Join(", ", DecryptedCommands.Select(a => a.Length > 40 ? a.Substring(0, 40) : a))}] socket: {_socket?.State} errors: [{string.Join(" | ", Errors)}]";
        }

        public static async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return true;
                }
                await Task.Delay(10);
            }
            return condition();
        }
    }
}
