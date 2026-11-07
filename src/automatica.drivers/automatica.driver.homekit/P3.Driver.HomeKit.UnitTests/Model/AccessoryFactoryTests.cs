using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKit.UnitTests.Integration;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Model
{
    public class AccessoryFactoryTests
    {
        public static IEnumerable<object[]> Accessories()
        {
            yield return new object[] { "light", AccessoryFactory.CreateLightBulbAccessory(2, "n", "m", "s", true, 50), AccessoryFactory.LightBulbType };
            yield return new object[] { "outlet", AccessoryFactory.CreateOutletAccessory(2, "n", "m", "s", true), AccessoryFactory.OutletType };
            yield return new object[] { "contact", AccessoryFactory.CreateContactSensorAccessory(2, "n", "m", "s", 1), AccessoryFactory.ContactSensorType };
            yield return new object[] { "switch", AccessoryFactory.CreateSwitchAccessory(2, "n", "m", "s", true), AccessoryFactory.SwitchType };
            yield return new object[] { "temperature", AccessoryFactory.CreateTemperatureSensorAccessory(2, "n", "m", "s", 21.5), AccessoryFactory.TemperatureSensorType };
            yield return new object[] { "blind", AccessoryFactory.CreateWindowCovering(2, "n", "m", "s", 30), AccessoryFactory.WindowCoveringType };
        }

        private static IEnumerable<int> InstanceIds(Accessory accessory)
        {
            return accessory.Services.Select(s => s.Id).Concat(accessory.Services.SelectMany(s => s.Characteristics).Select(c => c.Id));
        }

        [Theory]
        [MemberData(nameof(Accessories))]
        public void FactoryAccessories_HaveUniqueInstanceIds(string kind, Accessory accessory, string serviceType)
        {
            var ids = InstanceIds(accessory).ToList();

            Assert.True(ids.Count == ids.Distinct().Count(), kind);
            Assert.All(ids, id => Assert.True(id >= 1));
            Assert.Equal(serviceType, accessory.Services[1].Type);
            Assert.Equal(AccessoryFactory.PrimaryServiceId, accessory.Services[1].Id);
        }

        [Theory]
        [MemberData(nameof(Accessories))]
        public void FactoryAccessories_HaveTheAccessoryInformationServiceFirst(string kind, Accessory accessory, string serviceType)
        {
            var info = accessory.Services[0];

            Assert.Equal(AccessoryFactory.AccessoryInformationType, info.Type);
            Assert.Equal(1, info.Id);
            Assert.Same(accessory.AccessoryInfo, info);
            var types = info.Characteristics.Select(c => c.Type).ToList();
            Assert.Contains(CharacteristicBase.NameCharacteristicType, types);
            Assert.Contains(CharacteristicBase.ManufacturerCharacteristicType, types);
            Assert.Contains(CharacteristicBase.SerialCharacteristicType, types);
            Assert.Contains(CharacteristicBase.ModelCharacteristicType, types);
            var identify = info.Characteristics.Single(c => c.Type == CharacteristicBase.IdentifyCharacteristicType);
            Assert.Equal(new[] { "pw" }, identify.Permissions);
            Assert.Equal("bool", identify.Format);
            Assert.NotNull(serviceType + kind);
        }

        [Fact]
        public void LightBulb_OnIsReadWriteNotify_WithTheInitialValues()
        {
            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "m", "s", true, 50);
            var service = accessory.Services[1];

            var on = service.Characteristics.Single(c => c.Type == CharacteristicBase.OnType);
            var brightness = service.Characteristics.Single(c => c.Type == CharacteristicBase.BrightnessType);

            Assert.Equal(new[] { "pr", "pw", "ev" }, on.Permissions);
            Assert.Equal("bool", on.Format);
            Assert.Equal(true, on.Value);
            Assert.Equal(new[] { "pr", "pw", "ev" }, brightness.Permissions);
            Assert.Equal("int", brightness.Format);
            Assert.Equal(50, brightness.Value);
        }

        [Fact]
        public void LightBulb_CreateHueAndBrightness_UseTheNextInstanceIds()
        {
            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "m", "s", false, 0);

            var hue = accessory.CreateHue();
            var brightness = accessory.CreateBrightness();

            Assert.Equal(10, hue.Id);
            Assert.Equal(11, brightness.Id);
            Assert.Equal("float", hue.Format);
            Assert.Contains(hue, accessory.Services[1].Characteristics);
            Assert.Equal(InstanceIds(accessory).Count(), InstanceIds(accessory).Distinct().Count());
        }

        [Fact]
        public void Outlet_InUseIsReadOnly()
        {
            var accessory = AccessoryFactory.CreateOutletAccessory(2, "o", "m", "s", true);
            var inUse = accessory.Services[1].Characteristics.Single(c => c.Type == CharacteristicBase.OutletInUseType);

            Assert.DoesNotContain("pw", inUse.Permissions);
            Assert.Contains("ev", inUse.Permissions);
            Assert.Equal(true, inUse.Value);
        }

        [Theory]
        [InlineData("contact", CharacteristicBase.ContactSensorStateType, "uint8")]
        [InlineData("temperature", CharacteristicBase.CurrentTemperatureType, "float")]
        public void Sensors_AreReadOnly(string kind, string type, string format)
        {
            var accessory = kind == "contact"
                ? AccessoryFactory.CreateContactSensorAccessory(2, "c", "m", "s", 1)
                : AccessoryFactory.CreateTemperatureSensorAccessory(2, "t", "m", "s", 21);
            var characteristic = accessory.Services[1].Characteristics.Single(c => c.Type == type);

            Assert.Equal(new[] { "pr", "ev" }, characteristic.Permissions);
            Assert.Equal(format, characteristic.Format);
        }

        [Fact]
        public void WindowCovering_TargetIsWritable_CurrentAndStateAreReadOnly()
        {
            var blind = AccessoryFactory.CreateWindowCovering(2, "b", "m", "s", 30);

            Assert.Contains("pw", blind.TargetPosition.Permissions);
            Assert.DoesNotContain("pw", blind.CurrentPosition.Permissions);
            Assert.DoesNotContain("pw", blind.PositionType.Permissions);
            Assert.Equal(30, blind.TargetPosition.Value);
            Assert.Equal(30, blind.CurrentPosition.Value);
            Assert.Equal(CharacteristicBase.PositionStateType, blind.PositionType.Type);
            Assert.Same(blind.Services[1], blind.Specific);
        }

        [Fact]
        public void NextInstanceId_IsTheHighestServiceOrCharacteristicIdPlusOne()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(3, "x", "m", "s", AccessoryFactory.ThermostatType,
                HapCharacteristics.CurrentTemperature);

            var next = accessory.NextInstanceId();

            Assert.Equal(InstanceIds(accessory).Max() + 1, next);
            var service = accessory.AddService(AccessoryFactory.BatteryServiceType);
            Assert.Equal(next, service.Id);
        }

        [Fact]
        public void GetOrAddBatteryService_ReturnsTheSameServiceTwice()
        {
            var accessory = AccessoryFactory.CreateServiceAccessory(3, "x", "m", "s", AccessoryFactory.MotionSensorType, HapCharacteristics.MotionDetected);

            var first = accessory.GetOrAddBatteryService();

            Assert.Same(first, accessory.GetOrAddBatteryService());
            Assert.Single(accessory.Services, s => s.Type == AccessoryFactory.BatteryServiceType);
        }

        [Fact]
        public void AccessoryContainer_RejectsADuplicateAid()
        {
            var server = LoopbackHapHost.CreateServer();

            Assert.Throws<ArgumentException>(() => server.AddAccessory(AccessoryFactory.CreateSwitchAccessory(1, "s", "m", "s", false))); // aid 1 is the bridge
            server.AddAccessory(AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false));
            Assert.Throws<ArgumentException>(() => server.AddAccessory(AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false)));
        }

        [Fact]
        public void Bridge_ExposesTheAccessoryInformationAndProtocolVersion()
        {
            var server = LoopbackHapHost.CreateServer();

            var bridge = server.AccessoryContainer.AccessoryMap[1];

            Assert.Equal(new[] { AccessoryFactory.AccessoryInformationType, AccessoryFactory.HapInformationProtocol }, bridge.Services.Select(s => s.Type));
            Assert.Contains(bridge.Services[0].Characteristics, c => c.Type == CharacteristicBase.FirmwareRevision && (string)c.Value == "1");
            Assert.Contains(bridge.Services[1].Characteristics, c => c.Type == CharacteristicBase.Version && (string)c.Value == "1.1.0");
        }

        [Fact]
        public void SetCharacteristicValue_ForACharacteristicOfAnotherServer_ChangesNothing()
        {
            var server = LoopbackHapHost.CreateServer();
            var foreign = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            var on = foreign.Services[1].Characteristics[0];

            server.SetCharacteristicValue(on, true);

            Assert.Equal(false, on.Value);
        }

        [Fact]
        public void SetCharacteristicValue_WithoutSubscribers_SetsTheValue()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];

            server.SetCharacteristicValue(on, true);

            Assert.Equal(true, on.Value);
        }

        [Fact]
        public void Accessories_SerializeWithHapPropertyNames()
        {
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);

            var json = JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(new AccessoryData { Accessories = new List<Accessory> { accessory } }, HapMiddleware.JsonSettings));

            var a = json["accessories"].Single();
            Assert.Equal(2, (int)a["aid"]);
            var on = a["services"][1]["characteristics"][0];
            Assert.Equal(CharacteristicBase.OnType, (string)on["type"]);
            Assert.Equal("bool", (string)on["format"]);
            Assert.NotNull(on["iid"]);
            Assert.NotNull(on["perms"]);
            Assert.Null(on["aid"]); // characteristics in the accessory database carry no aid
        }
    }

    public class CharacteristicSubscriptionEdgeCaseTests
    {
        private static Dictionary<string, List<Characteristic>> Subscriptions(HomeKitServer server)
        {
            return LoopbackHapHost.GetField<Dictionary<string, List<Characteristic>>>(server, "_eventBasedNotifications");
        }

        private static void Put(HomeKitServer server, HapSession session, string json)
        {
            new CharacteristicsController(NullLogger.Instance).Put(System.Text.Encoding.UTF8.GetBytes(json), session, server);
        }

        [Fact]
        public void Subscribe_Twice_IsRegisteredOnce()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];
            var session = new HapSession { ClientUsername = "c" };

            Put(server, session, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":true}}]}}");
            Put(server, session, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":true}}]}}");

            Assert.Single(Subscriptions(server)["c"]);
        }

        [Fact]
        public void Unsubscribe_WithoutSubscription_DoesNotThrow()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];

            Put(server, new HapSession { ClientUsername = "c" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":false}}]}}");

            Assert.False(Subscriptions(server).ContainsKey("c"));
        }

        [Fact]
        public void Subscriptions_ArePerController()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];

            Put(server, new HapSession { ClientUsername = "a" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":true}}]}}");
            Put(server, new HapSession { ClientUsername = "b" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":true}}]}}");
            Put(server, new HapSession { ClientUsername = "a" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":false}}]}}");

            Assert.Empty(Subscriptions(server)["a"]);
            Assert.Contains(on, Subscriptions(server)["b"]);
        }

        [Fact]
        public void ValueAndEv_InOneWrite_SubscribesAndWrites()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];
            var changed = new List<object>();
            server.ValueChanged += (s, e) => changed.Add(e.Value);

            Put(server, new HapSession { ClientUsername = "a" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"value\":true,\"ev\":true}}]}}");

            Assert.Contains(on, Subscriptions(server)["a"]);
            Assert.Equal(new object[] { true }, changed);
        }

        [Fact]
        public void SetCharacteristicValue_WithASubscriberThatIsNotConnected_DoesNotThrow()
        {
            var server = LoopbackHapHost.CreateServer();
            var accessory = AccessoryFactory.CreateSwitchAccessory(2, "s", "m", "s", false);
            server.AddAccessory(accessory);
            var on = accessory.Services[1].Characteristics[0];
            Put(server, new HapSession { ClientUsername = "gone" }, $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{on.Id},\"ev\":true}}]}}");

            server.SetCharacteristicValue(on, true);

            Assert.Equal(true, on.Value);
        }
    }
}
