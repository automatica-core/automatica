using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap.TlvData;
using SecureRemotePassword;

namespace P3.Driver.HomeKit.UnitTests.Integration
{
    /// <summary>
    /// Long-term identity of a test controller (iOS device): pairing identifier and Ed25519 key pair.
    /// Uses NSec's standard Ed25519 / X25519 implementations, independent of the accessory's Chaos.NaCl / P3.Elliptic code.
    /// </summary>
    internal sealed class HapControllerIdentity : IDisposable
    {
        public HapControllerIdentity(string id)
        {
            Id = id;
            SigningKey = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
            PublicKey = SigningKey.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        }

        public string Id { get; }
        public Key SigningKey { get; }
        public byte[] PublicKey { get; }
        public string PublicKeyHex => Convert.ToHexString(PublicKey).ToLowerInvariant();

        /// <summary>
        /// Accessory long-term public key learned in pair-setup M6 (or copied from another identity)
        /// </summary>
        public byte[] AccessoryLtpk { get; set; }
        public string AccessoryPairingId { get; set; }

        public byte[] Sign(byte[] data)
        {
            return SignatureAlgorithm.Ed25519.Sign(SigningKey, data);
        }

        public void Dispose()
        {
            SigningKey.Dispose();
        }
    }

    internal sealed class HapHttpMessage
    {
        public string StartLine { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string BodyText => Encoding.UTF8.GetString(Body);
        public bool IsEvent => StartLine.StartsWith("EVENT/1.0", StringComparison.Ordinal);
        public string ContentType => Headers.TryGetValue("Content-Type", out var value) ? value : null;
    }

    internal sealed class HapPairSetupResult
    {
        public bool Success { get; set; }
        public int FailedState { get; set; }
        public ErrorCodes? Error { get; set; }
    }

    /// <summary>
    /// One controller connection: plain HTTP until pair-verify succeeded, then ChaCha20-Poly1305 framed (HAP 6.5.2).
    /// </summary>
    internal sealed class HapTestConnection : IDisposable
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        private const string SetupUser = "Pair-Setup";

        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly TlvParser _tlv = new TlvParser(NullLogger.Instance);

        private byte[] _raw = Array.Empty<byte>();
        private byte[] _plain = Array.Empty<byte>();
        private byte[] _controllerToAccessoryKey;
        private byte[] _accessoryToControllerKey;
        private long _outCounter;
        private long _inCounter;

        public bool IsEncrypted { get; private set; }

        private HapTestConnection(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true;
            _stream = client.GetStream();
        }

