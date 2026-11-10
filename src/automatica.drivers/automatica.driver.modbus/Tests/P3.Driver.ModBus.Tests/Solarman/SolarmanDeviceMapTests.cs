using System.Linq;
using P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3;
using Xunit;

namespace P3.Driver.ModBus.Tests.Solarman
{
    /// <summary>
    /// Register addresses of the Deye SG04LP3 map, checked against the ha-solarman deye_p3.yaml / deye_sg04lp3.yaml definitions
    /// </summary>
    public class SolarmanDeviceMapTests
    {
        private readonly SG04LP3DeviceMap _map = new();
        private readonly SG043LP3DeviceGroupMap _groups = new();

        [Theory]
        [InlineData(1, 172, 166)]
        [InlineData(2, 173, 167)]
        [InlineData(3, 174, 168)]
        [InlineData(4, 175, 169)]
        [InlineData(5, 176, 170)]
        [InlineData(6, 177, 171)]
        public void TimeOfUseSlots(int slot, int chargeRegister, int socRegister)
        {
            Assert.Equal(new[] { chargeRegister }, _map[SG04LP3DeviceMap.GridChargeSlotKey(slot)]);
            Assert.Equal(new[] { socRegister }, _map[SG04LP3DeviceMap.SocSlotKey(slot)]);
            Assert.Contains(SG04LP3DeviceMap.GridChargeSlotKey(slot), _map.GridChargeKeys);
            Assert.DoesNotContain(SG04LP3DeviceMap.SocSlotKey(slot), _map.GridChargeKeys);
        }

        [Fact]
        public void GridChargeAll_CoversTheSixChargeRegisters()
        {
            Assert.Equal(new[] { 172, 173, 174, 175, 176, 177 }, _map[SG04LP3DeviceMap.GridChargeAllKey]);
            Assert.Contains(SG04LP3DeviceMap.GridChargeAllKey, _map.GridChargeKeys);
            Assert.Equal((172, 6), _map.TimeOfUseChargeRegisters);
        }

        [Fact]
        public void OnlyTheChargeRegistersAreWritable()
        {
            Assert.Equal(new[] { 172, 173, 174, 175, 176, 177 }, _map.WritableRegisters.OrderBy(a => a));
        }

        [Theory]
        [InlineData("battery-power", 590)]
        [InlineData("battery-current", 591)]
        [InlineData("total-grid-power", 625)]
        [InlineData("total-power", 636)]
        [InlineData("internal-ct-l1-power", 604)]
        [InlineData("external-ct-l1-power", 616)]
        [InlineData("inverter-l1-power", 633)]
        [InlineData("current-l1", 630)]
        [InlineData("running", 500)]
        [InlineData("grid-status", 552)]
        [InlineData("smartload-enabled-status", 133)]
        public void RegisterAddresses(string name, int register)
        {
            Assert.Equal(new[] { register }, _map[SG04LP3DeviceMap.KeyPrefix + name]);
        }

        [Theory]
        [InlineData("battery-power")]
        [InlineData("battery-current")]
        [InlineData("total-grid-power")]
        [InlineData("total-power")]
        [InlineData("internal-ct-l1-power")]
        [InlineData("internal-ct-l2-power")]
        [InlineData("internal-ct-l3-power")]
        [InlineData("external-ct-l1-power")]
        [InlineData("external-ct-l2-power")]
        [InlineData("external-ct-l3-power")]
        [InlineData("inverter-l1-power")]
        [InlineData("inverter-l2-power")]
        [InlineData("inverter-l3-power")]
        [InlineData("current-l1")]
        [InlineData("current-l2")]
        [InlineData("current-l3")]
        public void SignedRegisters(string name)
        {
            Assert.True(_map.IsSigned(SG04LP3DeviceMap.KeyPrefix + name));
        }

        [Theory]
        [InlineData("battery-voltage")]
        [InlineData("battery-soc")]
        [InlineData("pv1-power")]
        [InlineData("grid-voltage-l1")]
        [InlineData("tou-soc-1")]
        [InlineData("tou-grid-charge-1")]
        public void UnsignedRegisters(string name)
        {
            Assert.False(_map.IsSigned(SG04LP3DeviceMap.KeyPrefix + name));
        }

        [Fact]
        public void SignedKeysExistInTheMap()
        {
            Assert.All(_map.SignedKeys, key => Assert.True(_map.ContainsKey(key), key));
        }

        [Theory]
        [InlineData("battery-power")]
        [InlineData("battery-current")]
        [InlineData("total-grid-power")]
        [InlineData("total-power")]
        [InlineData("tou-grid-charge-all")]
        [InlineData("tou-grid-charge-1")]
        [InlineData("tou-grid-charge-6")]
        [InlineData("tou-soc-1")]
        [InlineData("tou-soc-6")]
        [InlineData("running")]
        [InlineData("grid-status")]
        [InlineData("smartload-enabled-status")]
        public void RegistersArePolled(string name)
        {
            var registers = _map[SG04LP3DeviceMap.KeyPrefix + name];
            Assert.Contains(_groups.GroupRead, group => registers.First() >= group.start && registers.Last() <= group.end);
        }

        [Fact]
        public void EveryMappedRegisterIsPolled()
        {
            Assert.All(_map.NameRegisterMap, entry => Assert.Contains(_groups.GroupRead,
                group => entry.Value.First() >= group.start && entry.Value.Last() <= group.end));
        }

        [Theory]
        [InlineData("running", 2, true)]
        [InlineData("running", 1, false)]
        [InlineData("grid-status", 5, 1)]
        [InlineData("grid-status", 1, 0)]
        [InlineData("smartload-enabled-status", 1, true)]
        [InlineData("smartload-enabled-status", 0, false)]
        public void Decoders(string name, int raw, object expected)
        {
            var decoder = _map.Decoders[SG04LP3DeviceMap.KeyPrefix + name];
            Assert.Equal(expected, decoder(new[] { (ushort)raw }));
        }
    }
}
