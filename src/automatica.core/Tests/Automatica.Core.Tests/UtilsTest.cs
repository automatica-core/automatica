using System;
using Automatica.Core.Driver.Utility;
using Xunit;

namespace Automatica.Core.Tests
{
    
    public class UtilsTest
    {
        [Fact]
        public void TestIsBitSet1()
        {
            Assert.True(Utils.IsBitSet(0x80, 7));

            Assert.True(Utils.IsBitSet(0xFF, 2));
            Assert.True(Utils.IsBitSet(0x02, 1));
            Assert.False(Utils.IsBitSet(0x04, 1));

            Assert.Throws<ArgumentException>(() => Utils.IsBitSet(0x90, 10));

        }

        [Fact]
        public void TestSetBit()
        {
            Assert.Equal(0x81, Utils.SetBitsTo1(0x80, 0));

            Assert.Equal(0x82, Utils.SetBitsTo1(0x80, 1));
        }

        [Fact]
        public void TestByteAraryToString()
        {
            Assert.Equal("80", Utils.ByteArrayToString(new byte[] {0x80}.AsSpan()));
        }


        [Fact]
        public void TestStringToByteArray()
        {
            Assert.Equal(new byte[] {0x80, 0x64,0x50,0xFF}, Utils.StringToByteArray("806450FF"));
        }

        [Fact]
        public void TestBitValue()
        {
            Assert.Equal(1, Utils.BitValue(0x80, 7));
            Assert.Equal(1, Utils.BitValue(0xFF, 2));
            Assert.Equal(0, Utils.BitValue(0x80, 0));
        }

        [Fact]
        public void TestShiftRight()
        {
            Assert.Equal(0x40, Utils.ShiftRight(0x80, 1));
            Assert.Equal((ushort)0x40, Utils.ShiftRight((ushort)0x80, 1));
        }

        [Fact]
        public void TestShiftLeft()
        {
            Assert.Equal(0x80, Utils.ShiftLeft(0x40, 1));
            Assert.Equal((ushort)0x80, Utils.ShiftLeft((ushort)0x40, 1));
        }

        [Fact]
        public void TestGetUShort()
        {
            Assert.Equal(ushort.MaxValue, Utils.GetUShort(0xFF, 0xFF));
        }

        [Fact]
        public void TestSetsBitTo0()
        {
            Assert.Equal(0x80, Utils.SetBitsTo0(0x81, 0x01));
        }

        [Fact]
        public void TestSetsBitTo1()
        {
            Assert.Equal(0x81, Utils.SetBitsTo1(0x80, 0x80));
        }

        [Fact]
        public void TestSetBitIfTrue()
        {
            Assert.Equal(0x81, Utils.SetBitIfTrue(0x80, 0, true));
            Assert.Equal(0x81, Utils.SetBitIfTrue(0x81, 0, false));
        }
    }
}
