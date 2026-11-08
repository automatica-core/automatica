#nullable enable annotations
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.Rpc
{
    public class ShellyRpcException : Exception
    {
        public int Code { get; }

        public ShellyRpcException(int code, string message) : base(message)
        {
            Code = code;
        }
    }

    /// <summary>
    /// A Gen2+ device notification: NotifyStatus (partial status), NotifyFullStatus (complete status) or NotifyEvent (events).
    /// </summary>
    public delegate void ShellyNotificationHandler(string method, JObject parameters);

    /// <summary>
    /// JSON-RPC client of a Gen2 / Gen3 / Gen4 device.
    /// Calls go over HTTP POST /rpc (digest authentication with the "admin" user), live updates come over the
    /// websocket ws://{address}/rpc: after the first (authenticated) request on the socket the device pushes its notifications to it.
    /// The websocket reconnects on its own until <see cref="DisconnectAsync"/> or dispose.
    /// </summary>
    public sealed class ShellyRpcClient : IDisposable
    {
        private readonly string _address;
        private readonly string? _password;
        private readonly ILogger _logger;
        private readonly ITelegramMonitorInstance _monitor;
        private readonly TimeSpan _timeout;
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _authLock = new();

        private ShellyDigestChallenge? _challenge;
        private int _nc;
        private string _cnonce = ShellyDigestAuth.NewCnonce();
        private int _nextId;

        private CancellationTokenSource? _wsCts;
        private Task? _wsLoop;
        private ClientWebSocket? _ws;
        private bool _disposed;

        public string Source { get; }

        /// <summary>
        /// Wait time before the first websocket reconnect, doubled up to <see cref="MaxReconnectDelay"/>.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

        public event ShellyNotificationHandler? Notification;
        public event Action? WebSocketConnected;
        public event Action? WebSocketDisconnected;

        public bool IsWebSocketConnected => _ws is { State: WebSocketState.Open };

        public ShellyRpcClient(string address, string? password, ILogger logger, ITelegramMonitorInstance? monitor = null, TimeSpan? timeout = null, string? source = null)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            _password = string.IsNullOrEmpty(password) ? null : password;
            _logger = logger;
            _monitor = monitor ?? new EmptyTelegramMonitorInstance();
            _timeout = timeout ?? TimeSpan.FromSeconds(5);
            Source = source ?? $"automatica-{Guid.NewGuid():N}";
            _http = new HttpClient { BaseAddress = new Uri($"http://{address}") };
        }

        public async Task<JToken?> CallAsync(string method, object? parameters = null, CancellationToken token = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var frame = new JObject
            {
                ["id"] = Interlocked.Increment(ref _nextId),
                ["src"] = Source,
                ["method"] = method
            };
            if (parameters != null)
            {
                frame["params"] = parameters as JToken ?? JToken.FromObject(parameters);
            }
            var body = frame.ToString(Formatting.None);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/rpc");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var authorization = CreateAuthorizationHeader();
                if (authorization != null)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", authorization);
                }

                using var timeoutCts = new CancellationTokenSource(_timeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

                await _monitor.NotifyTelegram(TelegramDirection.Output, "self", _address, method, parameters == null ? "" : frame["params"]!.ToString(Formatting.None));

                HttpResponseMessage response;
                try
                {
                    response = await _http.SendAsync(request, linked.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    throw new ShellyRpcException(408, $"{_address} did not answer {method} within {_timeout.TotalSeconds:0.#} s");
                }
                catch (HttpRequestException e)
                {
                    throw new ShellyRpcException(503, $"{_address} is not reachable: {e.Message}");
                }

                using (response)
                {
                    var text = await response.Content.ReadAsStringAsync(linked.Token);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        if (_password == null)
                        {
                            throw new ShellyRpcException(401, $"{_address} requires authentication, set the password of the device");
                        }

                        var header = response.Headers.TryGetValues("WWW-Authenticate", out var values) ? string.Join(", ", values) : null;
                        var challenge = ShellyDigestAuth.ParseChallenge(header);
                        if (challenge == null || attempt > 0)
                        {
                            throw new ShellyRpcException(401, $"{_address} rejected the password");
                        }

                        lock (_authLock)
                        {
                            _challenge = challenge;
                            _nc = 0;
                            _cnonce = ShellyDigestAuth.NewCnonce();
                        }
                        continue;
                    }

                    await _monitor.NotifyTelegram(TelegramDirection.Input, _address, "self", text, method);

                    if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(text))
                    {
                        throw new ShellyRpcException((int)response.StatusCode, $"{_address} answered {method} with HTTP {(int)response.StatusCode}");
                    }

                    JObject json;
                    try
                    {
                        json = JObject.Parse(text);
                    }
                    catch (JsonException)
                    {
                        throw new ShellyRpcException((int)response.StatusCode, $"{_address} answered {method} with no JSON-RPC frame (HTTP {(int)response.StatusCode})");
                    }

                    if (json["error"] is JObject error)
                    {
                        throw new ShellyRpcException(error.Value<int?>("code") ?? -1, $"{method} failed on {_address}: {error.Value<string>("message")}");
                    }

                    return json["result"];
                }
            }

            throw new ShellyRpcException(401, $"{_address} rejected the password");
        }

        private string? CreateAuthorizationHeader()
        {
            lock (_authLock)
            {
                if (_challenge == null || _password == null)
                {
                    return null;
                }

                _nc++;
                return ShellyDigestAuth.CreateAuthorizationHeader(_challenge, _password, "POST", "/rpc", _nc, _cnonce);
            }
        }

        /// <summary>
        /// Starts the websocket (and its reconnect loop). Returns whether the first connect succeeded within the timeout.
        /// </summary>
        public async Task<bool> ConnectAsync(CancellationToken token = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await DisconnectAsync();

            var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _wsCts = new CancellationTokenSource();
            var loopToken = _wsCts.Token;
            _wsLoop = Task.Run(() => WebSocketLoop(first, loopToken), CancellationToken.None);

            try
            {
                return await first.Task.WaitAsync(_timeout + TimeSpan.FromSeconds(1), token);
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            var cts = _wsCts;
            var loop = _wsLoop;
            _wsCts = null;
            _wsLoop = null;

            if (cts == null)
            {
                return;
            }

            await cts.CancelAsync();
            var ws = _ws;
            try
            {
                ws?.Abort();
            }
            catch
            {
                // ignored, the socket is being thrown away
            }

            if (loop != null)
            {
                try
                {
                    await loop.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch
                {
                    // the loop ends with the cancellation
                }
            }
            cts.Dispose();
        }

        private async Task WebSocketLoop(TaskCompletionSource<bool> first, CancellationToken token)
        {
            var delay = ReconnectDelay;
            while (!token.IsCancellationRequested)
            {
                var connected = false;
                try
                {
                    using var ws = new ClientWebSocket();
                    ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    using (var connectTimeout = new CancellationTokenSource(_timeout))
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(connectTimeout.Token, token))
                    {
                        await ws.ConnectAsync(new Uri($"ws://{_address}/rpc"), linked.Token);
                    }

                    _ws = ws;
                    connected = true;
                    delay = ReconnectDelay;
                    _logger.LogDebug($"Shelly websocket {_address} connected");

                    var helloId = Interlocked.Increment(ref _nextId);
                    await SendHello(ws, helloId, null, token);
                    first.TrySetResult(true);
                    WebSocketConnected?.Invoke();

                    await ReceiveLoop(ws, helloId, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogDebug($"Shelly websocket {_address}: {e.Message}");
                }
                finally
                {
                    _ws = null;
                    if (connected)
                    {
                        WebSocketDisconnected?.Invoke();
                    }
                }

                first.TrySetResult(false);
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(delay, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReconnectDelay.Ticks));
            }
        }

        private async Task SendHello(ClientWebSocket ws, int id, JObject? auth, CancellationToken token)
        {
            // any request registers this socket (by "src") for the notifications of the device
            var frame = new JObject { ["id"] = id, ["src"] = Source, ["method"] = "Shelly.GetStatus" };
            if (auth != null)
            {
                frame["auth"] = auth;
            }

            await _sendLock.WaitAsync(token);
            try
            {
                await ws.SendAsync(Encoding.UTF8.GetBytes(frame.ToString(Formatting.None)), WebSocketMessageType.Text, true, token);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task ReceiveLoop(ClientWebSocket ws, int helloId, CancellationToken token)
        {
            var buffer = new byte[16 * 1024];
            var authenticated = false;
            var currentHello = helloId;
            using var message = new System.IO.MemoryStream();

            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.ToArray());
                message.SetLength(0);

                JObject frame;
                try
                {
                    frame = JObject.Parse(text);
                }
                catch (JsonException)
                {
                    _logger.LogDebug($"Shelly websocket {_address}: ignoring a frame that is no JSON");
                    continue;
                }

                await _monitor.NotifyTelegram(TelegramDirection.Input, _address, "self", text, "ws");

                var method = frame.Value<string>("method");
                if (method != null)
                {
                    RaiseNotification(method, frame["params"] as JObject ?? new JObject());
                    continue;
                }

                if (frame["id"]?.Type == JTokenType.Integer && frame.Value<int>("id") == currentHello)
                {
                    if (frame["error"] is JObject error && error.Value<int?>("code") == 401)
                    {
                        if (authenticated || _password == null)
                        {
                            _logger.LogError(_password == null
                                ? $"Shelly {_address} requires authentication, set the password of the device"
                                : $"Shelly {_address} rejected the password on the websocket");
                            continue;
                        }

                        authenticated = true;
                        var auth = CreateWebSocketAuth(error.Value<string>("message"));
                        if (auth != null)
                        {
                            currentHello = Interlocked.Increment(ref _nextId);
                            await SendHello(ws, currentHello, auth, token);
                        }
                        continue;
                    }

                    if (frame["result"] is JObject status)
                    {
                        // the answer to Shelly.GetStatus is the complete status
                        RaiseNotification("NotifyFullStatus", status);
                    }
                }
            }
        }

        private JObject? CreateWebSocketAuth(string? errorMessage)
        {
            try
            {
                var challenge = JObject.Parse(errorMessage ?? "{}");
                var realm = challenge.Value<string>("realm");
                var nonce = challenge.Value<long?>("nonce");
                if (realm == null || nonce == null)
                {
                    return null;
                }

                var cnonce = BitConverter.ToInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)) & long.MaxValue;
                return ShellyDigestAuth.CreateRpcAuth(realm, nonce.Value, _password!, cnonce, challenge.Value<int?>("nc") ?? 1);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void RaiseNotification(string method, JObject parameters)
        {
            try
            {
                Notification?.Invoke(method, parameters);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error handling {method} of {_address}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                DisconnectAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // ignored on dispose
            }
            _http.Dispose();
            _sendLock.Dispose();
        }
    }
}
