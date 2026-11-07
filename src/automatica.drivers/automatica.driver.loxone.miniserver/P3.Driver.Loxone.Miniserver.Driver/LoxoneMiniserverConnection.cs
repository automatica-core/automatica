using Automatica.Core.Base.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using P3.Driver.Loxone.Miniserver.Driver.Api;
using P3.Driver.Loxone.Miniserver.Driver.Data;
using P3.Driver.Loxone.Miniserver.Driver.Data.LoxApp;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using P3.Driver.Loxone.Miniserver.Driver.EventArgs;
using PureWebSockets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Loxone.Miniserver.Driver
{
    public enum ConnectionState
    {
        Connecting,
        ExchangeKeys,
        AcquireToken,
        SaveToken,
        SendEnableUpdates,
        Connected,
        Disconnected
    }

    public class LoxoneMiniserverConnection
    {
        public const int DefaultTlsPort = 443;
        private const long MaxBinaryMessageLength = 16 * 1024 * 1024;

        private readonly string password;
        private readonly ILogger _logger;
        private LoxoneWebApi webApi;

        public const string LoxoneUuid = "FF88FFe1-02b4-FFFF-ffffeee000d80cff";
        private byte[] _aesKey;
        private byte[] _aesIv;
        private PureWebSocket _webSocket;
        private readonly object _connectionLock = new object();

        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private byte[] _pubKey;

        private string _salt;

        private bool _encrypted = false;
        private LoxApp3Data _loxData;
        private readonly List<byte> _buffer = new List<byte>();
        private readonly object _lock = new object();

        private readonly Timer _keepAliveTimer;

        public event EventHandler<OnEventTableMessageEventArgs> OnMessage;
        public event EventHandler<System.EventArgs> OnConnectionEstablished;


        public event EventHandler<System.EventArgs> OnConnectionClosed;

        public LoxoneMiniserverConnection(string address, int port, string username, string password, ILogger logger)
            : this(address, port, username, password, logger, false, DefaultTlsPort, true)
        {
        }

        /// <param name="useTls">try https/wss on <paramref name="tlsPort"/> first (Miniserver Gen2)</param>
        /// <param name="allowPlainFallback">fall back to http/ws on <paramref name="port"/> if TLS is not available</param>
        public LoxoneMiniserverConnection(string address, int port, string username, string password, ILogger logger,
            bool useTls, int tlsPort, bool allowPlainFallback)
        {
            _keepAliveTimer = new Timer(TimeSpan.FromMinutes(4).TotalMilliseconds);
            Address = address;
            Port = port;
            TlsPort = tlsPort > 0 ? tlsPort : DefaultTlsPort;
            UseTls = useTls;
            AllowPlainFallback = allowPlainFallback;
            Username = username;
            this.password = password;
            _logger = logger;

            UseTransport(useTls);
            _keepAliveTimer.Elapsed += _keepAliveTimer_Elapsed;
        }

        public bool UseTls { get; }
        public bool AllowPlainFallback { get; }
        public int TlsPort { get; }

        /// <summary>
        /// true if the current connection uses https/wss
        /// </summary>
        public bool IsTlsActive { get; private set; }

        public static string GetHttpBaseAddress(string address, int port, bool tls) => $"{(tls ? "https" : "http")}://{address}:{port}";
        public static string GetWebSocketAddress(string address, int port, bool tls) => $"{(tls ? "wss" : "ws")}://{address}:{port}/ws/rfc6455";

        private void UseTransport(bool tls)
        {
            lock (_connectionLock)
            {
                var port = tls ? TlsPort : Port;
                IsTlsActive = tls;
                webApi = new LoxoneWebApi(GetHttpBaseAddress(Address, port, tls), Username, password);
                var socketOptions = new PureWebSocketOptions()
                {
                    DebugMode = false
                };

                _webSocket = new PureWebSocket(GetWebSocketAddress(Address, port, tls), socketOptions);
            }
        }

        /// <summary>
        /// Loads LoxAPP3.json, over TLS if configured. Falls back to plain http if allowed.
        /// </summary>
        private async Task<LoxApp3Data> LoadLoxAppDataWithFallback()
        {
            try
            {
                return await webApi.GetBasicAuthRequest<LoxApp3Data>("/data/LoxAPP3.json");
            }
            catch (Exception e) when (IsTlsActive && AllowPlainFallback && (e is System.Net.Http.HttpRequestException || e is TaskCanceledException))
            {
                _logger.LogWarning($"Miniserver {Address} is not reachable over https ({e.Message}), falling back to http. Use the certificate host name of the Miniserver or disable the fallback to enforce TLS.");
                UseTransport(false);
                return await webApi.GetBasicAuthRequest<LoxApp3Data>("/data/LoxAPP3.json");
            }
        }

        private void _keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_webSocket.State != System.Net.WebSockets.WebSocketState.Open)
            {
                return;
            }
            _webSocket.Send("keepalive");
        }

        public LoxApp3Data LoxData => _loxData;

        public Task<LoxApp3Data> LoadLoxAppData()
        {
            return LoadLoxAppDataWithFallback();
        }

        /// <summary>
        /// true once the websocket is authenticated with the token
        /// </summary>
        public bool IsAuthenticated => _connectionState == ConnectionState.Connected;

        public Task<LoxoneApiResponse<WriteValueResponse>> WriteValue(string uuid, object value)
        {
            var command = $"jdev/sps/io/{WebUtility.UrlEncode(uuid)}/{WebUtility.UrlEncode(FormatValue(value))}";

            if (IsAuthenticated && _webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                // the websocket is authenticated with the token, no credentials are sent
                _webSocket.Send(EncryptMessageForMs($"salt/{GenerateSalt()}/{command}", false));
                return Task.FromResult(new LoxoneApiResponse<WriteValueResponse>
                {
                    Data = new WriteValueResponse { Code = 200, Control = command }
                });
            }

            return webApi.GetRequest<WriteValueResponse>("/" + command);
        }

        /// <summary>
        /// Formats a value for the /jdev/sps/io command, the miniserver expects "." as decimal separator regardless of the server culture.
        /// </summary>
        internal static string FormatValue(object value)
        {
            if (value is bool vBool)
            {
                return vBool ? "on" : "off";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public async Task<bool> Connect()
        {
            try
            {
                _logger.LogInformation($"Try to connect to miniserver {Address}:{(IsTlsActive ? TlsPort : Port)}");

                try
                {
                    // may switch to the plain transport, subscribe to the websocket afterwards
                    _loxData = await LoadLoxAppDataWithFallback();
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Could not connect to miniserver... {e.Message}");
                    return false;
                }
                lock (_lock)
                {
                    // never parse leftovers of a previous connection as the start of the new stream
                    _buffer.Clear();
                }


                _webSocket.OnStateChanged += _webSocket_OnStateChanged;
                _webSocket.OnMessage += _webSocket_OnMessage;
                _webSocket.OnClosed += _webSocket_OnClosed;
                _webSocket.OnData += _webSocket_OnData;

                _logger.LogInformation($"Connecting to miniserver over {(IsTlsActive ? "https/wss" : "http/ws")}");
                _keepAliveTimer.Start();
                var connect = await _webSocket.ConnectAsync();

                if(connect)
                {
                    await InitConnection();
                }
                return connect;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error occurred {e}");
                throw;
            }
        }

        public void Close()
        {
            _webSocket.OnStateChanged -= _webSocket_OnStateChanged;
            _webSocket.OnMessage -= _webSocket_OnMessage;
            _webSocket.OnClosed -= _webSocket_OnClosed;
            _webSocket.OnData -= _webSocket_OnData;

            _webSocket.Disconnect();
        }

        private void _webSocket_OnData(object sender, byte[] data)
        {
            ProcessData(data);
        }

        internal void ProcessData(byte[] data)
        {
            lock (_lock)
            {
                if(data == null)
                {
                    return;
                }
                _buffer.AddRange(data);

                TryReadData();
            }
        }

        private void TryReadData()
        {
            // a single data frame can contain several messages, process all complete ones
            while (_buffer.Count > 0)
            {
                // Console.WriteLine(Utility.Utils.ByteArrayToString(_buffer.ToArray()));
                if (_buffer[0] != 0x03) //check for header
                {
                    return;
                }

                if (_buffer.Count < 8)
                {
                    return;
                }

                var dataBuf = _buffer.ToArray().AsSpan();
                var header = new Header(dataBuf.Slice(0, 8));

                // estimated header (bit 7 of the info byte = byte 2), the exact header follows with the data
                if (Automatica.Core.Driver.Utility.Utils.IsBitSet(header.Info, 7))
                {
                    _buffer.RemoveRange(0, 8);
                    continue;
                }

                if (header.DataLength > MaxBinaryMessageLength)
                {
                    _logger.LogWarning($"Dropping binary message of {header.DataLength} bytes from miniserver (limit {MaxBinaryMessageLength})");
                    _buffer.Clear();
                    return;
                }

                if (header.Identifier == HeaderIdentifier.TextMessage)
                {
                    //will not be transmitted as OnData 
                    _buffer.RemoveRange(0, 8);
                    continue;
                }

                if (_buffer.Count < header.DataLength + 8)
                {
                    return;
                }

                //get my data array out of the buffer
                var dataBuffer = dataBuf.Slice(8, (int)header.DataLength);
                BinaryMessage msg = null;
                try
                {
                    msg = BinaryMessage.GenerateBinaryMessage(header, dataBuffer);
                }
                catch (Exception e) when (e is ArgumentException || e is IndexOutOfRangeException)
                {
                    _logger.LogWarning($"Dropping invalid {header.Identifier} message from miniserver: {e.Message}");
                }

                //remove the message from the buffer, the next data stays
                _buffer.RemoveRange(0, (int)header.DataLength + 8);

                if (msg != null)
                {
                    OnMessage?.Invoke(this, new OnEventTableMessageEventArgs(msg));
                }
            }
        }

        public static RSAParameters ToRSAParameters(RsaKeyParameters rsaKey)
        {
            var rp = new RSAParameters();
            rp.Modulus = rsaKey.Modulus.ToByteArrayUnsigned();
            if (rsaKey.IsPrivate)
                rp.D = rsaKey.Exponent.ToByteArrayUnsigned();
            else
                rp.Exponent = rsaKey.Exponent.ToByteArrayUnsigned();
            return rp;
        }

        private void _webSocket_OnSendFailed(object sender, string data, Exception ex)
        {
            _logger.LogError(ex, $"Failed to send data via websocket...{ex}");
        }

        private void _webSocket_OnClosed(object sender, System.Net.WebSockets.WebSocketCloseStatus reason)
        {
            _connectionState = ConnectionState.Disconnected;
            _encrypted = false;
            OnConnectionClosed?.Invoke(this, System.EventArgs.Empty);   
            _logger.LogInformation($"Websocket closed {reason}");
        }

        private void _webSocket_OnMessage(object sender, string message)
        {
            _logger.LogTrace($"WebSocket received: {message}");

            if (_connectionState == ConnectionState.Connected)
            {
                // responses to commands (e.g. writes) on the authenticated socket
                try
                {
                    var response = JsonConvert.DeserializeObject<LoxoneApiResponse<LoxoneApiResponseLL>>(message);
                    if (response?.Data != null && response.Data.Code != 200)
                    {
                        _logger.LogWarning($"Miniserver answered {response.Data.Code} for {response.Data.Control}");
                    }
                }
                catch (JsonException)
                {
                    // ignore
                }
                return;
            }

            if (_encrypted)
            {
                var encrypted = DecryptMessage(message);
                var baseMessage = JsonConvert.DeserializeObject<LoxoneApiResponse<LoxoneApiResponseLL>>(encrypted);

                if (baseMessage.Data.Code == 200)
                {
                    if (_connectionState == ConnectionState.AcquireToken)
                    {
                        var data = JsonConvert.DeserializeObject<LoxoneApiResponse<GetKeyResponseLL>>(encrypted);

                        var pwHash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes($"{password}:{data.Data.Value.Salt}")).ToHex(true);
                        var hash = $"{Username}:{pwHash}".CalculateHmacSha1(Automatica.Core.Driver.Utility.Utils.StringToByteArray(data.Data.Value.Key));

                        var cmd = $"salt/{GenerateSalt()}/jdev/sys/gettoken/{hash}/{Username}/4/{LoxoneUuid}/AutomaticaCoreServer";

                        var msg = EncryptMessageForMs(cmd);
                        _connectionState = ConnectionState.SaveToken;
                        _webSocket.Send(msg);
                    }
                    else if (_connectionState == ConnectionState.SaveToken)
                    {
                        var data = JsonConvert.DeserializeObject<LoxoneApiResponse<TokenResponseLL>>(encrypted);
                        _connectionState = ConnectionState.SendEnableUpdates;
                        _encrypted = false;
                        _webSocket.Send(EncryptMessageForMs($"salt/{GenerateSalt()}/jdev/sps/enablebinstatusupdate", false));

                    }
                }
                else
                {
                    //TODO: specify why
                    OnConnectionClosed?.Invoke(this, System.EventArgs.Empty);
                }
            }
            else
            {

                var baseMessage = JsonConvert.DeserializeObject<LoxoneApiResponse<LoxoneApiResponseLL>>(message);
                if (baseMessage.Data.Code == 200)
                {
                    _encrypted = true;
                    if (_connectionState == ConnectionState.ExchangeKeys)
                    {

                        var data = JsonConvert.DeserializeObject<LoxoneApiResponse<EncryptedMessageResponse>>(message);
                        var decryptedMessage = DecryptMessage(data.Data.Value);

                        _connectionState = ConnectionState.AcquireToken;
                        AcquireToken();
                    }
                    else if(_connectionState == ConnectionState.SendEnableUpdates)
                    {
                        _encrypted = false;
                        _connectionState = ConnectionState.Connected;
                        _logger.LogInformation("Miniserver websocket authenticated with token");
                    }
                }
            }
        }

        private string GenerateSalt()
        {
            if (_salt == null)
            {
                var random = new SecureRandom();
                var byteArray = new byte[2];
                random.NextBytes(byteArray);
                _salt = byteArray.ToHex(false);
            }
            return _salt;
        }

        private void AcquireToken()
        {
            var cmd = $"salt/{GenerateSalt()}/jdev/sys/getkey2/{Username}";
            var encrypted = EncryptMessageForMs(cmd);

            _webSocket.Send(encrypted);

        }

        private byte[] PrepareEncrypt(string payload, int outputBlockSize)
        {
            var byteArray = Encoding.UTF8.GetBytes(payload);
            if (byteArray.Length % outputBlockSize != 0)
            {
                var byteSize = Math.Ceiling((double)byteArray.Length / outputBlockSize);
                var newByteArray = new byte[(int)byteSize * outputBlockSize];

                Array.Copy(byteArray, newByteArray, byteArray.Length);
                return newByteArray;
            }
            return byteArray;
        }

        private string EncryptMessageForMs(string payload, bool sendsResponse=true)
        {
            using (var aes = Aes.Create())
            {
                aes.Padding = PaddingMode.None;
                aes.Mode = CipherMode.CBC;
                using (var encryptor = aes.CreateEncryptor(_aesKey, _aesIv))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            
                            cryptoStream.Write(PrepareEncrypt(payload, encryptor.OutputBlockSize));
                        }
                        var msFnc = "fenc";
                        if(!sendsResponse)
                        {
                            msFnc = "enc";
                        }
                        return $"jdev/sys/{msFnc}/{WebUtility.UrlEncode(Convert.ToBase64String(memoryStream.ToArray()))}"; 
                    }
                }
            }
        }

        private string DecryptMessage(string message)
        {
            var byteArray = Convert.FromBase64String(message);
            using (var aes = Aes.Create())
            {
                aes.Padding = PaddingMode.None;
                aes.Mode = CipherMode.CBC;
                using (var encryptor = aes.CreateDecryptor(_aesKey, _aesIv))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(byteArray);
                        }

                        return Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                }
            }
        }

        public byte[] GenerateAesKey(string key)
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            var hashed = KeyDerivation.Pbkdf2(
                key,
                salt,
                KeyDerivationPrf.HMACSHA256,
                50, 256/8);
            return hashed;
        }

    

        private async void _webSocket_OnStateChanged(object sender, System.Net.WebSockets.WebSocketState newState, System.Net.WebSockets.WebSocketState prevState)
        {
            _logger.LogInformation($"Websocket state changed from {prevState} to {newState}");

            if(newState == System.Net.WebSockets.WebSocketState.Open)
            {
            
            }
        }

        private async Task InitConnection()
        {
            OnConnectionEstablished?.Invoke(this, System.EventArgs.Empty);

            var publicKey = await webApi.GetRequest<GetPublicKeyResponse>("jdev/sys/getPublicKey"); ;
            _pubKey = Convert.FromBase64String(publicKey.Data.PublicKey.Replace("-----END CERTIFICATE-----", "").Replace("-----BEGIN CERTIFICATE-----", ""));
            string stringDataToEncrypt;

            var random = new SecureRandom();
            _aesKey = GenerateAesKey(LoxoneUuid);

            _aesIv = new byte[128 / 8];
            random.NextBytes(_aesIv);

            stringDataToEncrypt = $"{_aesKey.ToHex(false)}:{_aesIv.ToHex(false)}";

            var obj = Asn1Object.FromByteArray(_pubKey);
            var publicKeySequence = (DerSequence)obj;

            var encodedPublicKey = (DerBitString)publicKeySequence[1];
            var publicKeyDer = (DerSequence)Asn1Object.FromByteArray(encodedPublicKey.GetBytes());

            var modulus = (DerInteger)publicKeyDer[0];
            var exponent = (DerInteger)publicKeyDer[1];

            var keyParameters = new RsaKeyParameters(false, modulus.PositiveValue, exponent.PositiveValue);
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            encryptEngine.Init(true, keyParameters);

            var dataToEncrypt = Encoding.UTF8.GetBytes(stringDataToEncrypt);
            var encryptedData = encryptEngine.ProcessBlock(dataToEncrypt, 0, dataToEncrypt.Length);

            var publicKeySelf = Convert.ToBase64String(encryptedData);
            _connectionState = ConnectionState.ExchangeKeys;

            _logger.LogInformation($"Send connection info to websocket...");
            _webSocket.Send($"jdev/sys/keyexchange/{publicKeySelf}");
        }

        public string Address { get; }
        public int Port { get; }
        public string Username { get; }
    }
}
