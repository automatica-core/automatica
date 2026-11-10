using System.Threading.Tasks;
using P3.Driver.ModBus.SolarmanV5;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    /// <summary>
    /// Solarman V5 framing. The expected frames were computed independently (Modbus CRC16 + V5 byte sum) and match the
    /// layout pysolarmanv5 sends.
    /// </summary>
    public class SolarmanV5FrameTests
    {
        [Fact]
        public void BuildFrame_ReadHoldingRegisters()
        {
            var frame = SolarmanConnection.BuildFrame(FakeSolarmanLogger.Serial, new byte[] { 0x01, 0x03, 0x00, 0xA6, 0x00, 0x0C });

            Assert.Equal(new byte[]
            {
                0xA5, 0x17, 0x00, 0x10, 0x45, 0x00, 0x00, 0x4E, 0x1C, 0xAB, 0xA1, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x03, 0x00, 0xA6, 0x00, 0x0C, 0xA5, 0xEC, 0x6B, 0x15
            }, frame);
        }

        [Fact]
        public void BuildFrame_WriteMultipleRegisters()
        {
            // FC16, register 175 (program 4 charge), one register, value 0x0002
            var frame = SolarmanConnection.BuildFrame(FakeSolarmanLogger.Serial, new byte[] { 0x01, 0x10, 0x00, 0xAF, 0x00, 0x01, 0x02, 0x00, 0x02 });

            Assert.Equal(new byte[]
            {
                0xA5, 0x1A, 0x00, 0x10, 0x45, 0x00, 0x00, 0x4E, 0x1C, 0xAB, 0xA1, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0xAF, 0x00, 0x01, 0x02, 0x00, 0x02, 0x3F, 0xCE, 0xF9, 0x15
            }, frame);
        }

        [Fact]
        public void ExtractRtuFrame_WriteResponse()
        {
            // tail of a V5 response: ... payload times, RTU (slave, FC16, start, count, CRC), checksum, end
            var input = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0xAF, 0x00, 0x01, 0x31, 0xE8, 0x77, 0x15 };

            var rtu = SolarmanConnection.ExtractRtuFrame(ModBusFunction.WriteMultipleRegisters, 1, input);

            Assert.Equal(new byte[] { 0x01, 0x10, 0x00, 0xAF, 0x00, 0x01, 0x31, 0xE8 }, rtu);
        }

        [Fact]
        public async Task ReadAndWrite_ThroughAV5Logger()
        {
            await using var logger = new FakeSolarmanLogger();
            logger.Registers[0xAC] = 0x0003;

            var connection = logger.CreateConnection();
            try
            {
                var read = await connection.ReadRegisters(1, 0xAC, 2);
                var values = Assert.IsType<ModBusRegisterValueReturn>(read);
                Assert.Equal(ModBusRequestStatus.Success, values.ModBusRequestStatus);
                Assert.Equal(new ushort[] { 0x0003, 0x0000 }, values.Data);

                var write = await connection.WriteMultipleHoldingRegisters(1, 0xAD, new ushort[] { 0x0002 });
                Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
                Assert.Equal((ushort)0x0002, logger.Registers[0xAD]);
                Assert.Empty(logger.Errors);
            }
            finally
            {
                await connection.Stop();
            }
        }
    }
}
