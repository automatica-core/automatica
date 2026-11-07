using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;
using P3.Driver.EnOcean.Data.Packets;
using Xunit;
using static P3.Driver.EnOcean.Tests.Esp3FramingTests;

namespace P3.Driver.EnOcean.Tests
{
    /// <summary>
    /// <see cref="Driver"/> over an in-memory <see cref="BaseStream"/>.
    /// </summary>
    public class DriverTests
    {
        private sealed class FakeStream : BaseStream
        {
            public bool OpenResult { get; set; } = true;
            public List<byte[]> Written { get; } = new();
            public Func<EnOceanPacket, EnOceanPacket> Answer { get; set; } = _ => null;
            public bool Closed { get; private set; }

            public override event EventHandler<PacketReceivedEventArgs> TelegramReceived;

            public override Task<bool> Open() => Task.FromResult(OpenResult);

            public override Task<bool> Close()
            {
                Closed = true;
                return Task.FromResult(true);
            }

            public override Task<EnOceanPacket> WriteFrame(EnOceanPacket frame)
            {
                Written.Add(frame.RawData.ToArray());
                return Task.FromResult(Answer(frame));
            }

            public override void Pause()
            {
            }

            public override void Continue()
            {
            }

            public void Receive(EnOceanTelegram telegram) => TelegramReceived?.Invoke(this, new PacketReceivedEventArgs(telegram));
        }

        private static EnOceanPacket Response(params byte[] data) => EnOceanPacket.Parse(Frame(EnOcean.PacketType.Response, data, Array.Empty<byte>()));

        private static RadioErp1Packet Radio(params byte[] erp1) =>
            (RadioErp1Packet)EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(Frame(EnOcean.PacketType.RadioErp1, erp1, BroadcastOptional)));

        [Fact]
        public async Task Open_StreamFails_ReturnsFalse_WithoutSending()
        {
            var stream = new FakeStream { OpenResult = false };
            var driver = new Driver(stream);

            Assert.False(await driver.Open());
            Assert.Empty(stream.Written);
        }

        [Fact]
        public async Task Open_SendsReadIdBase_AndTakesTheBaseIdFromARetOkResponse()
        {
            var stream = new FakeStream { Answer = _ => Response(0x00, 0xFF, 0x80, 0x00, 0x00, 0x0A) };
            var driver = new Driver(stream);

            Assert.True(await driver.Open());

            Assert.Equal(new[] { Hex("5500010005700838") }, stream.Written);
            Assert.Equal(new byte[] { 0xFF, 0x80, 0x00, 0x00 }, driver.IdBase.ToArray());
        }

        [Fact]
        public async Task Open_ErrorResponse_LeavesTheIdBaseEmpty()
        {
            var stream = new FakeStream { Answer = _ => Response(0x02, 0xFF, 0x80, 0x00, 0x00) };
            var driver = new Driver(stream);

            Assert.True(await driver.Open());
            Assert.Equal(0, driver.IdBase.Length);
        }

        [Fact]
        public async Task Open_RadioTelegramInsteadOfTheResponse_LeavesTheIdBaseEmpty()
        {
            // regression: the stream returns the next frame, a radio telegram arriving first was taken as id base
            var stream = new FakeStream { Answer = _ => EnOceanPacket.Parse(Frame(EnOcean.PacketType.RadioErp1, Hex("A50123450E0181B74400"), BroadcastOptional)) };
            var driver = new Driver(stream);

            Assert.True(await driver.Open());
            Assert.Equal(0, driver.IdBase.Length);
        }

        [Fact]
        public async Task Open_ShortResponse_LeavesTheIdBaseEmpty()
        {
            var stream = new FakeStream { Answer = _ => Response(0x00, 0xFF) };
            var driver = new Driver(stream);

            Assert.True(await driver.Open());
            Assert.Equal(0, driver.IdBase.Length);
        }

        [Fact]
        public async Task Open_NoAnswer_StillOpens()
        {
            var driver = new Driver(new FakeStream());

            Assert.True(await driver.Open());
            Assert.Equal(0, driver.IdBase.Length);
        }

