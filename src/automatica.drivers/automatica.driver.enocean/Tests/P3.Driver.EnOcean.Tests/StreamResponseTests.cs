using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;

namespace P3.Driver.EnOcean.Tests
{
    /// <summary>
    /// A command waits for its RESPONSE while the receive path keeps reading: the answer is handed over, not read twice.
    /// </summary>
    public class StreamResponseTests
    {
        private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(200);

        private static EnOceanPacket Response(byte code) =>
            EnOceanPacket.Parse(Esp3FramingTests.Frame(EnOcean.PacketType.Response, new[] { code }, Array.Empty<byte>()));

        private static EnOceanPacket Radio() =>
            EnOceanPacket.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray("55000707017AF637FEF7D96E3001FFFFFFFF4600DC"));

        /// <summary>
        /// The receive path of the serial stream, without a port: what the stick sends is fed in with <see cref="Receive"/>.
        /// </summary>
        private class FakeStream : BaseStream
        {
            public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

            public readonly List<EnOceanPacket> Written = new List<EnOceanPacket>();
            public Action<EnOceanPacket> OnWrite;
            public TimeSpan Timeout = TimeSpan.FromSeconds(5);

            public void Receive(EnOceanPacket packet)
            {
                if (!TryCompleteResponse(packet))
                {
                    TelegramReceived?.Invoke(this, new PacketReceivedEventArgs(EnOceanTelegramFactory.FromPacket(packet)));
                }
            }

            public override Task<EnOceanPacket> WriteFrame(EnOceanPacket frame) =>
                SendAndWaitForResponse(() =>
                {
                    Written.Add(frame);
                    OnWrite?.Invoke(frame);
                    return Task.CompletedTask;
                }, Timeout);

            public override Task<bool> Open() => Task.FromResult(true);
            public override Task<bool> Close() => Task.FromResult(true);
            public override void Pause() { }
            public override void Continue() { }
        }

        private static EnOceanPacket Command() => new CommonCommandPacket(CommonCommands.CO_RD_IDBASE).ToPacket();

        [Fact]
        public async Task ResponseRightAfterTheWrite_IsReturned()
        {
            var stream = new FakeStream();
            var ok = Response(0x00);
            stream.OnWrite = _ => stream.Receive(ok);

            var answer = await stream.WriteFrame(Command());

            Assert.Same(ok, answer);
        }

        [Fact]
        public async Task RadioTelegramBeforeTheResponse_StillReachesTheDriver()
        {
            var stream = new FakeStream();
            var telegrams = new List<EnOceanTelegram>();
            stream.TelegramReceived += (s, e) => telegrams.Add(e.Telegram);
            var ok = Response(0x00);

            var write = stream.WriteFrame(Command());
            stream.Receive(Radio());
            stream.Receive(ok);

            Assert.Same(ok, await write);
            var telegram = Assert.Single(telegrams);
            Assert.IsType<RadioErp1Packet>(telegram);
        }

        [Fact]
        public async Task SendFromInsideTheTelegramHandler_DoesNotBlockTheReceivePath()
        {
            // the reply to a valve is sent while its status telegram is being handled
            var stream = new FakeStream();
            Task<EnOceanPacket> reply = null;
            stream.TelegramReceived += (s, e) => reply = stream.WriteFrame(Command());

            stream.Receive(Radio());

            Assert.NotNull(reply);
            Assert.False(reply.IsCompleted);
            Assert.Single(stream.Written);

            var ok = Response(0x00);
            stream.Receive(ok);
            Assert.Same(ok, await reply);
        }

        [Fact]
        public async Task NoResponse_IsNullAfterTheTimeout_AndALateResponseIsNotTakenByTheNextCommand()
        {
            var stream = new FakeStream { Timeout = Short };
            var telegrams = new List<EnOceanTelegram>();
            stream.TelegramReceived += (s, e) => telegrams.Add(e.Telegram);

            Assert.Null(await stream.WriteFrame(Command()));

            stream.Receive(Response(0x00));
            Assert.Single(telegrams);
        }

        [Fact]
        public async Task TwoCommands_AreSentOneAfterTheOther_EachGetsItsOwnResponse()
        {
            var stream = new FakeStream();
            var first = Response(0x00);
            var second = Response(0x02);

            var a = stream.WriteFrame(Command());
            var b = stream.WriteFrame(Command());
            await Task.Delay(50);
            Assert.Single(stream.Written);

            stream.Receive(first);
            Assert.Same(first, await a);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (stream.Written.Count < 2 && sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(10);
            }
            Assert.Equal(2, stream.Written.Count);

            stream.Receive(second);
            Assert.Same(second, await b);
        }
    }
}
