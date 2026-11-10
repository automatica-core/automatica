using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using P3.Driver.ModBus.SolarmanV5;
using P3.Driver.ModBus.SolarmanV5.DriverFactory;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    /// <summary>
    /// Read-modify-write of the Deye time of use charge registers (172-177) through a Solarman V5 logger
    /// </summary>
    public class SolarmanGridChargeWriteTests : IAsyncLifetime
    {
        private const ushort Start = SG04LP3DeviceMap.TimeOfUseChargeStart;
        private const int Count = SG04LP3DeviceMap.TimeOfUseSlots;

        private readonly SG04LP3DeviceMap _map = new();
        private FakeSolarmanLogger _logger;
        private SolarmanConnection _connection;

        public Task InitializeAsync()
        {
            _logger = new FakeSolarmanLogger();
            // SOC targets of the slots, must never change
            for (var i = 0; i < Count; i++)
            {
                _logger.Registers[SG04LP3DeviceMap.TimeOfUseSocStart + i] = (ushort)(10 * (i + 1));
            }
            _connection = _logger.CreateConnection();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _connection.Stop();
            await _logger.DisposeAsync();
        }

        private void SetChargeRegisters(params ushort[] values)
        {
            Array.Copy(values, 0, _logger.Registers, Start, values.Length);
        }

        private ushort[] ChargeRegisters => _logger.Registers.Skip(Start).Take(Count).ToArray();

        private Task<ushort[]> SetGridCharge(int? slotIndex, bool enabled)
        {
            return SolarmanRegisterWriter.ReadModifyWrite(_connection, 1, Start, Count,
                (index, raw) => slotIndex == null || slotIndex == index ? SolarmanRegisterCodec.WithGridCharge(raw, enabled) : raw,
                SolarmanRegisterCodec.GridChargeBit, _map.WritableRegisters);
        }

        private void AssertSocUntouched()
        {
            for (var i = 0; i < Count; i++)
            {
                Assert.Equal((ushort)(10 * (i + 1)), _logger.Registers[SG04LP3DeviceMap.TimeOfUseSocStart + i]);
            }
        }

        [Fact]
        public async Task DisableAllSlots_KeepsTheGeneratorAndOtherBits()
        {
            SetChargeRegisters(0x0001, 0x0003, 0x0002, 0x0000, 0x0101, 0x0001);

            var result = await SetGridCharge(null, false);

            Assert.Equal(new ushort[] { 0x0000, 0x0002, 0x0002, 0x0000, 0x0100, 0x0000 }, ChargeRegisters);
            Assert.Equal(ChargeRegisters, result);
            Assert.False(SolarmanRegisterCodec.IsGridChargeEnabledInAnySlot(result));

            // one FC16 request over the six charge registers, no FC06
            var write = Assert.Single(_logger.Writes);
            Assert.Equal(0x10, write.function);
            Assert.Equal(Start, write.start);
            Assert.Equal(6, write.values.Length);
            AssertSocUntouched();
            Assert.Empty(_logger.Errors);
        }

        [Fact]
        public async Task EnableOneSlot_WritesOnlyThatRegister()
        {
            SetChargeRegisters(0x0000, 0x0002, 0x0000, 0x0002, 0x0000, 0x0000);

            var result = await SetGridCharge(3, true);

            Assert.Equal(new ushort[] { 0x0000, 0x0002, 0x0000, 0x0003, 0x0000, 0x0000 }, ChargeRegisters);
            Assert.True(SolarmanRegisterCodec.IsGridChargeEnabled(result[3]));

            var write = Assert.Single(_logger.Writes);
            Assert.Equal(0x10, write.function);
            Assert.Equal(175, write.start);
            Assert.Equal(new ushort[] { 0x0003 }, write.values);
            AssertSocUntouched();
        }

        [Fact]
        public async Task DisableOneSlot_LeavesTheOtherSlots()
        {
            SetChargeRegisters(0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001);

            await SetGridCharge(0, false);

            Assert.Equal(new ushort[] { 0x0000, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001 }, ChargeRegisters);
            var write = Assert.Single(_logger.Writes);
            Assert.Equal(172, write.start);
            Assert.Equal(new ushort[] { 0x0000 }, write.values);
        }

        [Fact]
        public async Task NothingToChange_WritesNothing()
        {
            SetChargeRegisters(0x0000, 0x0002, 0x0000, 0x0000, 0x0000, 0x0000);

            var result = await SetGridCharge(null, false);

            Assert.Equal(new ushort[] { 0x0000, 0x0002, 0x0000, 0x0000, 0x0000, 0x0000 }, result);
            Assert.Empty(_logger.Writes);
        }

        [Fact]
        public async Task ChangesOutsideTheGridChargeBit_AreRefused()
        {
            SetChargeRegisters(0x0002, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000);

            // an update that would clear the generator bit
            await Assert.ThrowsAsync<InvalidOperationException>(() => SolarmanRegisterWriter.ReadModifyWrite(_connection, 1, Start, Count,
                (index, raw) => index == 0 ? (ushort)0x0001 : raw, SolarmanRegisterCodec.GridChargeBit, _map.WritableRegisters));

            Assert.Empty(_logger.Writes);
            Assert.Equal((ushort)0x0002, _logger.Registers[Start]);
        }

        [Fact]
        public async Task RegistersOutsideTheAllowList_AreRefused()
        {
            // the SOC registers 166-171 are read only
            await Assert.ThrowsAsync<InvalidOperationException>(() => SolarmanRegisterWriter.ReadModifyWrite(_connection, 1,
                SG04LP3DeviceMap.TimeOfUseSocStart, Count, (index, raw) => SolarmanRegisterCodec.WithGridCharge(raw, true),
                SolarmanRegisterCodec.GridChargeBit, _map.WritableRegisters));

            Assert.Empty(_logger.Writes);
            AssertSocUntouched();
        }

        [Fact]
        public async Task InverterIgnoresTheWrite_Throws()
        {
            SetChargeRegisters(0x0001, 0x0001, 0x0001, 0x0001, 0x0001, 0x0001);
            _logger.IgnoreWrites = true;

            await Assert.ThrowsAsync<IOException>(() => SetGridCharge(null, false));
        }

        [Fact]
        public async Task NoConnection_Throws()
        {
            await Assert.ThrowsAsync<IOException>(() => SolarmanRegisterWriter.ReadModifyWrite(null, 1, Start, Count,
                (index, raw) => raw, SolarmanRegisterCodec.GridChargeBit, _map.WritableRegisters));
        }
    }
}
