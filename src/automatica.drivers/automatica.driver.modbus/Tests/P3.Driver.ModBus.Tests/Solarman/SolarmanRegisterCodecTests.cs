using System;
using P3.Driver.ModBus.SolarmanV5.DriverFactory;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    public class SolarmanRegisterCodecTests
    {
        [Theory]
        [InlineData(64220, -1316)] // battery power charging (example from the field)
        [InlineData(65370, -166)] // internal CT power
        [InlineData(65110, -426)] // inverter L2 power
        [InlineData(1316, 1316)]
        [InlineData(0, 0)]
        [InlineData(32767, 32767)]
        [InlineData(32768, -32768)]
        [InlineData(65535, -1)]
        public void Decode16_Signed_IsTwosComplement(int raw, int expected)
        {
            Assert.Equal(expected, SolarmanRegisterCodec.Decode16((ushort)raw, true, 0, 1.0));
        }

        [Theory]
        [InlineData(64220)]
        [InlineData(65535)]
        [InlineData(0)]
        public void Decode16_Unsigned_KeepsTheRawValue(int raw)
        {
            Assert.Equal(raw, SolarmanRegisterCodec.Decode16((ushort)raw, false, 0, 1.0));
        }

        [Fact]
        public void Decode16_SignedScaled_BatteryCurrent()
        {
            // 636.56 A before the fix
            Assert.Equal(-18.8, SolarmanRegisterCodec.Decode16(63656, true, 0, 0.01));
            Assert.Equal(636.56, SolarmanRegisterCodec.Decode16(63656, false, 0, 0.01));
        }

        [Theory]
        [InlineData(1250, 25.0)]
        [InlineData(950, -5.0)]
        [InlineData(1000, 0.0)]
        public void Decode16_Temperature_SubtractsOffsetBeforeScaling(int raw, double expected)
        {
            Assert.Equal(expected, SolarmanRegisterCodec.Decode16((ushort)raw, true, 1000, 0.1));
        }

        [Fact]
        public void Decode32_LowWordFirst()
        {
            Assert.Equal(65536, SolarmanRegisterCodec.Decode32(0x0000, 0x0001, false, 0, 1.0));
            Assert.Equal(46341.0, SolarmanRegisterCodec.Decode32(0x1232, 0x0007, false, 0, 0.1)); // 463410
        }

        [Fact]
        public void Decode32_SignedAndUnsigned()
        {
            Assert.Equal(-1, SolarmanRegisterCodec.Decode32(0xFFFF, 0xFFFF, true, 0, 1.0));
            Assert.Equal(4294967295L, SolarmanRegisterCodec.Decode32(0xFFFF, 0xFFFF, false, 0, 1.0));
        }

        [Theory]
        [InlineData(0x0000, true, 0x0001)]
        [InlineData(0x0001, false, 0x0000)]
        [InlineData(0x0002, true, 0x0003)] // generator charge stays on
        [InlineData(0x0003, false, 0x0002)] // generator charge stays on
        [InlineData(0x0003, true, 0x0003)]
        [InlineData(0x0000, false, 0x0000)]
        [InlineData(0xFF02, true, 0xFF03)] // unknown upper bits stay as they are
        [InlineData(0xFFFF, false, 0xFFFE)]
        public void WithGridCharge_ChangesOnlyBit0(int raw, bool enabled, int expected)
        {
            Assert.Equal((ushort)expected, SolarmanRegisterCodec.WithGridCharge((ushort)raw, enabled));
        }

        [Theory]
        [InlineData(0x0000, false)]
        [InlineData(0x0001, true)]
        [InlineData(0x0002, false)]
        [InlineData(0x0003, true)]
        public void IsGridChargeEnabled_ReadsBit0(int raw, bool expected)
        {
            Assert.Equal(expected, SolarmanRegisterCodec.IsGridChargeEnabled((ushort)raw));
        }

        [Fact]
        public void IsGridChargeEnabledInAnySlot()
        {
            Assert.False(SolarmanRegisterCodec.IsGridChargeEnabledInAnySlot(new ushort[] { 0, 2, 2, 0, 0, 0 }));
            Assert.True(SolarmanRegisterCodec.IsGridChargeEnabledInAnySlot(new ushort[] { 0, 2, 2, 0, 0, 1 }));
        }

        [Theory]
        [InlineData(0x0002, 0x0003, 0x0001, true)]
        [InlineData(0x0002, 0x0002, 0x0001, true)]
        [InlineData(0x0002, 0x0001, 0x0001, false)] // generator bit would be cleared
        [InlineData(0x0000, 0x0101, 0x0001, false)]
        public void ChangesOnlyBits(int current, int next, int mask, bool expected)
        {
            Assert.Equal(expected, SolarmanRegisterCodec.ChangesOnlyBits((ushort)current, (ushort)next, (ushort)mask));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(1.0, true)]
        [InlineData(0L, false)]
        [InlineData("true", true)]
        [InlineData("False", false)]
        [InlineData("on", true)]
        [InlineData("OFF", false)]
        [InlineData("1", true)]
        [InlineData("0", false)]
        public void ToBool_AcceptsTheUsualRepresentations(object value, bool expected)
        {
            Assert.Equal(expected, SolarmanRegisterCodec.ToBool(value));
        }

        [Fact]
        public void ToBool_RejectsGarbage()
        {
            Assert.Throws<FormatException>(() => SolarmanRegisterCodec.ToBool("maybe"));
            Assert.Throws<ArgumentNullException>(() => SolarmanRegisterCodec.ToBool(null));
        }
    }
}
