using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver.Master.Tcp;
using P3.Driver.ModBusDriver.Slave.Tcp;
using Xunit;

namespace P3.Driver.ModBus.Tests.Integration
{
    /// <summary>
    /// A real <see cref="ModBusSlaveTcpDriver"/> listening on 127.0.0.1 with an ephemeral port
    /// </summary>
    internal static class ModBusTcpTestHarness
    {
        public const byte DeviceId = 1;
        public const ushort RegisterCount = 32;

        public static readonly TimeSpan IoTimeout = TimeSpan.FromSeconds(5);

        public static ModBusSlaveTcpDriver CreateSlave(Action<ModBusSlaveTcpConfig> configure = null, bool open = true)
        {
            var config = new ModBusSlaveTcpConfig
            {
                Port = 0,
                BindAddress = IPAddress.Loopback,
                DeviceIds = new List<int> { DeviceId },
                IgnoreDeviceId = false
            };
            configure?.Invoke(config);

            var slave = new ModBusSlaveTcpDriver(config, new EmptyTelegramMonitorInstance(), NullLogger.Instance);
            foreach (var device in config.DeviceIds)
            {
                for (ushort i = 0; i < RegisterCount; i++)
                {
                    slave.InitHoldingRegister(device, i, 0);
                    slave.InitInputRegister(device, i, 0);
                    slave.InitCoil(device, i, false);
                    slave.InitDiscreteInput(device, i, false);
                }
            }

            if (open)
            {
                Assert.True(slave.Open());
            }
            return slave;
        }

        public static ModBusMasterTcpDriver CreateMaster(int port, int timeoutMs = 2000, bool open = true)
        {
            var master = new ModBusMasterTcpDriver(new ModBusMasterTcpConfig
            {
                IpAddress = IPAddress.Loopback,
                // the config stores the port as short, ports > 32767 wrap and are cast back to ushort on connect
                Port = unchecked((short)port),
                Timeout = timeoutMs
            }, new EmptyTelegramMonitorInstance());

            if (open)
            {
                Assert.True(master.Open());
            }
            return master;
        }

        public static async Task<TcpClient> ConnectRaw(int port)
        {
            var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(IPAddress.Loopback, port);
            return client;
        }

        public static byte[] Mbap(ushort transactionId, params byte[] unitAndPdu)
        {
            var frame = new List<byte>
            {
                (byte)(transactionId >> 8), (byte)transactionId, 0, 0,
                (byte)(unitAndPdu.Length >> 8), (byte)unitAndPdu.Length
            };
            frame.AddRange(unitAndPdu);
            return frame.ToArray();
        }

        public static byte[] ReadRequest(ushort transactionId, byte function, ushort start, ushort count, byte unit = DeviceId)
        {
            return Mbap(transactionId, unit, function, (byte)(start >> 8), (byte)start, (byte)(count >> 8), (byte)count);
        }

        /// <summary>
        /// Reads one MBAP response: header exactly, then the announced length
        /// </summary>
        public static async Task<byte[]> ReadResponse(TcpClient client, TimeSpan? timeout = null)
        {
            using var cts = new CancellationTokenSource(timeout ?? IoTimeout);
            var stream = client.GetStream();
            var header = new byte[6];
            await stream.ReadExactlyAsync(header, cts.Token);
            var body = new byte[(header[4] << 8) | header[5]];
            await stream.ReadExactlyAsync(body, cts.Token);
            return header.Concat(body).ToArray();
        }

        public static async Task<byte[]> SendAndReceive(TcpClient client, byte[] request)
        {
            await client.GetStream().WriteAsync(request);
            return await ReadResponse(client);
        }

        /// <summary>
        /// True if the server closed (FIN or RST) the connection within <paramref name="timeout"/>
        /// </summary>
        public static async Task<bool> IsClosedByServer(TcpClient client, TimeSpan? timeout = null)
        {
            try
            {
                var buffer = new byte[64];
                using var cts = new CancellationTokenSource(timeout ?? IoTimeout);
                while (true)
                {
                    var read = await client.GetStream().ReadAsync(buffer, cts.Token);
                    if (read == 0)
                    {
                        return true;
                    }
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public static async Task WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? IoTimeout);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("condition not met in time");
                }
                await Task.Delay(10);
            }
        }
    }

    /// <summary>
    /// Forwards a single connection between a master and a slave. Responses (slave to master) are forwarded frame by frame,
    /// <see cref="DuplicateResponses"/> sends each response a second time to simulate a late answer to an earlier request.
    /// </summary>
    internal sealed class ModBusTcpProxy : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly int _targetPort;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly List<ushort> _requestTransactionIds = new List<ushort>();

        public ModBusTcpProxy(int targetPort)
        {
            _targetPort = targetPort;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Task = System.Threading.Tasks.Task.Run(Run);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public bool DuplicateResponses { get; set; }

        public Task Task { get; }

        public IReadOnlyList<ushort> RequestTransactionIds
        {
            get
            {
                lock (_requestTransactionIds)
                {
                    return _requestTransactionIds.ToList();
                }
            }
        }

        private async Task Run()
        {
            try
            {
                using var master = await _listener.AcceptTcpClientAsync(_cts.Token);
                using var slave = new TcpClient();
                await slave.ConnectAsync(IPAddress.Loopback, _targetPort, _cts.Token);

                var up = Pump(master.GetStream(), slave.GetStream(), false);
                var down = Pump(slave.GetStream(), master.GetStream(), true);
                await System.Threading.Tasks.Task.WhenAny(up, down);
            }
            catch (Exception)
            {
                // closed
            }
        }

        private async Task Pump(Stream from, Stream to, bool isResponse)
        {
            var header = new byte[6];
            while (!_cts.IsCancellationRequested)
            {
                await from.ReadExactlyAsync(header, _cts.Token);
                var body = new byte[(header[4] << 8) | header[5]];
                await from.ReadExactlyAsync(body, _cts.Token);
                var frame = header.Concat(body).ToArray();

                if (!isResponse)
                {
                    lock (_requestTransactionIds)
                    {
                        _requestTransactionIds.Add((ushort)((header[0] << 8) | header[1]));
                    }
                }

                await to.WriteAsync(frame, _cts.Token);
                if (isResponse && DuplicateResponses)
                {
                    await to.WriteAsync(frame, _cts.Token);
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}