        [Fact]
        public async Task SendTelegram_UsesTheIdBase_AndRaisesSentAndAnswer()
        {
            var stream = new FakeStream();
            stream.Answer = frame => frame.Data.Span[0] == (byte)CommonCommands.CO_RD_IDBASE && frame.Data.Length == 1
                ? Response(0x00, 0xFF, 0x80, 0x00, 0x10)
                : Response(0x00);
            var driver = new Driver(stream);
            await driver.Open();

            var sent = new List<PacketSentEventArgs>();
            var answers = new List<AnswerReceviedEventArgs>();
            driver.PacketSent += (_, e) => sent.Add(e);
            driver.AnswerReceived += (_, e) => answers.Add(e);

            var answer = await driver.SendTelegram(new RadioErp1Packet(Rorg.Rps, new byte[] { 0x10 }));

            Assert.NotNull(answer);
            Assert.Single(sent);
            Assert.Single(answers);
            var radio = Assert.IsType<RadioErp1Packet>(EnOceanTelegramFactory.FromPacket(EnOceanPacket.Parse(stream.Written[^1])));
            Assert.Equal("FF800010", radio.SenderIdString);
        }

        [Fact]
        public async Task SendRawTelegram_NoAnswer_RaisesNoAnswerEvent()
        {
            var stream = new FakeStream();
            var driver = new Driver(stream);
            var answers = 0;
            driver.AnswerReceived += (_, _) => answers++;

            Assert.Null(await driver.SendRawTelegram(new CommonCommandPacket(CommonCommands.CO_RD_VERSION).ToPacket()));
            Assert.Equal(0, answers);
            Assert.Single(stream.Written);
        }

        [Fact]
        public async Task TeachInMode_RpsTelegram_RaisesTeachInAndTelegram()
        {
            var stream = new FakeStream();
            var driver = new Driver(stream);
            await driver.Open();
            var teachIns = 0;
            var telegrams = 0;
            driver.TeachInReceived += (_, _) => teachIns++;
            driver.TelegramReceived += (_, _) => telegrams++;

            driver.StartTeachInMode();
            stream.Receive(Radio(0xF6, 0x30, 0xFE, 0xF7, 0xD9, 0x6E, 0x30));

            Assert.Equal(1, teachIns);
            Assert.Equal(1, telegrams);
        }

        [Fact]
        public async Task TeachInMode_FourBsDataTelegram_IsNoTeachIn()
        {
            var stream = new FakeStream();
            var driver = new Driver(stream);
            await driver.Open();
            var teachIns = 0;
            var telegrams = 0;
            driver.TeachInReceived += (_, _) => teachIns++;
            driver.TelegramReceived += (_, _) => telegrams++;

            driver.StartTeachInMode();
            stream.Receive(Radio(0xA5, 0x01, 0x23, 0x45, 0x0E, 0x01, 0x81, 0xB7, 0x44, 0x00));

            Assert.Equal(0, teachIns);
            Assert.Equal(1, telegrams);
        }

        [Fact]
        public async Task TeachInModeStopped_RaisesOnlyTelegram()
        {
            var stream = new FakeStream();
            var driver = new Driver(stream);
            await driver.Open();
            var teachIns = 0;
            var telegrams = 0;
            driver.TeachInReceived += (_, _) => teachIns++;
            driver.TelegramReceived += (_, _) => telegrams++;

            driver.StartTeachInMode();
            driver.StopTeachInMode();
            stream.Receive(Radio(0xF6, 0x30, 0xFE, 0xF7, 0xD9, 0x6E, 0x30));

            Assert.Equal(0, teachIns);
            Assert.Equal(1, telegrams);
        }

        [Fact]
        public async Task Close_ClosesTheStream_AndStopsForwardingTelegrams()
        {
            var stream = new FakeStream();
            var driver = new Driver(stream);
            await driver.Open();
            var telegrams = 0;
            driver.TelegramReceived += (_, _) => telegrams++;

            driver.Close();
            stream.Receive(Radio(0xF6, 0x30, 0xFE, 0xF7, 0xD9, 0x6E, 0x30));

            Assert.True(stream.Closed);
            Assert.Equal(0, telegrams);
        }

        private static byte[] Hex(string hex) => Automatica.Core.Driver.Utility.Utils.StringToByteArray(hex);
    }
}
