using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;
using Xunit;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    public class ConnectionDataTests
    {
        private static readonly byte[] StateUuid = { 16, 65, 38, 22, 1, 146, 7, 66, 255, 255, 201, 38, 162, 106, 141, 33 };
        private static readonly byte[] IconUuid = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        private static byte[] HeaderBytes(HeaderIdentifier identifier, uint length, byte info = 0)
        {
            return new byte[] { 0x03, (byte)identifier, info, 0 }.Concat(BitConverter.GetBytes(length)).ToArray();
        }

        private static byte[] ValueState(double value)
        {
            return StateUuid.Concat(BitConverter.GetBytes(value)).ToArray();
        }

        private static (LoxoneMiniserverConnection Connection, List<BinaryMessage> Messages) CreateConnection()
        {
            var connection = new LoxoneMiniserverConnection("127.0.0.1", 80, "user", "password", NullLogger.Instance);
            var messages = new List<BinaryMessage>();
            connection.OnMessage += (sender, args) => messages.Add(args.Message);
            return (connection, messages);
        }

        [Fact]
        public void EstimatedHeader_IsSkipped_AndExactHeaderIsParsed()
        {
            var (connection, messages) = CreateConnection();

            var payload = ValueState(21.5);
            var data = HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)payload.Length, 0x80)
                .Concat(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)payload.Length))
                .Concat(payload)
                .ToArray();

            connection.ProcessData(data);

            var msg = Assert.IsType<EventTableOfValueStates>(Assert.Single(messages));
            Assert.Equal(21.5, msg.Values.Single().Value);
        }

        [Fact]
        public void SeveralMessagesInOneFrame_AreAllParsed()
        {
            var (connection, messages) = CreateConnection();

            var first = ValueState(1);
            var second = ValueState(2);
            var data = HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)first.Length).Concat(first)
                .Concat(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)second.Length)).Concat(second)
                .ToArray();

            connection.ProcessData(data);

            Assert.Equal(new[] { 1d, 2d }, messages.Cast<EventTableOfValueStates>().Select(a => a.Values.Single().Value));
        }

        [Fact]
        public void HeaderAndPayloadInSeparateFrames_AreJoined()
        {
            var (connection, messages) = CreateConnection();

            var payload = ValueState(3);
            connection.ProcessData(HeaderBytes(HeaderIdentifier.EventTableOfValueStates, (uint)payload.Length));
            Assert.Empty(messages);

            connection.ProcessData(payload);

            var msg = Assert.IsType<EventTableOfValueStates>(Assert.Single(messages));
            Assert.Equal(3, msg.Values.Single().Value);
        }

        [Fact]
        public void TextState_UsesIconUuid()
        {
            var text = Encoding.UTF8.GetBytes("hello");
            var data = StateUuid.Concat(IconUuid).Concat(BitConverter.GetBytes((uint)text.Length)).Concat(text)
                .Concat(new byte[3]) // pad to 4 bytes
                .ToArray();

            var header = new Header(HeaderBytes(HeaderIdentifier.EventTableOfTextStates, (uint)data.Length));
            var msg = (EventTableOfTextStates)BinaryMessage.GenerateBinaryMessage(header, data);

            var entry = msg.Values.Single();
            Assert.Equal("hello", entry.Value.Text);
            Assert.Equal(new LoxoneUuid(IconUuid.ToArray()).Uuid, entry.Value.IconId.Uuid);
            Assert.NotEqual(entry.Key.Uuid, entry.Value.IconId.Uuid);
        }

        [Fact]
        public void FormatValue_IsCultureInvariant()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.Equal("1.5", LoxoneMiniserverConnection.FormatValue(1.5));
                Assert.Equal("0.25", LoxoneMiniserverConnection.FormatValue(0.25f));
                Assert.Equal("42", LoxoneMiniserverConnection.FormatValue(42));
                Assert.Equal("on", LoxoneMiniserverConnection.FormatValue(true));
                Assert.Equal("off", LoxoneMiniserverConnection.FormatValue(false));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }
    }
}
