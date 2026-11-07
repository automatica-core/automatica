using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using P3.Driver.EnOcean.Tcp;
using Xunit;
using static P3.Driver.EnOcean.Tests.Esp3FramingTests;

namespace P3.Driver.EnOcean.Tests
{
    /// <summary>
    /// <see cref="TcpSerialStream"/> against a real TCP gateway on 127.0.0.1 (ephemeral port).
    /// </summary>
    public class TcpSerialStreamTests : IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private TcpListener _listener;
        private TcpClient _gateway;
        private TcpSerialStream _stream;
        private readonly ConcurrentQueue<EnOceanTelegram> _received = new();

        private static readonly byte[] IdBaseResponse = Frame(EnOcean.PacketType.Response, new byte[] { 0x00, 0xFF, 0x80, 0x00, 0x00 }, Array.Empty<byte>());

        public async Task InitializeAsync()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _stream = new TcpSerialStream("127.0.0.1", port);
            _stream.TelegramReceived += (_, e) => _received.Enqueue(e.Telegram);

            var accept = _listener.AcceptTcpClientAsync();
            Assert.True(await _stream.Open());
            _gateway = await accept.WaitAsync(Timeout);
        }

        public async Task DisposeAsync()
        {
            await _stream.Close();
            _gateway?.Dispose();
            _listener.Stop();
        }

        private async Task<byte[]> ReadFromGateway(int count)
        {
            using var cts = new CancellationTokenSource(Timeout);
            var buffer = new byte[count];
            await _gateway.GetStream().ReadExactlyAsync(buffer, cts.Token);
            return buffer;
        }

        /// <summary>
        /// Answers the next request after the receiver loop surely saw the pause (it polls every 100 ms).
        /// </summary>
        private async Task<byte[]> AnswerNextRequest(int requestLength, byte[] answer)
        {
            var request = await ReadFromGateway(requestLength);
            await Task.Delay(150);
            await _gateway.GetStream().WriteAsync(answer);
            return request;
        }

        private async Task WaitForTelegrams(int count)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (_received.Count < count && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        [Fact]
        public async Task WriteFrame_SendsTheWholeFrame_AndReturnsTheAnswer()
        {
            var command = new CommonCommandPacket(CommonCommands.CO_RD_IDBASE).ToPacket();
            var gateway = AnswerNextRequest(command.RawData.Length, IdBaseResponse);

            var answer = await _stream.WriteFrame(command);

            Assert.Equal(command.RawData.ToArray(), await gateway);
            Assert.NotNull(answer);
            Assert.Equal(EnOcean.PacketType.Response, answer.PacketType);
            Assert.Equal(new byte[] { 0x00, 0xFF, 0x80, 0x00, 0x00 }, answer.Data.ToArray());
            Assert.Empty(_received); // the answer belongs to the writer, not to the receive loop
        }

        [Fact]
        public async Task WriteFrame_LargeFrame_IsSentCompletelyBeforeTheAnswerIsRead()
        {
            // a frame bigger than one segment: the send has to complete (be awaited) before the answer is read
            var data = Enumerable.Range(0, 4000).Select(a => (byte)a).ToArray();
            var frame = EnOceanPacket.CreateNew(new byte[] { 0x55, 0x0F, 0xA0, 0x00, (byte)EnOcean.PacketType.CommonCommand }, data, Array.Empty<byte>());
            var gateway = AnswerNextRequest(frame.RawData.Length, Frame(EnOcean.PacketType.Response, new byte[] { 0x00 }, Array.Empty<byte>()));

            var answer = await _stream.WriteFrame(frame);

            Assert.Equal(frame.RawData.ToArray(), await gateway);
            Assert.Equal(EnOcean.PacketType.Response, answer.PacketType);
        }

        [Fact]
        public async Task Driver_Open_ReadsTheIdBaseOverTcp()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var stream = new TcpSerialStream("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port);
                var driver = new Driver(stream);

                var accept = listener.AcceptTcpClientAsync();
                var open = driver.Open();
                using var gateway = await accept.WaitAsync(Timeout);

                using var cts = new CancellationTokenSource(Timeout);
                var request = new byte[8];
                await gateway.GetStream().ReadExactlyAsync(request, cts.Token);
                await Task.Delay(150);
                await gateway.GetStream().WriteAsync(IdBaseResponse, cts.Token);

                Assert.True(await open.WaitAsync(Timeout));
                Assert.Equal(Hex("5500010005700838"), request);
                Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, 0x00 }, driver.IdBase.ToArray());

                driver.Close();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ReceiveLoop_FrameSplitOverManyTcpWrites_IsReassembled()
        {
            var frame = Frame(EnOcean.PacketType.RadioErp1, Hex("A50123450E0181B74400"), BroadcastOptional);

            foreach (var chunk in frame.Chunk(3))
            {
                await _gateway.GetStream().WriteAsync(chunk);
                await _gateway.GetStream().FlushAsync();
                await Task.Delay(15);
            }

            await WaitForTelegrams(1);

            var radio = Assert.IsType<RadioErp1Packet>(Assert.Single(_received));
            Assert.Equal(Rorg.FourBs, radio.Rorg);
            Assert.Equal("0181B744", radio.SenderIdString);
        }

        [Fact]
        public async Task ReceiveLoop_SeveralFramesInOneWrite_AreAllDelivered()
        {
            var a = Frame(EnOcean.PacketType.RadioErp1, Hex("F630FEF7D96E30"), BroadcastOptional);
            var b = Frame(EnOcean.PacketType.RadioErp1, Hex("F610FEF7D96F30"), BroadcastOptional);
            var c = Frame(EnOcean.PacketType.RadioErp1, Hex("D2010203040506070800"), BroadcastOptional);

            await _gateway.GetStream().WriteAsync(a.Concat(b).Concat(c).ToArray());
            await WaitForTelegrams(3);

            var senders = _received.Cast<RadioErp1Packet>().Select(t => t.SenderIdString).ToArray();
            Assert.Equal(new[] { "FEF7D96E", "FEF7D96F", "05060708" }, senders);
        }

        [Fact]
        public async Task ReceiveLoop_ResponseFramesAreDeliveredAsResponsePackets()
        {
            await _gateway.GetStream().WriteAsync(Frame(EnOcean.PacketType.Response, new byte[] { 0x00 }, Array.Empty<byte>()));
            await WaitForTelegrams(1);

            Assert.IsType<ResponsePacket>(Assert.Single(_received));
        }

        [Fact]
        public async Task ReceiveLoop_UnsupportedPacketType_IsSkipped()
        {
            var ev = Frame(EnOcean.PacketType.Event, new byte[] { 0x02 }, Array.Empty<byte>());
            var radio = Frame(EnOcean.PacketType.RadioErp1, Hex("F630FEF7D96E30"), BroadcastOptional);

            await _gateway.GetStream().WriteAsync(ev.Concat(radio).ToArray());
            await WaitForTelegrams(1);
            await Task.Delay(150);

            Assert.IsType<RadioErp1Packet>(Assert.Single(_received));
        }

        [Fact]
        public async Task WriteFrame_AfterTheGatewayClosed_ReturnsNullWithoutThrowing()
        {
            _gateway.Client.LingerState = new LingerOption(true, 0);
            _gateway.Close();
            _gateway = null;

            var answer = await _stream.WriteFrame(new CommonCommandPacket(CommonCommands.CO_RD_IDBASE).ToPacket()).WaitAsync(Timeout);

            Assert.Null(answer);
        }

        private static byte[] Hex(string hex) => Automatica.Core.Driver.Utility.Utils.StringToByteArray(hex);
    }
}
