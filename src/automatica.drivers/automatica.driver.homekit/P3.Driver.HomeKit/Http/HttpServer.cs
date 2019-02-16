using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Http
{
    internal class HttpServer
    {
        private readonly ILogger _logger;
        private readonly HomeKitServer _homeKitServer;
        private readonly int _port;
        private readonly string _name;
        private readonly string _pairCode;

        private TcpListener _listener;

        private CancellationTokenSource _cts;

        private readonly HapMiddleware _middleware;

        public HttpServer(ILogger logger, HomeKitServer homeKitServer, int port, string name, string pairCode)
        {
            _logger = logger;
            _homeKitServer = homeKitServer;
            _port = port;
            _name = name;
            _pairCode = pairCode;
            
            _middleware = new HapMiddleware(logger, _pairCode, homeKitServer);
        }


        public Task<bool> Start()
        {
            _logger.LogDebug($"Starting http listener on port {_port}");
            _cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                CancellationTokenSource ctsConnection = null;

                try
                {
                    _listener = TcpListener.Create(_port);
                    _listener.Server.DualMode = true;
                    _listener.Start();

                    while (true)
                    {
                        ctsConnection = new CancellationTokenSource();
                        _logger.LogDebug($"Accepting tcp connection");
                        var tcpClient = await _listener.AcceptTcpClientAsync();

                        _logger.LogDebug($"New tcp connection from {tcpClient.Client.LocalEndPoint}");
                        await HandleClient(tcpClient, ctsConnection);
                    }
                }
                catch (TaskCanceledException)
                {
                    _listener.Stop();
                    ctsConnection?.Cancel();
                }
                catch (Exception e)
                {
                    _listener.Stop();
                    _logger.LogDebug(e, "Could not accept client");
                }
            }, _cts.Token);


            return Task.FromResult(true);
        }

        private Task HandleClient(TcpClient tcpClient, CancellationTokenSource token)
        {
            return Task.Run(async () => {
                var session = Guid.NewGuid().ToString();
                var connectionSession = _middleware.GetSession(session);

                connectionSession.Socket = tcpClient.Client;

                tcpClient.ReceiveTimeout = TimeSpan.FromSeconds(1).Milliseconds;
                try
                {
                    using (var networkStream = tcpClient.GetStream())
                    {
                        byte[] receiveBuffer = new byte[tcpClient.ReceiveBufferSize];

                        while (true)
                        {
                            _logger.LogDebug("Waiting for more data from the client....");

                            // This is blocking and will wait for data to come from the client.
                            //
                            var bytesRead =
                                await networkStream.ReadAsync(receiveBuffer, 0, tcpClient.ReceiveBufferSize);

                            lock (connectionSession)
                            {
                                if (bytesRead == 0)
                                {
                                    _logger.LogDebug(
                                        "**************************** REQUEST RECEIVED but not data available *************************");

                                    tcpClient.Close();
                                    break;
                                }

                                var content = receiveBuffer.AsSpan(0, bytesRead).ToArray();

                                if (connectionSession.IsVerified)
                                {
                                    _logger.LogDebug("Already in a verified state..");

                                    var encryptionResult = DecryptData(content, connectionSession);
                                    content = encryptionResult;
                                }


                                _logger.LogTrace($"Read {Encoding.UTF8.GetString(content)}");

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
                                var version = tokens[2];

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


                                var result = _middleware.Invoke(session, url, method, data);
                                var response = GetHttpResponse("HTTP/1.1", result.Item1, result.Item2);

                                _logger.LogTrace($"Writing {Encoding.UTF8.GetString(response)}");

                                if (connectionSession.IsVerified && !connectionSession.SkipFirstEncryption)
                                {
                                    response = EncryptData(response, connectionSession);

                                    networkStream.Write(response, 0, response.Length);
                                    networkStream.Flush();
                                }
                                else
                                {
                                    networkStream.Write(response, 0, response.Length);
                                    networkStream.Flush();

                                    if (connectionSession.SkipFirstEncryption)
                                    {
                                        connectionSession.SkipFirstEncryption = false;
                                    }
                                }


                                _logger.LogDebug(
                                    "**************************** REQUEST DONE *************************");
                            }
                        }

                        _middleware.TerminateSession(session);
                    }
                }
                catch (Exception e)
                {
                    _middleware.TerminateSession(session);
                }
            }, token.Token);
        }

        private byte[] GetHttpResponse(string header, string contentType, byte[] data)
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
                response = response.Concat(Encoding.ASCII.GetBytes($"{header} 200 OK")).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.ASCII.GetBytes(contentLength)).Concat(returnChars).ToArray();
                response = response.Concat(Encoding.ASCII.GetBytes($"Content-Type: {contentType}")).Concat(returnChars).ToArray();
            }

            response = response.Concat(returnChars).ToArray();
            response = response.Concat(data).ToArray();

            return response;
        }

        private byte[] EncryptData(byte[] plainData, HapSession session)
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

        private byte[] DecryptData(byte[] content, HapSession session)
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

        public Task<bool> Stop()
        {
            _cts.Cancel();

            _listener?.Stop();
            return Task.FromResult(true);
        }

        public void SendNotification(Characteristic characteristic, List<HapSession> eventBasedNotification)
        {
            var baseChar = new CharacteristicBase(characteristic.DefaultType)
            {
                Id = characteristic.Id,
                Value = characteristic.Value,
                AccessoryId = characteristic.Service.Accessory.Id
            };

            var charReturn = new CharacteristicsList<CharacteristicBase>
            {
                Characteristics = new List<CharacteristicBase> {baseChar}
            };

            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(charReturn, HapMiddleware.JsonSettings));
            var response = GetHttpResponse("EVENT/1.0", "application/hap+json", data);


            _logger.LogTrace($"Writing {Encoding.UTF8.GetString(response)}");

            foreach (var session in eventBasedNotification)
            {
                lock (session)
                {
                    try
                    {
                        var encrypted = EncryptData(response, session);
                        session.Socket.Send(encrypted);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Could not set value");
                    }
                }
            }
        }
    }
}
