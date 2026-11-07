using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Config;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Udp;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    /// <summary>A real <see cref="MBusUdp"/> against a fake M-Bus/UDP gateway on 127.0.0.1 with an ephemeral port.</summary>
    public sealed class MBusUdpIntegrationTests : IDisposable
    {
        private readonly UdpClient _gateway = new(new IPEndPoint(IPAddress.Loopback, 0));
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<byte[]> _received = new();
        private Func<byte[], byte[]> _reply = _ => null;
        private readonly Task _loop;

        private static readonly byte[] MeterUserData = Utils.StringToByteArray(
            "78563412" + "2D2C" + "01" + "07" + "01" + "00" + "0000" + "0413" + "39300000" + "025AE100");

        public MBusUdpIntegrationTests()
        {
            _loop = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    UdpReceiveResult request;
                    try
                    {
                        request = await _gateway.ReceiveAsync(_cts.Token);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    _received.Enqueue(request.Buffer);
                    var answer = _reply(request.Buffer);
                    if (answer != null)
                    {
                        await _gateway.SendAsync(answer, answer.Length, request.RemoteEndPoint);
                    }
                }
            });
        }

        public void Dispose()
        {
            _cts.Cancel();
            _gateway.Dispose();
            try
            {
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
            }

            _cts.Dispose();
        }

        private int GatewayPort => ((IPEndPoint)_gateway.Client.LocalEndPoint!).Port;

        private MBusUdp CreateClient(int timeout = 2000, bool resetBeforeRead = false) =>
            new(new MBusUdpConfig { IpAddress = IPAddress.Loopback, Port = GatewayPort, Timeout = timeout, ResetBeforeRead = resetBeforeRead },
                new RecordingTelegramMonitor(), NullLogger.Instance);

        private static bool IsShortFrame(byte[] data, byte control) => data.Length == 5 && data[0] == 0x10 && data[1] == control && data[4] == 0x16;

        [Fact]
        public async Task ReadDevice_GatewayAnswersWithLongFrame_ReturnsParsedVariableDataFrame()
        {
            _reply = req => IsShortFrame(req, MBus.ControlMaskReqUd2) ? MBusFrameTests.LongFrame(0x08, req[2], 0x72, MeterUserData) : null;
            var client = CreateClient();

            var frame = await client.ReadDevice(3, false, 300);

            var data = Assert.IsType<VariableDataFrame>(frame);
            Assert.Equal(12345678, data.Identifier);
            Assert.Equal(3, data.AddressField);
            Assert.Equal(2, data.DataBlocks.Count);
            Assert.Equal(12.345, (double)data.DataBlocks[0].Value, 9);
            Assert.Equal(22.5, (double)data.DataBlocks[1].Value, 9);

            // the request on the wire is REQ_UD2 to address 3
            Assert.Contains(_received, r => r.AsSpan().SequenceEqual(new byte[] { 0x10, 0x5B, 0x03, 0x5E, 0x16 }));
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_ResetBeforeRead_SendsSndNkeThenReqUd2()
        {
            _reply = req =>
            {
                if (IsShortFrame(req, MBus.ControlMaskSndNke))
                {
                    return new byte[] { 0xE5 };
                }

                return IsShortFrame(req, MBus.ControlMaskReqUd2) ? MBusFrameTests.LongFrame(0x08, req[2], 0x72, MeterUserData) : null;
            };
            var client = CreateClient(resetBeforeRead: true);

            var frame = await client.ReadDevice(7, false, 300);

            Assert.IsType<VariableDataFrame>(frame);
            var requests = _received.ToArray();
            Assert.Equal(2, requests.Length);
            Assert.Equal(new byte[] { 0x10, 0x40, 0x07, 0x47, 0x16 }, requests[0]);
            Assert.Equal(new byte[] { 0x10, 0x5B, 0x07, 0x62, 0x16 }, requests[1]);
            client.Close();
        }

        [Fact]
        public async Task ResetDevice_GatewayAcks_ReturnsAckFrame()
        {
            _reply = req => IsShortFrame(req, MBus.ControlMaskSndNke) ? new byte[] { 0xE5 } : null;
            var client = CreateClient();

            var frame = await client.ResetDevice(1);

            Assert.IsType<AckFrame>(frame);
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_NoAnswer_ReturnsNullAfterTheTimeout()
        {
            var client = CreateClient(timeout: 250);
            var watch = Stopwatch.StartNew();

            var frame = await client.ReadDevice(1, false, 100);

            watch.Stop();
            Assert.Null(frame);
            // the device timeout (100) never shortens the connection timeout (250)
            Assert.True(watch.ElapsedMilliseconds >= 200, $"returned after {watch.ElapsedMilliseconds} ms");
            Assert.True(watch.ElapsedMilliseconds < 5000, $"returned after {watch.ElapsedMilliseconds} ms");
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_AnswerWithBadChecksum_ReturnsNull()
        {
            _reply = req =>
            {
                var frame = MBusFrameTests.LongFrame(0x08, req[2], 0x72, MeterUserData);
                frame[^2] ^= 0xFF;
                return frame;
            };
            var client = CreateClient();

            Assert.Null(await client.ReadDevice(1, false, 300));
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_AnswerWithWrongLengthField_ReturnsNull()
        {
            _reply = req =>
            {
                var frame = MBusFrameTests.LongFrame(0x08, req[2], 0x72, MeterUserData);
                frame[1] = frame[2] = (byte)(frame[1] + 1);
                return frame;
            };
            var client = CreateClient();

            Assert.Null(await client.ReadDevice(1, false, 300));
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_GarbageAnswer_ReturnsNull()
        {
            _reply = _ => new byte[] { 0x01, 0x02, 0x03 };
            var client = CreateClient();

            Assert.Null(await client.ReadDevice(1, false, 300));
            client.Close();
        }

        [Fact]
        public async Task ReadDevice_NotifiesTheTelegramMonitorForRequestAndResponse()
        {
            _reply = req => MBusFrameTests.LongFrame(0x08, req[2], 0x72, MeterUserData);
            var monitor = new RecordingTelegramMonitor();
            var client = new MBusUdp(new MBusUdpConfig { IpAddress = IPAddress.Loopback, Port = GatewayPort, Timeout = 2000 }, monitor, NullLogger.Instance);

            await client.ReadDevice(4, false, 300);

            Assert.Equal(2, monitor.Telegrams.Count);
            Assert.Equal("4", monitor.Telegrams.ToArray()[0].Target);
            client.Close();
        }

        private sealed class RecordingTelegramMonitor : ITelegramMonitorInstance
        {
            public ConcurrentQueue<(string Source, string Target, string Data)> Telegrams { get; } = new();

            public Task NotifyTelegram(TelegramDirection direction, string sourceAddress, string targetAddress, string data, string additionalMessage)
            {
                Telegrams.Enqueue((sourceAddress, targetAddress, data));
                return Task.CompletedTask;
            }
        }
    }
}
