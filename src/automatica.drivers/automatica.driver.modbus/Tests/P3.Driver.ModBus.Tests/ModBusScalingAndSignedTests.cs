using System;
using System.Threading.Tasks;
using Automatica.Core.Driver.Exceptions;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Attributes;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    /// <summary>
    /// Signed (±) templates and factor/offset: read (value = raw / factor + offset) and write are exact inverses
    /// </summary>
    public class ModBusScalingAndSignedTests : ModBusBaseTest
    {
        private static Func<NodeInstance, NodeInstance> Configure(double factor = 1, double offset = 0, object byteOrder = null)
        {
            return instance =>
            {
                instance.GetProperty("factor").Value = factor;
                instance.GetProperty("offset").Value = offset;
                if (byteOrder != null)
                {
                    instance.GetProperty("modbus-byte-order").Value = byteOrder;
                }
                return instance;
            };
        }

        private async Task<ModBusAttribute> Create(Guid template, double factor = 1, double offset = 0, object byteOrder = null)
        {
            var attribute = await InitAttribute(template, Configure(factor, offset, byteOrder));
            return attribute.Attribute;
        }

        [Fact]
        public async Task Signed2Byte_ReadsTwosComplement()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register2ByteUnsignedGuid); // "±2 Byte"
            Assert.IsType<ModBus2ByteSignedInteger>(attribute);

            Assert.Equal((short)-1, attribute.ConvertValueFromBus(new ushort[] { 0xFFFF }));
            Assert.Equal((short)short.MinValue, attribute.ConvertValueFromBus(new ushort[] { 0x8000 }));
            Assert.Equal((short)123, attribute.ConvertValueFromBus(new ushort[] { 123 }));
        }

        [Fact]
        public async Task Signed2Byte_WritesNegativeValues()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register2ByteUnsignedGuid);

            var data = attribute.ConvertValueToBus(-5, out var converted);

            Assert.Equal(new ushort[] { 0xFFFB }, data);
            Assert.Equal((short)-5, converted);
            Assert.Equal((short)-5, attribute.ConvertValueFromBus(data));

            Assert.Throws<InvalidInputValueException>(() => attribute.ConvertValueToBus(40000, out _));
        }

        [Fact]
        public async Task Unsigned2Byte_RejectsOutOfRange()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid); // "+2 Byte"
            Assert.Equal((ushort)0xFFFF, attribute.ConvertValueFromBus(new ushort[] { 0xFFFF }));

            Assert.Throws<InvalidInputValueException>(() => attribute.ConvertValueToBus(-1, out _));
            Assert.Throws<InvalidInputValueException>(() => attribute.ConvertValueToBus(65536, out _));
        }

        [Fact]
        public async Task Signed4Byte_ReadWriteNegative()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register4ByteUnsignedGuid, byteOrder: ModBus4ByteOrder.AB_CD); // "±4 Byte"
            Assert.IsType<ModBus4ByteSignedInteger>(attribute);

            Assert.Equal(-1, attribute.ConvertValueFromBus(new ushort[] { 0xFFFF, 0xFFFF }));

            var data = attribute.ConvertValueToBus(-5, out _);
            Assert.Equal(new ushort[] { 0xFFFF, 0xFFFB }, data);
            Assert.Equal(-5, attribute.ConvertValueFromBus(data));

            data = attribute.ConvertValueToBus(-70000, out _);
            Assert.Equal(-70000, attribute.ConvertValueFromBus(data));
        }

        [Fact]
        public async Task Signed8Byte_ReadWriteNegative()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register8ByteUnsignedGuid, byteOrder: ModBus8ByteOrder.AB_CD_EF_GH); // "±8 Byte"
            Assert.IsType<ModBus8ByteSignedInteger>(attribute);

            var data = attribute.ConvertValueToBus(-5L, out _);
            Assert.Equal(new ushort[] { 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFB }, data);
            Assert.Equal(-5L, attribute.ConvertValueFromBus(data));

            data = attribute.ConvertValueToBus(long.MinValue, out _);
            Assert.Equal(long.MinValue, attribute.ConvertValueFromBus(data));
        }

        [Fact]
        public async Task Unsigned8Byte_KeepsFullRange()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, byteOrder: ModBus8ByteOrder.AB_CD_EF_GH);

            var data = attribute.ConvertValueToBus(ulong.MaxValue, out _);
            Assert.Equal(ulong.MaxValue, attribute.ConvertValueFromBus(data));
        }

        public static TheoryData<Guid, object> IntegerTemplates => new TheoryData<Guid, object>
        {
            { ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid, null },
            { ModBusDriverFactory.ModBusDriverFactory.Register2ByteUnsignedGuid, null },
            { ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, ModBus4ByteOrder.AB_CD },
            { ModBusDriverFactory.ModBusDriverFactory.Register4ByteUnsignedGuid, ModBus4ByteOrder.CD_AB },
            { ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, ModBus8ByteOrder.AB_CD_EF_GH },
            { ModBusDriverFactory.ModBusDriverFactory.Register8ByteUnsignedGuid, ModBus8ByteOrder.GH_EF_CD_AB },
        };

        [Theory]
        [MemberData(nameof(IntegerTemplates))]
        public async Task Integer_RoundTrip_WithFactorAndOffset(Guid template, object byteOrder)
        {
            // value = raw / 10 + 5  <=>  raw = (value - 5) * 10
            var attribute = await Create(template, 10, 5, byteOrder);

            var data = attribute.ConvertValueToBus(17, out var raw);
            Assert.Equal(120m, Convert.ToDecimal(raw));
            Assert.Equal(17d, Convert.ToDouble(attribute.ConvertValueFromBus(data)), 10);

            // rounded after scaling: (17.04 - 5) * 10 = 120.4 -> 120
            attribute.ConvertValueToBus(17.04, out raw);
            Assert.Equal(120m, Convert.ToDecimal(raw));

            // (17.06 - 5) * 10 = 120.6 -> 121, reads back as 17.1
            data = attribute.ConvertValueToBus(17.06, out raw);
            Assert.Equal(121m, Convert.ToDecimal(raw));
            Assert.Equal(17.1d, Convert.ToDouble(attribute.ConvertValueFromBus(data)), 10);
        }

        [Fact]
        public async Task Signed2Byte_RoundTrip_WithFactorAndOffset_Negative()
        {
            var attribute = await Create(ModBusDriverFactory.ModBusDriverFactory.Register2ByteUnsignedGuid, 10, 5);

            // (-2.5 - 5) * 10 = -75
            var data = attribute.ConvertValueToBus(-2.5, out var raw);
            Assert.Equal((short)-75, raw);
            Assert.Equal(-2.5d, Convert.ToDouble(attribute.ConvertValueFromBus(data)), 10);

            // a raw register is read with the factor/offset as well
            Assert.Equal(17.3d, Convert.ToDouble(attribute.ConvertValueFromBus(new ushort[] { 123 })), 10);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Float_RoundTrip_WithFactorAndOffset(bool eightByte)
        {
            var attribute = eightByte
                ? await Create(ModBusDriverFactory.ModBusDriverFactory.Register8ByteFloat, 10, 5, ModBus8ByteOrder.AB_CD_EF_GH)
                : await Create(ModBusDriverFactory.ModBusDriverFactory.Register4ByteFloat, 10, 5, ModBus4ByteOrder.AB_CD);

            var data = attribute.ConvertValueToBus(17.25, out var raw);
            Assert.Equal(122.5d, Convert.ToDouble(raw), 4);
            Assert.Equal(17.25d, Convert.ToDouble(attribute.ConvertValueFromBus(data)), 4);
        }
    }
}
