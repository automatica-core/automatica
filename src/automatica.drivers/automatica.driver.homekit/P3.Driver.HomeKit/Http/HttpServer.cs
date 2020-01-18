using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
                        _logger.LogDebug($"Waiting for new tcp connection");
                        var tcpClient = await _listener.AcceptTcpClientAsync();

                        _logger.LogDebug($"New tcp connection from {((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}");

                        var connection = new HttpServerConnection(_middleware, _logger, tcpClient, ctsConnection);

                        await Task.Run(connection.HandleClient).ConfigureAwait(false);
                        _logger.LogDebug($"Started handler for the new tcp connection...");
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
                    _logger.LogError(e, "Could not accept client");
                }
            }, _cts.Token);


            return Task.FromResult(true);
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
            var response = HttpServerConnection.GetHttpResponse("EVENT/1.0", "application/hap+json", data);


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
