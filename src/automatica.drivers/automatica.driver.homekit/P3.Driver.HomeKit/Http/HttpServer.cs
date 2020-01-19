using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKit.Http
{
    internal class HttpServer
    {
        private readonly ILogger _logger;
        private readonly int _port;

        private TcpListener _listener;

        private CancellationTokenSource _cts;

        private readonly HapMiddleware _middleware;

        private readonly List<HttpServerConnection> _connections = new List<HttpServerConnection>();

        public HttpServer(ILogger logger, HomeKitServer homeKitServer, int port, string pairCode)
        {
            _logger = logger;
            _port = port;

            _middleware = new HapMiddleware(logger, pairCode, homeKitServer);
        }


        public async Task<bool> Start()
        {
            _logger.LogDebug($"Starting http listener on port {_port}");
            _cts = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                
                try
                {
                    _listener = TcpListener.Create(_port);
                    _listener.Server.DualMode = true;
                    _listener.Start();

                    while (true)
                    {
                        _logger.LogDebug("Waiting for new tcp connection");
                        var tcpClient = await _listener.AcceptTcpClientAsync();

                        _logger.LogDebug($"New tcp connection from {((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}");

                        ThreadPool.QueueUserWorkItem(async state =>
                        {
                            var connection = new HttpServerConnection(_middleware, _logger, (TcpClient)state, this);
                            _connections.Add(connection);
                            await connection.HandleClient().ConfigureAwait(false);
                        }, tcpClient);
                    }
                }
                catch (TaskCanceledException)
                {
                    _listener.Stop();
                }
                catch (Exception e)
                {
                    _listener.Stop();
                    _logger.LogError(e, "Could not accept client");
                }
            }, _cts.Token).ConfigureAwait(false);


            return true;
        }

        internal void ConnectionClosed(HttpServerConnection connection)
        {
            _logger.LogDebug($"Closed connection from {connection.GetRemoteEndpoint()}");
            _connections.Remove(connection);
        }

        public Task<bool> Stop()
        {
            foreach (var con in _connections)
            {
                con.Close();
            }

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
            var response = HttpServerConnection.GetHttpResponse("EVENT/1.0", "application/hap+json", data, DateTime.Now);


            _logger.LogTrace($"Writing {Encoding.UTF8.GetString(response)}");

            foreach (var session in eventBasedNotification)
            {
                lock (session)
                {
                    try
                    {
                        var encrypted = HttpServerConnection.EncryptData(response, session);
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
