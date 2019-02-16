using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.MBus.Frames;
using P3.Driver.MBus.Frames.VariableData;
using Xunit;

namespace P3.Driver.MBus.Tests
{
    public class VariableDataFrameTests
    {
        [Fact]
        public void TestVariableDataFrame()
        {
            var frame = Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "066D4324CEFB161A04037EA70E0004833C000000008410FB8273AD2F00008410FB82F33C200C0200042B3901000004AB3C0000000004FB140000000004FB943C900000000483FF0400000000");
            var variableDataFrame = new VariableDataFrame();
            variableDataFrame.ParseVariableDataFrame(frame, 0, NullLogger.Instance);


            Assert.True(variableDataFrame.DataBlocks.Count == 10);

            var timePoint = variableDataFrame.DataBlocks[0];
            var energieAp = variableDataFrame.DataBlocks[1];
            var energieAm = variableDataFrame.DataBlocks[2];
            var energieRp = variableDataFrame.DataBlocks[3];
            var energieRm = variableDataFrame.DataBlocks[4];
            var energiePp = variableDataFrame.DataBlocks[5];
            var energiePm = variableDataFrame.DataBlocks[6];
            var blindleistungQp  = variableDataFrame.DataBlocks[7];
            var blindleistungQm = variableDataFrame.DataBlocks[8];
            var inkasso = variableDataFrame.DataBlocks[9];

            Assert.Equal(typeof(DateTime), timePoint.Value.GetType());
            Assert.Equal(new DateTime(2015, 6, 27, 14, 36, 03), timePoint.Value);


            Assert.Equal(960382, energieAp.Value);
            Assert.Equal(0, energieAm.Value);
            Assert.Equal(12205, energieRp.Value);
            Assert.Equal(134176, energieRm.Value);
            Assert.Equal(313, energiePp.Value);
            Assert.Equal(0, energiePm.Value);
            Assert.Equal(0, blindleistungQp.Value);
            Assert.Equal(144, blindleistungQm.Value);
            Assert.Equal(0, inkasso.Value);
        }

        [Fact]
        public void TestVariableDataFrame2()
        {
            var completeFrame = Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "68F7F768081172175885062D2C0804040000000C78175885060406E791000004142CDB00000422D90300000459B9270000045D081200000461B1150000042D5B010000142DC0010000043B1F020000143B74020000841006000000008420060000000084401400000000848040140000000084C0400600000000046D1A2F65114406518200004414B2C30000542D26020000543B03040000C4100600000000C4200600000000C4401400000000C480401400000000C4C0400600000000426C5F1C0F00000000E7E40000636600000000000000000000000000005BC9A50234530000E0B20300899C680000000000010001070709010300000000009816");

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, completeFrame) as VariableDataFrame;

            Assert.NotNull(frame);

            Assert.Equal(37351000d, frame.DataBlocks[1].Value);
            Assert.Equal(561.08, frame.DataBlocks[2].Value);
            Assert.Equal(985, frame.DataBlocks[3].Value);
            Assert.Equal(101.69, frame.DataBlocks[4].Value);
            Assert.Equal(46.160000000000004, frame.DataBlocks[5].Value);
        }

        [Fact]
        public void TestInvalidCrcTest()
        {
            var completeFrame = Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "68F7F768081172175885062D2C0804040000000C78175885060406E791000004142CDB00000422D90300000459B9270000045D081200000461B1150000042D5B010000142DC0010000043B1F020000143B74020000841006000000008420060000000084401400000000848040140000000084C0400600000000046D1A2F65114406518200004414B2C30000542D26020000543B03040000C4100600000000C4200600000000C4401400000000C480401400000000C4C0400600000000426C5F1C0F00000000E7E40000636600000000000000000000000000005BC9A50234530000E0B20300899C680000000000010001070709010300000000005816");

            var frame = MBusFrame.FromByteArray(MBusFrameType.LongFrame, NullLogger.Instance, completeFrame) as VariableDataFrame;

            Assert.Null(frame);
        }

        [Fact]
        public void Test48BitToLong()
        {
            byte[] mybytes = new byte[] { 0,0, 0, 0, 0, 25 };
            var longValue = VariableDataBlock.Bit48ToInt64(mybytes);

            Assert.Equal(25, longValue);
        }
    }
}
