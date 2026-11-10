using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using P3.Driver.ModBus.SolarmanV5;
using P3.Driver.ModBus.SolarmanV5.Config;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.Tests.Solarman
{
    /// <summary>
    /// A Solarman V5 data logger on 127.0.0.1 in front of an inverter with 1000 holding registers. It checks the V5
    /// envelope (start, length, checksum, end) and the RTU CRC of every request and answers FC03 and FC16 like a Deye
    /// logger; FC06 is answered with an illegal function exception.
    /// </summary>
    internal sealed class FakeSolarmanLogger : IAsyncDisposable
    {
        public const uint Serial = 2712345678;

        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _serverTask;

        public ushort[] Registers { get; } = new ushort[1000];

        /// <summary>
        /// Every request as (function code, start register, values written)
        /// </summary>
        public List<(byte function, int start, ushort[] values)> Requests { get; } = new();

        public List<string> Errors { get; } = new();

        /// <summary>
        /// Acknowledges writes without storing them, like an inverter that refuses the setting
        /// </summary>
        public bool IgnoreWrites { get; set; }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public FakeSolarmanLogger()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _serverTask = Task.Run(Serve);
        }

        public IEnumerable<(byte function, int start, ushort[] values)> Writes => Requests.Where(a => a.function != 0x03);

        public SolarmanConnection CreateConnection()
        {
            var connection = new SolarmanConnection(new SolarmanConfig
            {
                IpAddress = IPAddress.Loopback,
                Port = unchecked((short)Port),
                SolarmanSerialNumber = Serial,
                Timeout = 2000
            }, new EmptyTelegramMonitorInstance());

            if (!connection.Open())
            {
                throw new IOException("Could not connect to the fake logger");
            }

            return connection;
        }

        private async Task Serve()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var stream = client.GetStream();
                    while (true)
                    {
                        var request = await ReadRequest(stream);
                        if (request == null)
                        {
                            break;
                        }

                        var response = Handle(request);
                        await stream.WriteAsync(response, _cts.Token);
                    }
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or IOException or SocketException)
            {
            }
        }

        private async Task<byte[]> ReadRequest(NetworkStream stream)
        {
            var header = new byte[11];
            if (!await ReadExactly(stream, header))
            {
                return null;
            }

            var length = header[1] | (header[2] << 8);
            var rest = new byte[length + 2];
            if (!await ReadExactly(stream, rest))
            {
                return null;
            }

            var frame = header.Concat(rest).ToArray();
            Check(frame[0] == 0xA5, "start byte");
            Check(frame[3] == 0x10 && frame[4] == 0x45, "request control code");
            Check(BitConverter.ToUInt32(frame, 7) == Serial, "logger serial");
            Check(frame[^1] == 0x15, "end byte");
            Check(frame[^2] == Checksum(frame), "V5 checksum");
            Check(frame[11] == 0x02, "frame type");

            // payload: frame type (1), sensor type (2), delivery/power on/offset time (12), then the RTU frame
            var rtu = frame.Skip(11 + 15).Take(length - 15).ToArray();
            var crc = ModbusUtility.CalculateCrc(rtu.Take(rtu.Length - 2).ToArray());
            Check(crc[0] == rtu[^2] && crc[1] == rtu[^1], "RTU CRC");

            return rtu.Take(rtu.Length - 2).ToArray();
        }

        private byte[] Handle(byte[] rtu)
        {
            var slave = rtu[0];
            var function = rtu[1];
            var start = (rtu[2] << 8) | rtu[3];
            var count = (rtu[4] << 8) | rtu[5];

            List<byte> answer;
            switch (function)
            {
                case 0x03:
                    lock (Requests)
                    {
                        Requests.Add((function, start, Array.Empty<ushort>()));
                    }
                    answer = new List<byte> { slave, function, (byte)(count * 2) };
                    for (var i = 0; i < count; i++)
                    {
                        answer.Add((byte)(Registers[start + i] >> 8));
                        answer.Add((byte)(Registers[start + i] & 0xFF));
                    }
                    break;
                case 0x10:
                {
                    Check(rtu[6] == count * 2, "FC16 byte count");
                    var values = new ushort[count];
                    for (var i = 0; i < count; i++)
                    {
                        values[i] = (ushort)((rtu[7 + i * 2] << 8) | rtu[8 + i * 2]);
                    }

                    lock (Requests)
                    {
                        Requests.Add((function, start, values));
                    }

                    if (!IgnoreWrites)
                    {
                        Array.Copy(values, 0, Registers, start, count);
                    }

                    answer = new List<byte> { slave, function, rtu[2], rtu[3], rtu[4], rtu[5] };
                    break;
                }
                default:
                    lock (Requests)
                    {
                        Requests.Add((function, start, new[] { (ushort)count }));
                    }
                    answer = new List<byte> { slave, (byte)(function | 0x80), 0x01 };
                    break;
            }

            answer.AddRange(ModbusUtility.CalculateCrc(answer.ToArray()));

            var payload = new List<byte> { 0x02, 0x01 };
            payload.AddRange(new byte[12]);
            payload.AddRange(answer);

            var response = new List<byte> { 0xA5, (byte)(payload.Count & 0xFF), (byte)(payload.Count >> 8), 0x10, 0x15, 0x00, 0x00 };
            response.AddRange(BitConverter.GetBytes(Serial));
            response.AddRange(payload);
            response.Add(0x00);
            response.Add(0x15);

            var result = response.ToArray();
            result[^2] = Checksum(result);
            return result;
        }

        private static byte Checksum(byte[] frame)
        {
            byte sum = 0;
            for (var i = 1; i < frame.Length - 2; i++)
            {
                sum += frame[i];
            }
            return sum;
        }

        private void Check(bool condition, string what)
        {
            if (!condition)
            {
                lock (Errors)
                {
                    Errors.Add(what);
                }
            }
        }

        private async Task<bool> ReadExactly(NetworkStream stream, byte[] buffer)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read), _cts.Token);
                if (n == 0)
                {
                    return false;
                }
                read += n;
            }
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                await _serverTask;
            }
            catch
            {
                // already stopped
            }
            _cts.Dispose();
        }
    }
}
