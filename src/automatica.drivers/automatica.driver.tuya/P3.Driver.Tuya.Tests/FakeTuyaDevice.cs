using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.Tuya.Driver;

namespace P3.Driver.Tuya.Tests
{
    /// <summary>
    /// A Tuya plug on 127.0.0.1 as the real LSC plug answers: 3.4 session negotiation, status answers with return code,
    /// a status frame (cmd 8) after every control command. Takes one session at a time like the device.
    /// </summary>
    internal sealed class FakeTuyaDevice : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private readonly string _version;
        private readonly byte[] _localKey;
        private readonly Task _accept;
        private TcpClient _current;
        private NetworkStream _stream;
        private byte[] _key;
        private uint _seq;

        public FakeTuyaDevice(string version, string localKey)
        {
            _version = version;
            _localKey = Encoding.UTF8.GetBytes(localKey);
            _listener.Start();
            _accept = Task.Run(AcceptLoopAsync);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public Dictionary<string, object> Dps { get; } = new()
        {
            ["1"] = true, ["9"] = 0, ["17"] = 3, ["18"] = 37, ["19"] = 48, ["20"] = 2286, ["21"] = 1, ["26"] = 0, ["38"] = "off", ["40"] = "relay"
        };

        /// <summary>
        /// Plain payloads of the frames the client sent, by command
        /// </summary>
        public ConcurrentQueue<(uint Cmd, string Payload)> Received { get; } = new();

        public int Sessions { get; private set; }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    Sessions++;
                    _ = Task.Run(() => ServeAsync(client));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task ServeAsync(TcpClient client)
        {
            using (client)
            {
                _current = client;
                _stream = client.GetStream();
                _key = _localKey;
                byte[] localNonce = null;
                var remoteNonce = Encoding.ASCII.GetBytes("fedcba9876543210");
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var header = new byte[16];
                        await _stream.ReadExactlyAsync(header, _cts.Token);
                        var rest = new byte[TuyaProtocol.RestLength(header)];
                        await _stream.ReadExactlyAsync(rest, _cts.Token);
                        var frame = TuyaProtocol.Decode(_version, _key, header, rest);
                        var text = Encoding.UTF8.GetString(frame.Payload);
                        Received.Enqueue((frame.Cmd, text));

                        switch (frame.Cmd)
                        {
                            case TuyaProtocol.SessKeyNegStart:
                                localNonce = frame.Payload;
                                await SendAsync(TuyaProtocol.SessKeyNegResp, remoteNonce.Concat(HMACSHA256.HashData(_localKey, localNonce)).ToArray());
                                break;
                            case TuyaProtocol.SessKeyNegFinish:
                                if (!frame.Payload.SequenceEqual(HMACSHA256.HashData(_localKey, remoteNonce)))
                                {
                                    return;
                                }
                                _key = TuyaProtocol.SessionKey(_localKey, localNonce, remoteNonce);
                                break;
                            case TuyaProtocol.DpQuery:
                            case TuyaProtocol.DpQueryNew:
                                await SendAsync(frame.Cmd, Json(new Dictionary<string, object> { ["dps"] = Dps }));
                                break;
                            case TuyaProtocol.Control:
                            case TuyaProtocol.ControlNew:
                                using (var doc = JsonDocument.Parse(text))
                                {
                                    var root = doc.RootElement;
                                    var dps = (_version == "3.4" ? root.GetProperty("data") : root).GetProperty("dps");
                                    var changed = new Dictionary<string, object>();
                                    foreach (var p in dps.EnumerateObject())
                                    {
                                        object value = p.Value.ValueKind switch
                                        {
                                            JsonValueKind.True => true,
                                            JsonValueKind.False => false,
                                            JsonValueKind.Number => p.Value.GetInt64(),
                                            _ => p.Value.GetString()
                                        };
                                        Dps[p.Name] = changed[p.Name] = value;
                                    }
                                    await SendAsync(frame.Cmd, Array.Empty<byte>());
                                    await PushAsync(changed);
                                }
                                break;
                        }
                    }
                }
                catch (Exception)
                {
                    // session ended
                }
            }
        }

        /// <summary>
        /// A status the device sends by itself (button pressed)
        /// </summary>
        public Task PushAsync(Dictionary<string, object> dps) => _version == "3.4"
            ? SendAsync(TuyaProtocol.Status, Json(new Dictionary<string, object> { ["protocol"] = 4, ["t"] = 1, ["data"] = new Dictionary<string, object> { ["dps"] = dps } }), header: true)
            : SendAsync(TuyaProtocol.Status, Json(new Dictionary<string, object> { ["devId"] = "x", ["dps"] = dps, ["t"] = 1 }), header: true);

        public void DropSession() => _current?.Dispose();

        private static byte[] Json(object value) => JsonSerializer.SerializeToUtf8Bytes(value);

        /// <summary>
        /// A frame of the device: return code 0 in front of the encrypted payload
        /// </summary>
        private async Task SendAsync(uint cmd, byte[] payload, bool header = false)
        {
            var version = Encoding.ASCII.GetBytes(_version).Concat(new byte[12]).ToArray();
            byte[] body;
            if (_version == "3.4")
            {
                body = payload.Length == 0 ? payload : TuyaProtocol.Encrypt(_key, header ? version.Concat(payload).ToArray() : payload);
            }
            else
            {
                body = TuyaProtocol.Encrypt(_key, payload);
                if (header)
                {
                    body = version.Concat(body).ToArray();
                }
            }
            var data = new byte[4].Concat(body).ToArray();
            var trailer = _version == "3.4" ? 32 : 4;
            var head = new byte[16];
            BinaryPrimitives.WriteUInt32BigEndian(head, TuyaProtocol.Prefix);
            BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(4), ++_seq);
            BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(8), cmd);
            BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(12), (uint)(data.Length + trailer + 4));
            var signed = head.Concat(data).ToArray();
            var check = _version == "3.4" ? HMACSHA256.HashData(_key, signed) : BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(TuyaProtocol.Crc32(signed)));
            var suffix = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(suffix, TuyaProtocol.Suffix);
            var frame = signed.Concat(check).Concat(suffix).ToArray();
            await _stream.WriteAsync(frame, _cts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            _current?.Dispose();
            try
            {
                await _accept;
            }
            catch
            {
                // stopped
            }
            _cts.Dispose();
        }
    }
}
