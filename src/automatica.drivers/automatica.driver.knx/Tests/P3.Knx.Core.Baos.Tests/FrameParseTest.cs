using Microsoft.Extensions.Logging.Abstractions;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Frames;
using System.IO;
using System.Linq;
using Xunit;

namespace P3.Knx.Core.Baos.Tests
{
    public class FrameParseTest
    {
        [Fact]
        public void TestLongFrameInvalidChecksum()
        {
            var byteArray = new byte[] { 0x68, 0x11, 0x11, 0x68, 0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x10, 0x01, 0x01, 0x00, 0x02, 0x10, 0x01, 0x01, 0x96, 0x16 };

            var frame = BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, byteArray);

            Assert.Null(frame);
        }

        [Fact]
        public void TestLongFrameValidChecksum()
        {
            var byteArray = new byte[] { 0x68, 0x11, 0x11, 0x68, 0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x10, 0x01, 0x01, 0x00, 0x02, 0x10, 0x01, 0x01, 0x92, 0x16 };

            var frame = BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, byteArray);

            Assert.True(frame.ChecksumCheck());
        }


        [Fact]
        public void TestShortFrameIsFourBytes()
        {
            // reset indication 10 C0 C0 16 (start byte already read), followed by the start of the next frame
            var stream = new MemoryStream(new byte[] { 0xC0, 0xC0, 0x16, 0x68 });

            var frame = BaosSerial.ReadShortFrame(stream.Read, NullLogger.Instance);

            Assert.IsType<ShortFrame>(frame);
            Assert.Equal(0xC0, frame.ControlField);
            Assert.Equal(3, stream.Position); // the next frame is not consumed
            Assert.Equal(new byte[] { 0x10, 0x40, 0x40, 0x16 }, ShortFrame.CreateResetFrame().ToByteFrame().ToArray());
        }

        [Fact]
        public void TestShortFrameInvalidStopByte()
        {
            var stream = new MemoryStream(new byte[] { 0xC0, 0xC0, 0x17 });

            Assert.Null(BaosSerial.ReadShortFrame(stream.Read, NullLogger.Instance));
        }

        [Fact]
        public void TestParseDatapointValues()
        {
            var byteArray = new byte[] { 0x68, 0x11, 0x11, 0x68, 0xF3, 0xF0, 0x85, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x10, 0x01, 0x01, 0x00, 0x02, 0x10, 0x01, 0x01, 0x92, 0x16 };

            var frame = BaosFrame.FromByteArray(BaosFrameType.LongFrame, NullLogger.Instance, byteArray);

            var data = BaosDriver.ParseDatapointValues((LongFrame)frame).ToList();


            Assert.Equal(2, data.Count);

            //dp 1
            Assert.Equal(1, data[0].DatapointId);
            Assert.Equal(0x10, data[0].State);
            Assert.Equal(0x1, data[0].Length);
            Assert.Equal(1, data[0].Data.ToArray()[0]);

            //dp 2
            Assert.Equal(2, data[1].DatapointId);
            Assert.Equal(0x10, data[1].State);
            Assert.Equal(0x1, data[1].Length);
            Assert.Equal(1, data[1].Data.ToArray()[0]);
        }
    }
}
