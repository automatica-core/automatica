using System;
using System.Collections.Generic;
using System.Linq;
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


        public HapSession GetClientSession(string clientUserName)
        {
            var connections = _connections.Where(a => a.Session != null &&  a.Session.ClientUsername == clientUserName);
            var session = connections.FirstOrDefault(a => a.Session != null && a.Session.Client.Connected);
            return session?.Session;
        }
        public Task<bool> Start()
        {
            _logger.LogDebug($"Starting http listener on port {_port}");
            _cts = new CancellationTokenSource();
#pragma warning disable 4014
            Task.Run(async () =>
            {

                try
                {
                    _listener = TcpListener.Create(_port);
                    _listener.Server.DualMode = true;
                    _listener.Start();

                    while (true)
                    {
                        _logger.LogDebug("Waiting for new tcp connection");
                        var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);

                        _logger.LogDebug(
                            $"New tcp connection from {((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}");

                        ThreadPool.QueueUserWorkItem(async state =>
                        {
                            var connection = new HttpServerConnection(_middleware, _logger, (TcpClient)state, this);
                            _connections.Add(connection);
                            await connection.HandleClient(_cts.Token).ConfigureAwait(false);
                        }, tcpClient);
                    }
                }
                catch (TaskCanceledException)
                {
                    _listener.Stop();
                }
                catch (OperationCanceledException)
                {

                    _listener.Stop();
                }
                catch (Exception e)
                {
                    _listener.Stop();
                    _logger.LogError(e, "Could not accept client");
                }
            }, _cts.Token).ConfigureAwait(false);
#pragma warning restore 4014

           
            return Task.FromResult(true);
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

        public bool SendNotification(Characteristic characteristic, HapSession session)
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
            var response =
                HttpServerConnection.GetHttpResponse("EVENT/1.0", "application/hap+json", data, DateTime.Now);

            _logger.LogTrace($"Writing {Encoding.UTF8.GetString(response)}");

            lock (session)
            {
                try
                {
                    var encrypted = HttpServerConnection.EncryptData(response, session);
                    session.Client.Client.Send(encrypted);
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not set value");
                }
            }

            return false;
        }

    }
}
