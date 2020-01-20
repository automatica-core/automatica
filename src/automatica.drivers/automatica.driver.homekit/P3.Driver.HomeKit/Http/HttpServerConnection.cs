using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap;

namespace P3.Driver.HomeKit.Http
{
    internal class HttpServerConnection
    {
        private readonly HapMiddleware _middleware;
        private readonly ILogger _logger;
        private readonly TcpClient _client;
        private readonly HttpServer _httpServer;
        private readonly ConnectionSession _session = new ConnectionSession();
        private EndPoint _remoteEndPoint;
        private HapSession _connectionSession;

        public HapSession Session => _connectionSession;

        public HttpServerConnection(HapMiddleware middleware, ILogger logger, TcpClient client, HttpServer httpServer)
        {
            _middleware = middleware;
            _logger = logger;
            _client = client;
            _httpServer = httpServer;
        }

        internal Task HandleClient()
        {
            _remoteEndPoint = _client.Client.RemoteEndPoint;
            _logger.LogDebug($"Start client socket on port {_remoteEndPoint}");

            var session = Guid.NewGuid().ToString();
            _connectionSession = _middleware.GetSession(session);

            _connectionSession.Client = _client;

            _client.ReceiveTimeout = 0;
            try
            {
                using (var networkStream = _client.GetStream())
                {
                    byte[] receiveBuffer = new byte[_client.ReceiveBufferSize];

                    while (_client.Connected)
                    {
                        _logger.LogDebug("Waiting for more data from the client....");

                        // This is blocking and will wait for data to come from the client.
                        //
                        var bytesRead = networkStream.Read(receiveBuffer, 0, _client.ReceiveBufferSize);;

                        lock (_connectionSession)
                        {
                            if (bytesRead == 0)
                            {
                                _logger.LogDebug(
                                    "**************************** REQUEST RECEIVED but no data available *************************");
                                break;
                            }

                            var content = receiveBuffer.AsSpan(0, bytesRead).ToArray();

                            if (_connectionSession.IsVerified)
                            {
                                _logger.LogDebug("Already in a verified state..");

                                var encryptionResult = DecryptData(content, _connectionSession);
                                content = encryptionResult;
                            }


                            //_logger.LogTrace($"Read {Encoding.UTF8.GetString(content)}");

                            var ms = new MemoryStream(content);
                            var sr = new StreamReader(ms);

                            var request = sr.ReadLine();
                            var tokens = request.Split(' ');
                            if (tokens.Length != 3)
                            {
                                _middleware.TerminateSession(session);
                                throw new Exception("Invalid HTTP request line");
                            }

                            var method = tokens[0].ToUpper();
                            var url = tokens[1].Trim('/');

                            _logger.LogDebug($"Request {method} on path {url}");

                            string line;

                            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();

                            while ((line = sr.ReadLine()) != null)
                            {
                                if (String.IsNullOrEmpty(line))
                                {
                                    break;
                                }

                                var lineSplit = line.Split(":", StringSplitOptions.RemoveEmptyEntries);

                                if (lineSplit.Length != 2)
                                {
                                    _logger.LogWarning($"Invalid header format in {line}");
                                    continue;
                                }

                                httpHeaders.Add(lineSplit[0].ToLower(), lineSplit[1]);
                            }


                            var contentLengthHeader =
                                httpHeaders.ContainsKey("content-length");

                            var contentLenght = 0;

                            if (contentLengthHeader)
                            {
                                contentLenght =
                                    Convert.ToInt32(
                                        httpHeaders.Single(a => a.Key == "content-length").Value);
                            }

                            var datLen = content.Length;
                            var data = content.AsSpan(datLen - contentLenght, contentLenght).ToArray();


                            var result = _middleware.Invoke(session, url, method, data, _session);
                            var response = GetHttpResponse("HTTP/1.1", result.Item1, result.Item2, DateTime.Now);

                                _logger.LogTrace($"Writing {Encoding.UTF8.GetString(response)}");

                            if (_connectionSession.IsVerified && !_connectionSession.SkipFirstEncryption)
                            {
                                response = EncryptData(response, _connectionSession);

                                networkStream.Write(response, 0, response.Length);
                                networkStream.Flush();
                            }
                            else
                            {
                                networkStream.Write(response, 0, response.Length);
                                networkStream.Flush();

                                if (_connectionSession.SkipFirstEncryption)
                                {
                                    _connectionSession.SkipFirstEncryption = false;
                                }
                            }


                            _logger.LogDebug(
                                "**************************** REQUEST DONE *************************");
                        }
                    }

                    _middleware.TerminateSession(session);
                    _client.Close();
                    _client.Dispose();
                    _httpServer.ConnectionClosed(this);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured in http server");
                _middleware.TerminateSession(session);
            }

            return Task.CompletedTask;
        }

        internal static byte[] GetHttpResponse(string header, string contentType, byte[] data, DateTime date)
        {
            var response = new byte[0];
            var returnChars = new byte[2];
            returnChars[0] = 0x0D;
            returnChars[1] = 0x0A;

            var contentLength = $"Content-Length: {data.Length}";

            if (data.Length == 0)
            {
                response = response
                    .Concat(Encoding.ASCII.GetBytes($"{header} 204 No Content"))
                    .Concat(returnChars).ToArray();
            }
            else
            {
                response = response.Concat(Encoding.UTF8.GetBytes($"{header} 200 OK")).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.UTF8.GetBytes(contentLength)).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.UTF8.GetBytes($"Content-Type: {contentType}")).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.UTF8.GetBytes($"Date: {date.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture)}")).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.UTF8.GetBytes($"Connection: keep-alive")).Concat(returnChars).ToArray();
                //response = response.Concat(Encoding.UTF8.GetBytes($"Transfer-Encoding: chunked")).Concat(returnChars).ToArray();

            }

            response = response.Concat(returnChars).ToArray();
            response = response.Concat(data).ToArray();

            return response;
        }

        private static string RemoveLeadingZero(string input)
        {
            if (input.StartsWith("0"))
            {
                var newStr = input.Substring(1, input.Length - 1);
                return RemoveLeadingZero(newStr);
            }

            return input;
        }

        internal static byte[] EncryptData(byte[] plainData, HapSession session)
        {
            var resultData = new byte[0];

            for (int offset = 0; offset < plainData.Length;)
            {
                int length = Math.Min(plainData.Length - offset, 1024);

                var dataLength = BitConverter.GetBytes((short)length);

                resultData = resultData.Concat(dataLength).ToArray();

                var zeros = new byte[] { 0, 0, 0, 0 };
                var nonce = new Nonce(zeros, BitConverter.GetBytes(session.OutboundBinaryMessageCount++));

                var dataToEncrypt = new byte[length];
                Array.Copy(plainData, offset, dataToEncrypt, 0, length);

                // Use the AccessoryToController key to decrypt the data
                var encryptedData = AeadAlgorithm.ChaCha20Poly1305.Encrypt(Key.Import(AeadAlgorithm.ChaCha20Poly1305, session.AccessoryToControllerKey, KeyBlobFormat.RawSymmetricKey), nonce, dataLength, dataToEncrypt);
                resultData = resultData.Concat(encryptedData).ToArray();

                offset += length;
            }

            return resultData;
        }

        internal static byte[] DecryptData(byte[] content, HapSession session)
        {
            var encryptionResult = new byte[0];

            for (int offset = 0; offset < content.Length;)
            {
                // The first type bytes represent the length of the data.
                //
                byte[] twoBytes = new Byte[] { content[0], content[1] };

                offset += 2;

                UInt16 frameLength = BitConverter.ToUInt16(twoBytes, 0);

                int availableDataLength = content.Length - offset;
                var message = content.AsSpan(offset, availableDataLength);

                var zeros = new byte[] { 0, 0, 0, 0 };
                var nonce = new Nonce(zeros, BitConverter.GetBytes(session.InboundBinaryMessageCount++));


                // Use the AccessoryToController key to decrypt the data.
                //
                var decrypt = AeadAlgorithm.ChaCha20Poly1305.Decrypt(Key.Import(AeadAlgorithm.ChaCha20Poly1305, session.ControllerToAccessoryKey, KeyBlobFormat.RawSymmetricKey), nonce, twoBytes, message, out var output);

                if (!decrypt)
                {
                    throw new InvalidDataException();
                }

                encryptionResult = encryptionResult.Concat(output).ToArray();

                offset += (18 + frameLength);
            }

            return encryptionResult;
        }

        public void Close()
        {
            try
            {
                _client.Close();
                _client.Dispose();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public EndPoint GetRemoteEndpoint()
        {
            return _remoteEndPoint;
        }
    }
}
