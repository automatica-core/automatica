using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Tuya.Driver
{
    /// <summary>
    /// One local TCP session (port 6668) with a Tuya device. A device accepts one session at a time. Status answers and
    /// the status the device sends by itself (switched in the app or by the button) arrive through <see cref="DpsReceived"/>.
    /// </summary>
    internal sealed class TuyaClient : IAsyncDisposable
    {
        public const int Port = 6668;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        private readonly string _host;
        private readonly int _port;
        private readonly string _deviceId;
        private readonly byte[] _localKey;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly CancellationTokenSource _cts = new();
        private TcpClient _tcp;
        private NetworkStream _stream;
        private byte[] _key;
        private uint _seq;
        private Task _receiveLoop;

        public TuyaClient(string host, string deviceId, string localKey, string version, int port = Port)
        {
            _host = host;
            _port = port;
            _deviceId = deviceId;
            _localKey = Encoding.UTF8.GetBytes(localKey ?? "");
            Version = version;
            if (_localKey.Length != 16)
            {
                throw new ArgumentException("the local key must have 16 characters");
            }
        }

        public string Version { get; }

        public bool IsConnected => _receiveLoop is { IsCompleted: false };

        /// <summary>
        /// Data point values by DP id, from a status answer or a status the device sent by itself
        /// </summary>
        public event Action<IReadOnlyDictionary<string, JsonElement>> DpsReceived;

        /// <summary>
        /// The session ended (closed by the device, network error, bad frame)
        /// </summary>
        public event Action<Exception> Disconnected;

        public async Task ConnectAsync(CancellationToken token = default)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);
            timeout.CancelAfter(Timeout);
            _tcp = new TcpClient { NoDelay = true };
            await _tcp.ConnectAsync(_host, _port, timeout.Token);
            _stream = _tcp.GetStream();
            _key = _localKey;
            if (Version == "3.4")
            {
                await NegotiateAsync(timeout.Token);
            }
            _receiveLoop = Task.Run(ReceiveLoopAsync);
        }

        /// <summary>
        /// 3.4: local nonce → device nonce + HMAC(local key, local nonce) → HMAC(local key, device nonce); session key from both nonces
        /// </summary>
        private async Task NegotiateAsync(CancellationToken token)
        {
            var localNonce = Encoding.ASCII.GetBytes(Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant());
            await SendAsync(TuyaProtocol.SessKeyNegStart, localNonce, token);
            TuyaFrame answer;
            try
            {
                answer = await ReadFrameAsync(token);
            }
            catch (Exception ex) when (ex is EndOfStreamException or IOException || ex is OperationCanceledException && !_cts.IsCancellationRequested)
            {
                // the device drops a session start it cannot decrypt
                throw new TuyaProtocolException($"no answer to the key exchange (wrong local key?): {ex.Message}");
            }
            if (answer.Cmd != TuyaProtocol.SessKeyNegResp || answer.Payload.Length < 48)
            {
                throw new TuyaProtocolException($"unexpected answer {answer.Cmd} to the session start");
            }
            var remoteNonce = answer.Payload[..16];
            if (!CryptographicOperations.FixedTimeEquals(answer.Payload.AsSpan(16, 32), HMACSHA256.HashData(_localKey, localNonce)))
            {
                throw new TuyaProtocolException("the device answered with another key (wrong local key?)");
            }
            await SendAsync(TuyaProtocol.SessKeyNegFinish, HMACSHA256.HashData(_localKey, remoteNonce), token);
            _key = TuyaProtocol.SessionKey(_localKey, localNonce, remoteNonce);
        }

        /// <summary>
        /// Asks for all data points, the answer arrives through <see cref="DpsReceived"/>
        /// </summary>
        public Task QueryAsync(CancellationToken token = default)
        {
            if (Version == "3.4")
            {
                return SendAsync(TuyaProtocol.DpQueryNew, "{}"u8.ToArray(), token);
            }
            return SendJsonAsync(TuyaProtocol.DpQuery, new Dictionary<string, object> { ["gwId"] = _deviceId, ["devId"] = _deviceId, ["uid"] = _deviceId, ["t"] = Now() }, token);
        }

        /// <summary>
        /// Sets data points, e.g. {"1": true}; the device confirms with its new status
        /// </summary>
        public Task SetAsync(IReadOnlyDictionary<string, object> dps, CancellationToken token = default)
        {
            if (Version == "3.4")
            {
                return SendJsonAsync(TuyaProtocol.ControlNew, new Dictionary<string, object> { ["protocol"] = 5, ["t"] = Now(), ["data"] = new Dictionary<string, object> { ["dps"] = dps } }, token);
            }
            return SendJsonAsync(TuyaProtocol.Control, new Dictionary<string, object> { ["devId"] = _deviceId, ["uid"] = _deviceId, ["t"] = Now(), ["dps"] = dps }, token);
        }

        private static string Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        private Task SendJsonAsync(uint cmd, object payload, CancellationToken token) => SendAsync(cmd, JsonSerializer.SerializeToUtf8Bytes(payload), token);

        private async Task SendAsync(uint cmd, byte[] payload, CancellationToken token)
        {
            if (_stream == null)
            {
                throw new IOException("not connected");
            }
            await _sendLock.WaitAsync(token);
            try
            {
                var frame = TuyaProtocol.Encode(Version, _key, ++_seq, cmd, payload);
                await _stream.WriteAsync(frame, token);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private async Task<TuyaFrame> ReadFrameAsync(CancellationToken token)
        {
            var header = new byte[TuyaProtocol.HeaderLength];
            await _stream.ReadExactlyAsync(header, token);
            var rest = new byte[TuyaProtocol.RestLength(header)];
            await _stream.ReadExactlyAsync(rest, token);
            return TuyaProtocol.Decode(Version, _key, header, rest);
        }

        private async Task ReceiveLoopAsync()
        {
            Exception error = null;
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var frame = await ReadFrameAsync(_cts.Token);
                    if (Dps(frame.Payload) is { } dps)
                    {
                        DpsReceived?.Invoke(dps);
                    }
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                error = ex;
            }
            _tcp?.Dispose();
            if (!_cts.IsCancellationRequested)
            {
                Disconnected?.Invoke(error ?? new EndOfStreamException("closed by the device"));
            }
        }

        /// <summary>
        /// The "dps" of a status: {"dps":{...}} (3.3, 3.4 query answer) or {"data":{"dps":{...}}} (3.4 status sent by the device)
        /// </summary>
        internal static IReadOnlyDictionary<string, JsonElement> Dps(byte[] payload)
        {
            if (payload.Length == 0 || payload[0] != (byte)'{')
            {
                return null;
            }
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    root = data;
                }
                if (!root.TryGetProperty("dps", out var dps) || dps.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }
                var result = new Dictionary<string, JsonElement>();
                foreach (var p in dps.EnumerateObject())
                {
                    result[p.Name] = p.Value.Clone();
                }
                return result;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _tcp?.Dispose();
            if (_receiveLoop != null)
            {
                try
                {
                    await _receiveLoop;
                }
                catch
                {
                    // ended with the connection
                }
            }
            _cts.Dispose();
        }
    }
}
