using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Http;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Http
{
    public class HttpResponseTests
    {
        private static readonly DateTime FixedDate = new DateTime(2026, 11, 6, 12, 30, 45, DateTimeKind.Utc);

        private static string Text(byte[] response) => Encoding.UTF8.GetString(response);

        [Fact]
        public void EmptyBody_With200_Is204NoContent_WithoutHeaders()
        {
            var raw = Text(HttpServerConnection.GetHttpResponse("HTTP/1.1", "application/hap+json", Array.Empty<byte>(), FixedDate));

            Assert.Equal("HTTP/1.1 204 No Content\r\n\r\n", raw);
        }

        [Fact]
        public void EmptyBody_WithAnErrorStatus_KeepsTheStatusAndContentLength0()
        {
            var raw = Text(HttpServerConnection.GetHttpResponse("HTTP/1.1", "application/hap+json", Array.Empty<byte>(), FixedDate, 400, "Bad Request"));

            Assert.StartsWith("HTTP/1.1 400 Bad Request\r\n", raw);
            Assert.Contains("Content-Length: 0\r\n", raw);
        }

        [Fact]
        public void Body_HasContentLengthTypeDateAndKeepAlive()
        {
            var body = Encoding.UTF8.GetBytes("{\"a\":1}");

            var raw = Text(HttpServerConnection.GetHttpResponse("HTTP/1.1", "application/hap+json", body, FixedDate));

            Assert.Equal(
                "HTTP/1.1 200 OK\r\nContent-Length: 7\r\nContent-Type: application/hap+json\r\nDate: Fri, 06 Nov 2026 12:30:45 GMT\r\nConnection: keep-alive\r\n\r\n{\"a\":1}",
                raw);
        }

        [Fact]
        public void Date_IsInvariant_UnderAnotherCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-AT");
                var raw = Text(HttpServerConnection.GetHttpResponse("HTTP/1.1", "x", new byte[] { 1 }, FixedDate));
                Assert.Contains("Date: Fri, 06 Nov 2026 12:30:45 GMT\r\n", raw);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Event_UsesTheEventProtocolLine()
        {
            var raw = Text(HttpServerConnection.GetHttpResponse("EVENT/1.0", "application/hap+json", new byte[] { (byte)'x' }, FixedDate));

            Assert.StartsWith("EVENT/1.0 200 OK\r\n", raw);
            Assert.EndsWith("\r\n\r\nx", raw);
        }

        [Fact]
        public void ContentLength_CountsBytes_NotCharacters()
        {
            var body = Encoding.UTF8.GetBytes("\"Wohnzimmer Lüster\"");

            var raw = Text(HttpServerConnection.GetHttpResponse("HTTP/1.1", "x", body, FixedDate));

            Assert.Contains($"Content-Length: {body.Length}\r\n", raw);
        }
    }

    public class EncryptedFrameEdgeCaseTests
    {
        private static HapSession Session(byte[] key = null)
        {
            if (key == null)
            {
                key = new byte[32];
                RandomNumberGenerator.Fill(key);
            }

            return new HapSession { AccessoryToControllerKey = key, ControllerToAccessoryKey = key };
        }

        private static HapSession Reader(HapSession writer) => Session(writer.AccessoryToControllerKey);

        private static byte[] Plain(int length) => Enumerable.Range(0, length).Select(a => (byte)(a * 7)).ToArray();

        [Fact]
        public void Encrypt_Empty_ProducesNoFrame()
        {
            var session = Session();

            Assert.Empty(HttpServerConnection.EncryptData(Array.Empty<byte>(), session));
            Assert.Equal(0, session.OutboundBinaryMessageCount);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1024, 1)]
        [InlineData(1025, 2)]
        [InlineData(2048, 2)]
        [InlineData(3000, 3)]
        public void Encrypt_SplitsInto1024ByteFrames(int length, int frames)
        {
            var session = Session();

            var encrypted = HttpServerConnection.EncryptData(Plain(length), session);

            Assert.Equal(length + frames * (2 + 16), encrypted.Length);
            Assert.Equal(frames, session.OutboundBinaryMessageCount);
            Assert.Equal((ushort)Math.Min(length, 1024), BitConverter.ToUInt16(encrypted, 0));
        }

        [Fact]
        public void Decrypt_AFrameOfExactly1024Bytes_IsAccepted()
        {
            var writer = Session();
            var encrypted = HttpServerConnection.EncryptData(Plain(1024), writer);

            Assert.Equal(Plain(1024), HttpServerConnection.DecryptData(encrypted, Reader(writer)));
        }

        [Fact]
        public void Decrypt_SeveralMessages_KeepsTheCounterInSync()
        {
            var writer = Session();
            var reader = Reader(writer);

            for (var i = 0; i < 5; i++)
            {
                var plain = Plain(100 + i);
                Assert.Equal(plain, HttpServerConnection.DecryptData(HttpServerConnection.EncryptData(plain, writer), reader));
            }

            Assert.Equal(5, reader.InboundBinaryMessageCount);
        }

        [Fact]
        public void Decrypt_AReplayedFrame_IsRejected()
        {
            var writer = Session();
            var reader = Reader(writer);
            var frame = HttpServerConnection.EncryptData(Plain(20), writer);

            HttpServerConnection.DecryptData(frame, reader);

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(frame, reader));
        }

        [Fact]
        public void Decrypt_FramesOutOfOrder_AreRejected()
        {
            var writer = Session();
            var first = HttpServerConnection.EncryptData(Plain(10), writer);
            var second = HttpServerConnection.EncryptData(Plain(10), writer);

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(second.Concat(first).ToArray(), Reader(writer)));
        }

        [Fact]
        public void Decrypt_WithAnotherKey_IsRejected()
        {
            var writer = Session();
            var frame = HttpServerConnection.EncryptData(Plain(10), writer);

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(frame, Session()));
        }

        [Fact]
        public void Decrypt_ATamperedLengthPrefix_IsRejected()
        {
            var writer = Session();
            var frame = HttpServerConnection.EncryptData(Plain(10), writer);
            var longer = new byte[] { 9, 0 }.Concat(frame.Skip(2)).Concat(new byte[] { 0 }).ToArray(); // AAD no longer matches

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(longer, Reader(writer)));
        }

        [Fact]
        public void Decrypt_Empty_ReturnsNothing()
        {
            var result = HttpServerConnection.DecryptData(Array.Empty<byte>(), Session(), out var remainder);

            Assert.Empty(result);
            Assert.Empty(remainder);
        }

        [Fact]
        public void Decrypt_CompleteFrameFollowedByALengthPrefixOnly_KeepsThePrefix()
        {
            var writer = Session();
            var first = HttpServerConnection.EncryptData(Plain(10), writer);
            var second = HttpServerConnection.EncryptData(Plain(10), writer);
            var reader = Reader(writer);

            var result = HttpServerConnection.DecryptData(first.Concat(second.Take(2)).ToArray(), reader, out var remainder);

            Assert.Equal(Plain(10), result);
            Assert.Equal(second.Take(2), remainder);
            Assert.Equal(Plain(10), HttpServerConnection.DecryptData(remainder.Concat(second.Skip(2)).ToArray(), reader, out remainder));
            Assert.Empty(remainder);
        }

        [Fact]
        public void Decrypt_ByteByByte_ReassemblesTheMessage()
        {
            var writer = Session();
            var plain = Plain(1500);
            var encrypted = HttpServerConnection.EncryptData(plain, writer);
            var reader = Reader(writer);
            var pending = Array.Empty<byte>();
            var output = Array.Empty<byte>();

            foreach (var b in encrypted)
            {
                output = output.Concat(HttpServerConnection.DecryptData(pending.Concat(new[] { b }).ToArray(), reader, out pending)).ToArray();
            }

            Assert.Equal(plain, output);
            Assert.Empty(pending);
            Assert.Equal(2, reader.InboundBinaryMessageCount);
        }

        [Fact]
        public void Decrypt_InvalidLengthAfterAValidFrame_IsRejected()
        {
            var writer = Session();
            var frame = HttpServerConnection.EncryptData(Plain(10), writer);
            var bogus = BitConverter.GetBytes((ushort)0xffff);

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(frame.Concat(bogus).ToArray(), Reader(writer), out _));
        }

        [Fact]
        public void StrictOverload_RejectsALengthPrefixOnly()
        {
            var writer = Session();
            var frame = HttpServerConnection.EncryptData(Plain(10), writer);

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(frame.Take(2).ToArray(), Reader(writer)));
        }

        [Fact]
        public void StrictOverload_AcceptsSeveralCompleteFrames()
        {
            var writer = Session();
            var plain = Plain(2500);

            Assert.Equal(plain, HttpServerConnection.DecryptData(HttpServerConnection.EncryptData(plain, writer), Reader(writer)));
        }
    }
}
