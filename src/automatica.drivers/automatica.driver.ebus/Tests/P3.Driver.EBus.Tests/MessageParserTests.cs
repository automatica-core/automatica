using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace P3.Driver.EBus.Tests
{
    public class MessageParserTests
    {
        [Fact]
        public void TestMessageParseOk1()
        {
            var frame = new byte[]
                {0x10, 0xfe, 0xb5, 0x05, 0x04, 0x27, 0xa9, 0x15, 0xaa};
            var msg = Message.Parse(frame.AsSpan());
        }

        [Fact]
        public void TestMessageParseOk2()
        {
            var frame = new byte[]
                {0x10, 0xfe, 0xb5, 0x05, 0x04, 0x27, 0xa9, 0x15, 0xaa};
           // var msg = Message.Parse(frame.AsSpan());
        }

    

        //
    }
}