        public static async Task<HapTestConnection> ConnectAsync(int port)
        {
            var client = new TcpClient(AddressFamily.InterNetwork);
            using (var cts = new CancellationTokenSource(Timeout))
            {
                await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
            }

            return new HapTestConnection(client);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        #region http

        public static byte[] BuildRequest(string method, string path, byte[] body, string contentType)
        {
            body = body ?? Array.Empty<byte>();
            var head = new StringBuilder();
            head.Append($"{method} /{path} HTTP/1.1\r\n");
            head.Append("Host: bridge\r\n");
            if (contentType != null)
            {
                head.Append($"Content-Type: {contentType}\r\n");
            }
            head.Append($"Content-Length: {body.Length}\r\n\r\n");
            return Encoding.ASCII.GetBytes(head.ToString()).Concat(body).ToArray();
        }

        public async Task<HapHttpMessage> SendAsync(string method, string path, byte[] body = null, string contentType = null)
        {
            await WriteRequestAsync(BuildRequest(method, path, body, contentType));
            return await ReadMessageAsync();
        }

        public Task<HapHttpMessage> SendJsonAsync(string method, string path, string json)
        {
            return SendAsync(method, path, Encoding.UTF8.GetBytes(json), "application/hap+json");
        }

        public async Task WriteRequestAsync(byte[] request)
        {
            await WriteRawAsync(IsEncrypted ? Encrypt(request) : request);
        }

        public async Task WriteRawAsync(byte[] bytes)
        {
            await _stream.WriteAsync(bytes, 0, bytes.Length);
            await _stream.FlushAsync();
        }

        /// <summary>
        /// Reads the next HTTP response or EVENT message.
        /// </summary>
        public async Task<HapHttpMessage> ReadMessageAsync()
        {
            using (var cts = new CancellationTokenSource(Timeout))
            {
                while (true)
                {
                    if (IsEncrypted)
                    {
                        DecryptAvailableFrames();
                        if (TryParse(ref _plain, out var message))
                        {
                            return message;
                        }
                    }
                    else if (TryParse(ref _raw, out var message))
                    {
                        return message;
                    }

                    var buffer = new byte[8192];
                    var read = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    if (read == 0)
                    {
                        throw new IOException("The accessory closed the connection");
                    }

                    _raw = _raw.Concat(buffer.Take(read)).ToArray();
                }
            }
        }

        /// <summary>
        /// True if the accessory closes the connection before the timeout (data that arrives meanwhile is discarded).
        /// </summary>
        public async Task<bool> WaitForCloseAsync(TimeSpan? timeout = null)
        {
            using (var cts = new CancellationTokenSource(timeout ?? Timeout))
            {
                try
                {
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var read = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (read == 0)
                        {
                            return true;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return true;
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
        }

        private static bool TryParse(ref byte[] buffer, out HapHttpMessage message)
        {
            message = null;
            var headerEnd = IndexOf(buffer, new byte[] { 13, 10, 13, 10 });
            if (headerEnd < 0)
            {
                return false;
            }

            var lines = Encoding.ASCII.GetString(buffer, 0, headerEnd).Split("\r\n");
            var result = new HapHttpMessage { StartLine = lines[0] };
            result.StatusCode = int.Parse(lines[0].Split(' ')[1]);
            foreach (var line in lines.Skip(1))
            {
                var index = line.IndexOf(':');
                result.Headers[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
            }

            var length = result.Headers.TryGetValue("Content-Length", out var value) ? int.Parse(value) : 0;
            var total = headerEnd + 4 + length;
            if (buffer.Length < total)
            {
                return false;
            }

            result.Body = buffer.Skip(headerEnd + 4).Take(length).ToArray();
            buffer = buffer.Skip(total).ToArray();
            message = result;
            return true;
        }

        private static int IndexOf(byte[] data, byte[] pattern)
        {
            for (var i = 0; i <= data.Length - pattern.Length; i++)
            {
                var match = true;
                for (var j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region session encryption

        private static byte[] Nonce(long counter)
        {
            return new byte[4].Concat(BitConverter.GetBytes(counter)).ToArray();
        }

        private static byte[] Nonce(string label)
        {
            return new byte[4].Concat(Encoding.UTF8.GetBytes(label)).ToArray();
        }

        /// <summary>
        /// Splits <paramref name="plain"/> into frames of at most <paramref name="frameSize"/> bytes and encrypts them with the controller-to-accessory key.
        /// </summary>
        public byte[] Encrypt(byte[] plain, int frameSize = 1024)
        {
            var result = new List<byte>();
            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, _controllerToAccessoryKey, KeyBlobFormat.RawSymmetricKey))
            {
                for (var offset = 0; offset < plain.Length; offset += frameSize)
                {
                    var chunk = plain.Skip(offset).Take(frameSize).ToArray();
                    result.AddRange(EncryptFrame(key, chunk, (ushort)chunk.Length));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Encrypts one frame with an arbitrary length prefix (used to send frames the accessory must reject).
        /// </summary>
        public byte[] EncryptSingleFrame(byte[] plain, ushort declaredLength)
        {
            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, _controllerToAccessoryKey, KeyBlobFormat.RawSymmetricKey))
            {
                return EncryptFrame(key, plain, declaredLength);
            }
        }

        private byte[] EncryptFrame(Key key, byte[] chunk, ushort declaredLength)
        {
            var aad = BitConverter.GetBytes(declaredLength);
            var cipher = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, Nonce(_outCounter++), aad, chunk);
            return aad.Concat(cipher).ToArray();
        }

        private void DecryptAvailableFrames()
        {
            while (_raw.Length >= 2)
            {
                var length = BitConverter.ToUInt16(_raw, 0);
                if (_raw.Length < 2 + length + 16)
                {
                    return;
                }

                var aad = _raw.Take(2).ToArray();
                var cipher = _raw.Skip(2).Take(length + 16).ToArray();
                using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, _accessoryToControllerKey, KeyBlobFormat.RawSymmetricKey))
                {
                    var plain = AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, Nonce(_inCounter++), aad, cipher);
                    if (plain == null)
                    {
                        throw new InvalidDataException("Could not decrypt accessory frame");
                    }

                    _plain = _plain.Concat(plain).ToArray();
                }

                _raw = _raw.Skip(2 + length + 16).ToArray();
            }
        }

        #endregion

        #region tlv

        public async Task<Tlv> PostTlvAsync(string path, Tlv request)
        {
            var response = await SendAsync("POST", path, _tlv.Serialize(request), "application/pairing+tlv8");
            if (response.StatusCode != 200)
            {
                throw new InvalidDataException($"{path} answered {response.StartLine}: {response.BodyText}");
            }

            return _tlv.Parse(response.Body);
        }

        private static ErrorCodes? ErrorOf(Tlv tlv)
        {
            return tlv.HasType(Constants.Error) ? (ErrorCodes?)tlv.GetTypeAsInt(Constants.Error) : null;
        }

        #endregion

        #region pair-setup (HAP 5.6)

        private static byte[] Hkdf(byte[] inputKey, string salt, string info)
        {
            using (var secret = SharedSecret.Import(inputKey, SharedSecretBlobFormat.RawSharedSecret))
            {
                return KeyDerivationAlgorithm.HkdfSha512.DeriveBytes(secret, Encoding.UTF8.GetBytes(salt), Encoding.UTF8.GetBytes(info), 32);
            }
        }

        public async Task<Tlv> PairSetupM1Async()
        {
            var m1 = new Tlv();
            m1.AddType(Constants.Method, 0);
            m1.AddType(Constants.State, 1);
            return await PostTlvAsync("pair-setup", m1);
        }

        /// <summary>
        /// Runs the whole SRP pair-setup. On success the accessory LTPK and pairing id are stored in <paramref name="identity"/>.
        /// </summary>
        public async Task<HapPairSetupResult> PairSetupAsync(HapControllerIdentity identity, string setupCode)
        {
            var m2 = await PairSetupM1Async();
            if (ErrorOf(m2).HasValue)
            {
                return new HapPairSetupResult { FailedState = 2, Error = ErrorOf(m2) };
            }

            var srp = new SrpClient(SrpParameters.Create3072<SHA512>());
            var salt = SrpInteger.FromByteArray(m2.GetType(Constants.Salt)).ToHex();
            var serverPublic = SrpInteger.FromByteArray(m2.GetType(Constants.PublicKey)).ToHex();

            var privateKey = srp.DerivePrivateKey(salt, SetupUser, setupCode);
            var ephemeral = srp.GenerateEphemeral();
            var session = srp.DeriveSession(ephemeral.Secret, serverPublic, salt, SetupUser, privateKey);

            var m3 = new Tlv();
            m3.AddType(Constants.State, 3);
            m3.AddType(Constants.PublicKey, SrpInteger.FromHex(ephemeral.Public).ToByteArray());
            m3.AddType(Constants.Proof, SrpInteger.FromHex(session.Proof).ToByteArray());
            var m4 = await PostTlvAsync("pair-setup", m3);
            if (ErrorOf(m4).HasValue)
            {
                return new HapPairSetupResult { FailedState = 4, Error = ErrorOf(m4) };
            }

            // throws if the accessory does not know the setup code
            srp.VerifySession(ephemeral.Public, session, SrpInteger.FromByteArray(m4.GetType(Constants.Proof)).ToHex());

            var sessionKey = SrpInteger.FromHex(session.Key).ToByteArray();
            var encryptKey = Hkdf(sessionKey, "Pair-Setup-Encrypt-Salt", "Pair-Setup-Encrypt-Info");
            var controllerX = Hkdf(sessionKey, "Pair-Setup-Controller-Sign-Salt", "Pair-Setup-Controller-Sign-Info");

            var idBytes = Encoding.UTF8.GetBytes(identity.Id);
            var sub = new Tlv();
            sub.AddType(Constants.Identifier, idBytes);
            sub.AddType(Constants.PublicKey, identity.PublicKey);
            sub.AddType(Constants.Signature, identity.Sign(controllerX.Concat(idBytes).Concat(identity.PublicKey).ToArray()));

            byte[] encrypted;
            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, encryptKey, KeyBlobFormat.RawSymmetricKey))
            {
                encrypted = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, Nonce("PS-Msg05"), Array.Empty<byte>(), _tlv.Serialize(sub));
            }

            var m5 = new Tlv();
            m5.AddType(Constants.State, 5);
            m5.AddType(Constants.EncryptedData, encrypted);
            var m6 = await PostTlvAsync("pair-setup", m5);
            if (ErrorOf(m6).HasValue)
            {
                return new HapPairSetupResult { FailedState = 6, Error = ErrorOf(m6) };
            }

            byte[] plain;
            using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, encryptKey, KeyBlobFormat.RawSymmetricKey))
            {
                plain = AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, Nonce("PS-Msg06"), Array.Empty<byte>(), m6.GetType(Constants.EncryptedData));
            }

            if (plain == null)
            {
                throw new InvalidDataException("Could not decrypt M6");
            }

            var accessory = _tlv.Parse(plain);
            var accessoryId = accessory.GetType(Constants.Identifier);
            var accessoryLtpk = accessory.GetType(Constants.PublicKey);
            var accessoryX = Hkdf(sessionKey, "Pair-Setup-Accessory-Sign-Salt", "Pair-Setup-Accessory-Sign-Info");

            var accessoryKey = NSec.Cryptography.PublicKey.Import(SignatureAlgorithm.Ed25519, accessoryLtpk, KeyBlobFormat.RawPublicKey);
            if (!SignatureAlgorithm.Ed25519.Verify(accessoryKey, accessoryX.Concat(accessoryId).Concat(accessoryLtpk).ToArray(), accessory.GetType(Constants.Signature)))
            {
                throw new InvalidDataException("The accessory signature in M6 is invalid");
            }

            identity.AccessoryLtpk = accessoryLtpk;
            identity.AccessoryPairingId = Encoding.UTF8.GetString(accessoryId);

            return new HapPairSetupResult { Success = true };
        }

        #endregion

        #region pair-verify (HAP 5.7)

        /// <summary>
        /// Runs pair-verify; on success the connection switches to encrypted frames. Returns the TLV error of the accessory, null on success.
        /// </summary>
        public async Task<ErrorCodes?> PairVerifyAsync(HapControllerIdentity identity)
        {
            using (var ephemeral = Key.Create(KeyAgreementAlgorithm.X25519))
            {
                var controllerPublic = ephemeral.PublicKey.Export(KeyBlobFormat.RawPublicKey);

                var m1 = new Tlv();
                m1.AddType(Constants.State, 1);
                m1.AddType(Constants.PublicKey, controllerPublic);
                var m2 = await PostTlvAsync("pair-verify", m1);
                if (ErrorOf(m2).HasValue)
                {
                    return ErrorOf(m2);
                }

                var accessoryPublic = m2.GetType(Constants.PublicKey);
                byte[] shared;
                using (var secret = KeyAgreementAlgorithm.X25519.Agree(ephemeral,
                           NSec.Cryptography.PublicKey.Import(KeyAgreementAlgorithm.X25519, accessoryPublic, KeyBlobFormat.RawPublicKey)))
                {
                    if (secret == null)
                    {
                        throw new InvalidDataException("X25519 agreement failed");
                    }

                    var verifyKey = KeyDerivationAlgorithm.HkdfSha512.DeriveBytes(secret, Encoding.UTF8.GetBytes("Pair-Verify-Encrypt-Salt"), Encoding.UTF8.GetBytes("Pair-Verify-Encrypt-Info"), 32);
                    _controllerToAccessoryKey = KeyDerivationAlgorithm.HkdfSha512.DeriveBytes(secret, Encoding.UTF8.GetBytes("Control-Salt"), Encoding.UTF8.GetBytes("Control-Write-Encryption-Key"), 32);
                    _accessoryToControllerKey = KeyDerivationAlgorithm.HkdfSha512.DeriveBytes(secret, Encoding.UTF8.GetBytes("Control-Salt"), Encoding.UTF8.GetBytes("Control-Read-Encryption-Key"), 32);
                    shared = verifyKey;
                }

                byte[] plain;
                using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, shared, KeyBlobFormat.RawSymmetricKey))
                {
                    plain = AeadAlgorithm.ChaCha20Poly1305.Decrypt(key, Nonce("PV-Msg02"), Array.Empty<byte>(), m2.GetType(Constants.EncryptedData));
                }

                if (plain == null)
                {
                    throw new InvalidDataException("Could not decrypt pair-verify M2");
                }

                var sub = _tlv.Parse(plain);
                var accessoryId = sub.GetType(Constants.Identifier);
                if (identity.AccessoryLtpk != null)
                {
                    var accessoryKey = NSec.Cryptography.PublicKey.Import(SignatureAlgorithm.Ed25519, identity.AccessoryLtpk, KeyBlobFormat.RawPublicKey);
                    if (!SignatureAlgorithm.Ed25519.Verify(accessoryKey, accessoryPublic.Concat(accessoryId).Concat(controllerPublic).ToArray(), sub.GetType(Constants.Signature)))
                    {
                        throw new InvalidDataException("The accessory signature in pair-verify M2 is invalid");
                    }
                }

                var idBytes = Encoding.UTF8.GetBytes(identity.Id);
                var m3Sub = new Tlv();
                m3Sub.AddType(Constants.Identifier, idBytes);
                m3Sub.AddType(Constants.Signature, identity.Sign(controllerPublic.Concat(idBytes).Concat(accessoryPublic).ToArray()));

                byte[] encrypted;
                using (var key = Key.Import(AeadAlgorithm.ChaCha20Poly1305, shared, KeyBlobFormat.RawSymmetricKey))
                {
                    encrypted = AeadAlgorithm.ChaCha20Poly1305.Encrypt(key, Nonce("PV-Msg03"), Array.Empty<byte>(), _tlv.Serialize(m3Sub));
                }

                var m3 = new Tlv();
                m3.AddType(Constants.State, 3);
                m3.AddType(Constants.EncryptedData, encrypted);
                var m4 = await PostTlvAsync("pair-verify", m3);
                if (ErrorOf(m4).HasValue)
                {
                    return ErrorOf(m4);
                }

                IsEncrypted = true;
                _outCounter = 0;
                _inCounter = 0;
                return null;
            }
        }

        #endregion

        #region pairings (HAP 5.10 - 5.12)

        public Task<Tlv> AddPairingAsync(string identifier, byte[] publicKey, bool admin)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.Method, 3);
            tlv.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(identifier));
            tlv.AddType(Constants.PublicKey, publicKey);
            tlv.AddType(Constants.Permissions, admin ? 1 : 0);
            return PostTlvAsync("pairings", tlv);
        }

        public Task<Tlv> RemovePairingAsync(string identifier)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.Method, 4);
            tlv.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(identifier));
            return PostTlvAsync("pairings", tlv);
        }

        public async Task<HapHttpMessage> ListPairingsRawAsync()
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.Method, 5);
            return await SendAsync("POST", "pairings", _tlv.Serialize(tlv), "application/pairing+tlv8");
        }

        #endregion
    }
}
