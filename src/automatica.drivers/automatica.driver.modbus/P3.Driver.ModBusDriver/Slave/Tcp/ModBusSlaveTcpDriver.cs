
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
        private bool _running = true;

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
                if (_listener.Pending())
                {
                    var clt = _listener.AcceptTcpClient();

                    var readTask = new Task(async () =>
                    {
                        await WorkOnClient(clt);

                    });

                    readTask.Start();
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
            _connections.Add(client);
            using (NetworkStream ns = client.GetStream())
            {
                while (client.Connected)
                {
                    try
                    {
                        var header = new byte[6];
                        int read = await ns.ReadAsync(header, 0, 6);

                        if (read == 6)
                        {
                            var frameLength = Automatica.Core.Driver.Utility.Utils.GetUShort(header[4], header[5]);
                            var frame = new byte[frameLength];

                            read = await ns.ReadAsync(frame, 0, frameLength);

                            if (read == frameLength)
                            {
                                var deviceId = frame[0];
                                var function = frame[1];
                                var startIndex = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[2], frame[3]);
                                var length = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[4], frame[5]);

                                var responseData = GetResponseData(deviceId, function, startIndex, length, frame, header).ToArray();
                                
                                await ns.WriteAsync(responseData, 0, responseData.Length);

                            }
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
            _connections.Remove(client);
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
            foreach (var c in _connections)
            {
                c.Close();
            }
            _connections.Clear();
            _running = false;
            _listener.Stop();
            return true;
        }


    }
}
