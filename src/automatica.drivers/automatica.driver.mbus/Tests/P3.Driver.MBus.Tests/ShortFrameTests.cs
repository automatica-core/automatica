using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Frames;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class ShortFrameTests
    {
        [Fact]
        public void TestShortFrame()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.ShortFrame, NullLogger.Instance, new byte[] { 0x10, 0x40, 0xF0, 0x30, 0x16 });

            Assert.NotNull(frame);
            Assert.Equal(typeof(ShortFrame), frame.GetType());
        }
    }
}
