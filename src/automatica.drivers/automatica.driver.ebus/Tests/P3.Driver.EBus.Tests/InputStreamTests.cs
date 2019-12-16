using System;
using P3.Driver.EBus.Tests.Moq;
using Xunit;

namespace P3.Driver.EBus.Tests
{
    public class InputStreamTests
    {
        [Fact]
        public void TestPackageOk1()
        {
            var frame = new byte[]
                {0x05, 0x03, 0x09, 0x07, 0x03, 0x55, 0x00, 0x50, 0x80, 0x00, 0x62, 0xff, 0xa3, 0xff, 0xaa};
            var ebus = new EBusMoq(null);

            var recv = ebus.Write(frame);

            Assert.Equal(frame, recv);
        }

        [Fact]
        public void TestPackageOk2()
        {
            var frame = new byte[]
                {0x05, 0x03, 0x09, 0x07, 0x03, 0x55, 0x00, 0x50, 0x80, 0x00, 0x62, 0xff, 0xa3, 0xff, 0xaa};

            var wrapFrame = new byte[frame.Length + 10];
            Array.Fill(wrapFrame, (byte)0xBB, 0, wrapFrame.Length);
            Array.Copy(frame, 0, wrapFrame, 0, frame.Length);

            var ebus = new EBusMoq(null);
            var recv = ebus.Write(wrapFrame);

            Assert.Equal(frame, recv);
        }

        [Fact]
        public void TestPackageNotOk1()
        {
            var frame = new byte[]
                {0x05, 0x03, 0x09, 0x07, 0x03, 0x55, 0x00, 0x50, 0x80, 0x00, 0x62, 0xff, 0xa3, 0xff, 0xbb};

            var wrapFrame = new byte[frame.Length + 10];
            Array.Fill(wrapFrame, (byte)0xBB, 0, wrapFrame.Length);
            Array.Copy(frame, 0, wrapFrame, 0, frame.Length);

            var ebus = new EBusMoq(null);
            var recv = ebus.Write(wrapFrame);

            Assert.Null(recv);
        }
    }
}
