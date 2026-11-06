
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging;

namespace P3.Driver.ModBusDriver.Slave.Tcp
{
    public class ModBusSlaveTcpDriver : ModBusSlaveDriver<ModBusSlaveTcpConfig>
    {
        private readonly ModBusSlaveTcpConfig _config;
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _connections = new List<TcpClient>();
        private volatile bool _running = true;

        private readonly Thread _listenerThread;
        private readonly object _lock = new object();

        public ModBusSlaveTcpDriver(ModBusSlaveTcpConfig config, ITelegramMonitorInstance telegramMonitor, ILogger logger) : base(config, telegramMonitor)
        {
            _config = config;
            _logger = logger;
            _listener = new TcpListener(IPAddress.Any, config.Port);
            _listenerThread = new Thread(Start);

            InitStore(config);
        }

        private void Start()
        {
            ModBus.Logger.LogInformation($"Start tcp listener on port {_config.Port}");
            while (_running)
            {
                TcpClient clt = null;
                try
                {
                    // Pending/Accept throw once the listener was stopped, never let that escape the listener thread
                    if (_listener.Pending())
                    {
                        clt = _listener.AcceptTcpClient();
                    }
                }
                catch (Exception e)
                {
                    if (_running)
                    {
                        ModBus.Logger.LogError(e, $"Could not accept tcp client {e}");
                    }
                }

                if (clt != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await WorkOnClient(clt);
                        }
                        catch (Exception e)
                        {
                            ModBus.Logger.LogError(e, $"Error working on tcp client {e}");
                        }
                    });
                }
                else
                {
                    Thread.Sleep(100); //<--- timeout
                }
            }

            ModBus.Logger.LogInformation($"Stopping tcp listener on port {_config.Port}");
        }

        private async Task WorkOnClient(TcpClient client)
        {
            lock (_connections)
            {
                _connections.Add(client);
            }

            try
            {
                await WorkOnClientStream(client);
            }
            finally
            {
                lock (_connections)
                {
                    _connections.Remove(client);
                }

                client.Dispose();
            }
        }

        private async Task WorkOnClientStream(TcpClient client)
        {
            using (NetworkStream ns = client.GetStream())
            {
                while (client.Connected)
                {
                    try
                    {
                        // a single read may return a part of a frame, read the header and the frame exactly
                        var header = new byte[6];
                        if (!await Master.ModBusFrameReader.ReadExactlyAsync(ns, header, CancellationToken.None))
                        {
                            // remote side closed the connection
                            break;
                        }

                        var frameLength = Automatica.Core.Driver.Utility.Utils.GetUShort(header[4], header[5]);
                        if (frameLength < 6)
                        {
                            // too short for unit id, function, address and quantity/value
                            break;
                        }

                        var frame = new byte[frameLength];
                        if (!await Master.ModBusFrameReader.ReadExactlyAsync(ns, frame, CancellationToken.None))
                        {
                            break;
                        }

                        var deviceId = frame[0];
                        var function = frame[1];
                        var startIndex = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[2], frame[3]);
                        var length = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[4], frame[5]);

                        var responseData = GetResponseData(deviceId, function, startIndex, length, frame, header).ToArray();

                        await ns.WriteAsync(responseData, 0, responseData.Length);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }

        protected void InitStore(ModBusSlaveTcpConfig config)
        {
            lock (_lock)
            {
                ModBus.Logger
                    .LogDebug($"Creating device store with {String.Join(",", config.DeviceIds)} devices");
                foreach (var dev in config.DeviceIds)
                {
                    DeviceStore.Add(dev, new ModBusSlaveStore(dev));
                }
            }
        }

        protected override bool IgnoreDeviceId => Config.IgnoreDeviceId;


        protected override Span<byte> CreateResponseFrame(Span<byte> data, Span<byte> header)
        {
            var array = new byte[data.Length + 6];
            Array.Copy(header.ToArray(), 0, array, 0, 4);

            var lengthByte = BitConverter.GetBytes((short)data.Length).Reverse().ToArray();
            Array.Copy(lengthByte, 0, array, 4, 2);

            Array.Copy(data.ToArray(), 0, array, 6, data.Length);

            return array;
        }

        protected override bool OpenConnection()
        {
            _listener.Start();
            _listenerThread.Start();
            return true;
        }

        protected override bool CloseConnection()
        {
            _running = false;
            lock (_connections)
            {
                foreach (var c in _connections)
                {
                    c.Close();
                }
                _connections.Clear();
            }
            _listener.Stop();
            return true;
        }


    }
}
