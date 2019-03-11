using Microsoft.Extensions.Logging.Abstractions;
using P3.Knx.Core.Baos.Driver;
using P3.Knx.Core.Baos.Driver.Frames;
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
