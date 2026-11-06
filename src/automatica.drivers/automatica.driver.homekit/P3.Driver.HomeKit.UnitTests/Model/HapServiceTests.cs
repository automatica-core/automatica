using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap.Model;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Model
{
    public class HapServiceTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private static JObject ToJson(object o) => JObject.Parse(JsonConvert.SerializeObject(o, Settings));

        private static void AssertUniqueInstanceIds(Accessory accessory)
        {
            var ids = accessory.Services.Select(a => a.Id).Concat(accessory.Services.SelectMany(a => a.Characteristics).Select(a => a.Id)).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void CreateServiceAccessory_CreatesPrimaryServiceWithCharacteristics()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(5, "Thermostat", "AutomaticaCore", "serial", AccessoryFactory.ThermostatType,
                HapCharacteristics.CurrentHeatingCoolingState, HapCharacteristics.TargetHeatingCoolingState, HapCharacteristics.CurrentTemperature,
                HapCharacteristics.TargetTemperature, HapCharacteristics.TemperatureDisplayUnits);

            Assert.Equal(5ul, accessory.Id);
            Assert.Equal(2, accessory.Services.Count);
            Assert.Same(accessory.Specific, accessory.Services[1]);
            Assert.Equal(AccessoryFactory.PrimaryServiceId, accessory.Specific.Id);
            Assert.Equal("4A", accessory.Specific.Type);
            Assert.Equal(new[] { "0F", "33", "11", "35", "36" }, accessory.Specific.Characteristics.Select(a => a.Type));
            Assert.Equal(new[] { 8, 9, 10, 11, 12 }, accessory.Specific.Characteristics.Select(a => a.Id));
            AssertUniqueInstanceIds(accessory);
        }

        [Fact]
        public void Characteristic_SerializesMetadata()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(5, "Thermostat", "AutomaticaCore", "serial", AccessoryFactory.ThermostatType,
                HapCharacteristics.TargetTemperature, HapCharacteristics.TargetHeatingCoolingState);

            var json = ToJson(accessory);
            var target = json["services"][1]["characteristics"][0];
            Assert.Equal("35", (string)target["type"]);
            Assert.Equal("float", (string)target["format"]);
            Assert.Equal(new[] { "pr", "pw", "ev" }, target["perms"].Select(a => (string)a));
            Assert.Equal(10d, (double)target["minValue"]);
            Assert.Equal(38d, (double)target["maxValue"]);
            Assert.Equal(0.5, (double)target["minStep"]);
            Assert.Equal("celsius", (string)target["unit"]);
            Assert.Null(target["valid-values"]);

            var mode = json["services"][1]["characteristics"][1];
            Assert.Equal("uint8", (string)mode["format"]);
            Assert.Equal(new[] { 0, 1, 2, 3 }, mode["valid-values"].Select(a => (int)a));
        }

        [Fact]
        public void ExistingAccessories_DoNotGetMetadata()
        {
            var json = ToJson(AccessoryFactory.CreateSwitchAccessory(2, "Switch", "AutomaticaCore", "serial", false));
            var on = (JObject)json["services"][1]["characteristics"][0];

            Assert.Equal(new[] { "iid", "value", "type", "perms", "format", "ev" }.OrderBy(a => a), on.Properties().Select(a => a.Name).OrderBy(a => a));
        }

        [Fact]
        public void EventAndWriteOnlyCharacteristics()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(5, "Button", "AutomaticaCore", "serial", AccessoryFactory.StatelessProgrammableSwitchType,
                HapCharacteristics.ProgrammableSwitchEvent);
            var hold = accessory.Specific.AddCharacteristic(HapCharacteristics.HoldPosition);
            var press = accessory.Specific.Characteristics[0];

            Assert.Equal(new[] { "pr", "ev" }, press.Permissions);
            Assert.Null(press.Value);
            Assert.Null(CharacteristicBase.GetDefault(press.DefaultType));

            Assert.Equal(new[] { "pw" }, hold.Permissions);
            Assert.False(hold.EventBasedNotification);
        }

        [Fact]
        public void GetOrAddCharacteristic_AddsOptionalCharacteristicOnce()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(5, "CO2", "AutomaticaCore", "serial", AccessoryFactory.CarbonDioxideSensorType,
                HapCharacteristics.CarbonDioxideDetected);

            var level = accessory.Specific.GetOrAddCharacteristic(HapCharacteristics.CarbonDioxideLevel);
            Assert.Same(level, accessory.Specific.GetOrAddCharacteristic(HapCharacteristics.CarbonDioxideLevel));
            Assert.Same(accessory.Specific.Characteristics[0], accessory.Specific.GetOrAddCharacteristic(HapCharacteristics.CarbonDioxideDetected));
            Assert.Equal(2, accessory.Specific.Characteristics.Count);
            AssertUniqueInstanceIds(accessory);
        }

        [Fact]
        public void GetOrAddBatteryService_AddsMandatoryCharacteristics()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(5, "Motion", "AutomaticaCore", "serial", AccessoryFactory.MotionSensorType,
                HapCharacteristics.MotionDetected);

            var battery = accessory.GetOrAddBatteryService();
            Assert.Same(battery, accessory.GetOrAddBatteryService());
            Assert.Equal(3, accessory.Services.Count);
            Assert.Equal("96", battery.Type);
            Assert.Equal(new[] { "68", "8F", "79" }, battery.Characteristics.Select(a => a.Type));
            Assert.Equal(100, battery.Characteristics[0].Value);
            Assert.Equal(2, battery.Characteristics[1].Value); // not chargeable

            accessory.Specific.GetOrAddCharacteristic(HapCharacteristics.StatusFault);
            AssertUniqueInstanceIds(accessory);
        }

        [Theory]
        [InlineData(21.3, 21.5)]
        [InlineData(21.2, 21.0)]
        [InlineData(50, 38)]
        [InlineData(-5, 10)]
        public void ConvertValue_FloatRangeAndStep(double value, double expected)
        {
            Assert.Equal(expected, HapCharacteristics.TargetTemperature.ConvertValue(value));
        }

        [Fact]
        public void ConvertValue_Formats()
        {
            Assert.Equal(-12.3, HapCharacteristics.CurrentTemperature.ConvertValue(-12.34));
            Assert.Equal(0.0001, HapCharacteristics.CurrentAmbientLightLevel.ConvertValue(0));
            Assert.Equal(1, HapCharacteristics.LeakDetected.ConvertValue(true));
            Assert.Equal(0, HapCharacteristics.LeakDetected.ConvertValue(false));
            Assert.Equal(true, HapCharacteristics.MotionDetected.ConvertValue(1));
            Assert.Equal(100, HapCharacteristics.BatteryLevel.ConvertValue(250));
            Assert.Equal(42, HapCharacteristics.CurrentPosition.ConvertValue("42"));
            Assert.Equal(3600L, HapCharacteristics.SetDuration.ConvertValue(5000));
            Assert.IsType<long>(HapCharacteristics.RemainingDuration.ConvertValue(10));
            Assert.Null(HapCharacteristics.On.ConvertValue(null));
        }

        [Fact]
        public void ConvertValue_RejectsInvalidEnumValue()
        {
            var noGap = new CharacteristicDefinition("XX", CharacteristicFormat.UInt8, false, 0, 0, 4, 1, validValues: new[] { 0, 2, 4 });
            Assert.Equal(2, noGap.ConvertValue(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => noGap.ConvertValue(3));
        }
    }
}
