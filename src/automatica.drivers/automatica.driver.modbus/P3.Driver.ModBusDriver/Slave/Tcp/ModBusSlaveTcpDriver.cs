using System;
using System.Collections.Generic;
using System.IO;
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
        /// <summary>
        /// MBAP length field: unit id + PDU (max 253 bytes)
        /// </summary>
        internal const int MaxFrameLength = 254;
        internal const int MinFrameLength = 2;

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
            _listener = new TcpListener(config.BindAddress ?? IPAddress.Any, config.Port);
            _listenerThread = new Thread(Start);

            InitStore(config);
        }

        protected override bool ReadOnly => _config.ReadOnly;

        internal int LocalPort => ((IPEndPoint)_listener.LocalEndpoint).Port;

        internal int ConnectionCount
        {
            get
            {
                lock (_connections)
                {
                    return _connections.Count;
                }
            }
        }

        private void Start()
        {
            ModBus.Logger.LogInformation($"Start tcp listener on {_config.BindAddress}:{_config.Port} (max {_config.MaxConnections} connections{(_config.ReadOnly ? ", read only" : "")})");
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
                    if (!TryRegisterClient(clt))
                    {
                        clt.Dispose();
                        continue;
                    }

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

        private bool TryRegisterClient(TcpClient client)
        {
            IPAddress remote = null;
            try
            {
                remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
            }
            catch (Exception)
            {
                // ignore, handled as not allowed below if an allow list is configured
            }

            if (!_config.IsClientAllowed(remote))
            {
                ModBus.Logger.LogWarning($"Rejected modbus connection from {remote}, not in the allowed client list");
                return false;
            }

            lock (_connections)
            {
                if (_config.MaxConnections > 0 && _connections.Count >= _config.MaxConnections)
                {
                    ModBus.Logger.LogWarning($"Rejected modbus connection from {remote}, {_config.MaxConnections} connections are open already");
                    return false;
                }

                _connections.Add(client);
            }

            return true;
        }

        private async Task WorkOnClient(TcpClient client)
        {
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

        /// <summary>
        /// Reads exactly buffer.Length bytes. Returns false if the remote side closed the connection before the first byte.
        /// </summary>
        private static async Task<bool> ReadExactly(Stream stream, byte[] buffer, TimeSpan firstByteTimeout, TimeSpan restTimeout)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var timeout = offset == 0 ? firstByteTimeout : restTimeout;
                using (var cts = new CancellationTokenSource(timeout))
                {
                    int read;
                    try
                    {
                        read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TimeoutException();
                    }

                    if (read == 0)
                    {
                        if (offset == 0)
                        {
                            return false;
                        }
                        throw new EndOfStreamException();
                    }

                    offset += read;
                }
            }

            return true;
        }

        private async Task WorkOnClientStream(TcpClient client)
        {
            using (NetworkStream ns = client.GetStream())
            {
                while (client.Connected && _running)
                {
                    try
                    {
                        var header = new byte[6];
                        if (!await ReadExactly(ns, header, _config.IdleTimeout, _config.FrameTimeout))
                        {
                            // remote side closed the connection
                            break;
                        }

                        var frameLength = Automatica.Core.Driver.Utility.Utils.GetUShort(header[4], header[5]);

                        if (frameLength < MinFrameLength || frameLength > MaxFrameLength)
                        {
                            ModBus.Logger.LogWarning($"Invalid modbus frame length {frameLength}, closing connection");
                            break;
                        }

                        var frame = new byte[frameLength];
                        await ReadExactly(ns, frame, _config.FrameTimeout, _config.FrameTimeout);

                        if (frameLength < 6)
                        {
                            // not enough data for start index and length
                            break;
                        }

                        var deviceId = frame[0];
                        var function = frame[1];
                        var startIndex = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[2], frame[3]);
                        var length = Automatica.Core.Driver.Utility.Utils.GetUShort(frame[4], frame[5]);

                        var responseData = GetResponseData(deviceId, function, startIndex, length, frame, header).ToArray();

                        await ns.WriteAsync(responseData, 0, responseData.Length);
                    }
                    catch (TimeoutException)
                    {
                        ModBus.Logger.LogDebug("Modbus connection timed out");
                        break;
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
