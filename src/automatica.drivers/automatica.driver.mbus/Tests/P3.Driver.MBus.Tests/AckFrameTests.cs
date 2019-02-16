using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Frames;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class AckFrameTests
    {
        [Fact]
        public void TestAckFrame()
        {
            var frame = MBusFrame.FromByteArray(MBusFrameType.SingleChar, NullLogger.Instance, new[] { MBus.SingleCharFrame });

            Assert.NotNull(frame);
            Assert.Equal(typeof(AckFrame), frame.GetType());
        }
    }
}
