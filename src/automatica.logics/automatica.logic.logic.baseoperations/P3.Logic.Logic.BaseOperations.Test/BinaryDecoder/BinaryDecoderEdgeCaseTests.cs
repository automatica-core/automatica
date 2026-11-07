using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Logic.BaseOperations.BinaryDecoder;
using Xunit;

namespace P3.Logic.Logic.BaseOperations.Tests.BinaryDecoder
{
    public class BinaryDecoderEdgeCaseTests : LogicTest<BinaryDecoderLogicFactory>
    {
        private static readonly Guid[] Outputs =
        {
            BinaryDecoderLogicFactory.RuleOutput1, BinaryDecoderLogicFactory.RuleOutput2,
            BinaryDecoderLogicFactory.RuleOutput3, BinaryDecoderLogicFactory.RuleOutput4,
            BinaryDecoderLogicFactory.RuleOutput5, BinaryDecoderLogicFactory.RuleOutput6,
            BinaryDecoderLogicFactory.RuleOutput7, BinaryDecoderLogicFactory.RuleOutput8
        };

        private IList<ILogicOutputChanged> Set(object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(BinaryDecoderLogicFactory.RuleInput1), Dispatchable, value);

        private static void AssertBits(IList<ILogicOutputChanged> outputs, int expectedByte)
        {
            Assert.Equal(8, outputs.Count);
            for (var bit = 0; bit < 8; bit++)
            {
                Assert.Equal(Outputs[bit], outputs[bit].Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);
                Assert.Equal((expectedByte & (1 << bit)) != 0, outputs[bit].Value);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void InputValueChanged_SingleBit_OnlyMatchingOutputIsTrue(int bit)
        {
            var outputs = Set(1 << bit);

            AssertBits(outputs, 1 << bit);
            Assert.Single(outputs, o => (bool)o.Value);
        }

        [Theory]
        [InlineData(0, 0x00)]
        [InlineData(255, 0xFF)]
        [InlineData(256, 0x00)]      // only the low byte is decoded
        [InlineData(257, 0x01)]
        [InlineData(0x1A5, 0xA5)]
        [InlineData(-1, 0xFF)]       // two's complement low byte
        [InlineData(-128, 0x80)]
        [InlineData(-256, 0x00)]
        public void InputValueChanged_Integer_DecodesLowByte(int value, int expectedByte)
        {
            AssertBits(Set(value), expectedByte);
        }

        [Fact]
        public void InputValueChanged_LongMaxValue_DecodesLowByte()
        {
            AssertBits(Set(long.MaxValue), 0xFF);
        }

        [Fact]
        public void InputValueChanged_UnsignedAndSmallTypes_AreDecoded()
        {
            AssertBits(Set((byte)0x0F), 0x0F);
            AssertBits(Set((ushort)0xF0F0), 0xF0);
            AssertBits(Set(0xC3u), 0xC3);
            AssertBits(Set((short)-2), 0xFE);
        }

        [Theory]
        [InlineData(true, 0x01)]
        [InlineData(false, 0x00)]
        public void InputValueChanged_Bool_DecodesAsOneOrZero(bool value, int expectedByte)
        {
            AssertBits(Set(value), expectedByte);
        }

        [Theory]
        [InlineData(3.7, 0x04)]   // Convert.ToInt64 rounds to nearest
        [InlineData(2.5, 0x02)]   // banker's rounding: 2.5 -> 2
        [InlineData(3.5, 0x04)]   // banker's rounding: 3.5 -> 4
        [InlineData(-0.4, 0x00)]
        public void InputValueChanged_Double_IsRoundedBeforeDecoding(double value, int expectedByte)
        {
            AssertBits(Set(value), expectedByte);
        }

        [Fact]
        public void InputValueChanged_NumericString_IsParsed()
        {
            AssertBits(Set("5"), 0x05);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void InputValueChanged_NotConvertibleBeforeAnyValue_EmitsNothing(object value)
        {
            Assert.Empty(Set(value));
        }

        [Fact]
        public void InputValueChanged_NotConvertibleAfterValue_EmitsNothingAndKeepsLastByte()
        {
            Set(0x0A);

            Assert.Empty(Set("abc"));
            AssertBits(Set(null), 0x0A);
        }

        [Fact]
        public void InputValueChanged_NullBeforeAnyValue_EmitsNothing()
        {
            Assert.Empty(Set(null));
        }

        [Fact]
        public void InputValueChanged_NullAfterValue_ReEmitsLastByte()
        {
            Set(0x81);

            AssertBits(Set(null), 0x81);
        }

        [Fact]
        public void InputValueChanged_SameValueTwice_IsEmittedAgain()
        {
            AssertBits(Set(6), 0x06);
            AssertBits(Set(6), 0x06);
        }

        [Fact]
        public void InputValueChanged_ValueChanges_AllEightOutputsAreRecomputed()
        {
            AssertBits(Set(0xFF), 0xFF);
            var outputs = Set(0x00);

            AssertBits(outputs, 0x00);
            Assert.All(outputs.Select(o => o.Value), v => Assert.Equal(false, v));
        }
    }
}
