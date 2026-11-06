using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.Accessories
{
    /// <summary>
    /// HomeKit accessories that can be created manually (folder node with one child node per characteristic).
    /// The guids are persisted in the database and must never change.
    /// </summary>
    internal static class HomeKitAccessoryCatalog
    {
        public static readonly Guid ValveTypePropertyGuid = new Guid("261f7f7c-9df5-4669-b3e6-ae41e9dc6b13");
        public static readonly Guid SwitchControlTypePropertyGuid = new Guid("f0e9fa04-973b-498b-91af-6b39a9fb2339");

        public static readonly IReadOnlyList<HomeKitAccessoryType> AccessoryTypes = new[]
        {
            new HomeKitAccessoryType("humidity-sensor-folder", "APPLE_HOMEKIT_SERVER.HUMIDITY_SENSOR", new Guid("1aeb767a-43b9-4231-b000-793c606a63ba"), AccessoryFactory.HumiditySensorType,
                new[]
                {
                    new HomeKitDataPoint("humidity-sensor-humidity", new Guid("d3f95852-26a3-447e-b393-9469c8eebd52"), "APPLE_HOMEKIT_SERVER.HUMIDITY_SENSOR.HUMIDITY", () => HapCharacteristics.CurrentRelativeHumidity, NodeDataType.Double, true),
                    new HomeKitDataPoint("humidity-sensor-low-battery", new Guid("4778cd16-d0ff-4736-a207-f368e1e74c35"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("humidity-sensor-battery-level", new Guid("db8e527d-314a-42e1-8b3e-6c9333ae7195"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("humidity-sensor-fault", new Guid("d6c0f113-9681-48b2-a153-134af15d32c4"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("light-sensor-folder", "APPLE_HOMEKIT_SERVER.LIGHT_SENSOR", new Guid("8780f737-a856-4b76-9476-bd44bfcc4113"), AccessoryFactory.LightSensorType,
                new[]
                {
                    new HomeKitDataPoint("light-sensor-light-level", new Guid("68340d22-5c78-48aa-93f0-e98a9cc6ca38"), "APPLE_HOMEKIT_SERVER.LIGHT_SENSOR.LIGHT_LEVEL", () => HapCharacteristics.CurrentAmbientLightLevel, NodeDataType.Double, true),
                    new HomeKitDataPoint("light-sensor-low-battery", new Guid("db77f9c9-2b61-4eb2-922b-3e4201951a9b"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("light-sensor-battery-level", new Guid("293f5a9f-3fb5-4f3b-bffd-b8f96ec5e8cd"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("light-sensor-fault", new Guid("5146c47b-13b6-4f76-8150-9a19b894ec7f"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("motion-sensor-folder", "APPLE_HOMEKIT_SERVER.MOTION_SENSOR", new Guid("69f45b17-95ec-4124-8a0c-fde3ce440778"), AccessoryFactory.MotionSensorType,
                new[]
                {
                    new HomeKitDataPoint("motion-sensor-motion", new Guid("189861a5-fa8b-4c64-88db-0016e26598af"), "APPLE_HOMEKIT_SERVER.MOTION_SENSOR.MOTION", () => HapCharacteristics.MotionDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("motion-sensor-low-battery", new Guid("2271f29a-e0fd-4036-9d2c-d7f4cda0df35"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("motion-sensor-battery-level", new Guid("a9a0b957-42b9-438b-822e-7cdd9b8edb7a"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("motion-sensor-fault", new Guid("dba30434-3387-46f5-851a-76954e0434e3"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("occupancy-sensor-folder", "APPLE_HOMEKIT_SERVER.OCCUPANCY_SENSOR", new Guid("bebd5b1e-5f1d-478d-8990-e36ec4c47d15"), AccessoryFactory.OccupancySensorType,
                new[]
                {
                    new HomeKitDataPoint("occupancy-sensor-occupancy", new Guid("07f0171e-1bb8-408f-8c65-a30f8a5bea0c"), "APPLE_HOMEKIT_SERVER.OCCUPANCY_SENSOR.OCCUPANCY", () => HapCharacteristics.OccupancyDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("occupancy-sensor-low-battery", new Guid("2be8cd4f-9184-496d-88b3-a17e7fd4889f"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("occupancy-sensor-battery-level", new Guid("9e88b44c-5643-4e4c-aeff-200e6c81982c"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("occupancy-sensor-fault", new Guid("2c30fced-5c6d-4413-a146-c3935ebd64ea"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("leak-sensor-folder", "APPLE_HOMEKIT_SERVER.LEAK_SENSOR", new Guid("1a9478d3-bfa0-49ee-8e94-bcddb7481903"), AccessoryFactory.LeakSensorType,
                new[]
                {
                    new HomeKitDataPoint("leak-sensor-leak", new Guid("937a3e52-a048-4e0f-97b9-b8dcaf521982"), "APPLE_HOMEKIT_SERVER.LEAK_SENSOR.LEAK", () => HapCharacteristics.LeakDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("leak-sensor-low-battery", new Guid("0d6a5354-4b68-4391-bc5b-a183412c98ee"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("leak-sensor-battery-level", new Guid("89d4f22f-5a7c-49a7-bd93-78314b05b1c8"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("leak-sensor-fault", new Guid("8db45066-d341-4ad5-90b8-bd985a1b1048"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("smoke-sensor-folder", "APPLE_HOMEKIT_SERVER.SMOKE_SENSOR", new Guid("8d432e4c-03f3-474f-a679-2daf404d6da5"), AccessoryFactory.SmokeSensorType,
                new[]
                {
                    new HomeKitDataPoint("smoke-sensor-smoke", new Guid("315871f3-05a8-44ae-ac04-a96af88244d1"), "APPLE_HOMEKIT_SERVER.SMOKE_SENSOR.SMOKE", () => HapCharacteristics.SmokeDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("smoke-sensor-low-battery", new Guid("9172ffc7-1f95-4fc9-a4fb-0b2041d859db"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("smoke-sensor-battery-level", new Guid("6cc454ac-bd7a-4a2c-a4d7-3abb16780e86"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("smoke-sensor-fault", new Guid("659b21fc-26ea-4683-8046-c241b03a8b6f"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("carbon-monoxide-sensor-folder", "APPLE_HOMEKIT_SERVER.CO_SENSOR", new Guid("d3f9398c-0292-4bee-b7f4-88d684b4e9ee"), AccessoryFactory.CarbonMonoxideSensorType,
                new[]
                {
                    new HomeKitDataPoint("carbon-monoxide-sensor-detected", new Guid("f75acb4e-21f2-4daa-9db2-51bdeb99f192"), "APPLE_HOMEKIT_SERVER.CO_SENSOR.DETECTED", () => HapCharacteristics.CarbonMonoxideDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("carbon-monoxide-sensor-level", new Guid("bc6ddd07-8941-4bc0-ac19-014ad5bf6628"), "APPLE_HOMEKIT_SERVER.CO_SENSOR.LEVEL", () => HapCharacteristics.CarbonMonoxideLevel, NodeDataType.Double, false),
                    new HomeKitDataPoint("carbon-monoxide-sensor-low-battery", new Guid("9816c6f5-9ff7-4416-a6cf-b2f5643e750f"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("carbon-monoxide-sensor-battery-level", new Guid("5382066e-4fe7-4604-b212-b5e9ed961d75"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("carbon-monoxide-sensor-fault", new Guid("fe4fd15a-5b5e-472c-84c1-4ce36a5e5293"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("carbon-dioxide-sensor-folder", "APPLE_HOMEKIT_SERVER.CO2_SENSOR", new Guid("7a7b6d51-ce4c-47cc-8b38-192a68ff7170"), AccessoryFactory.CarbonDioxideSensorType,
                new[]
                {
                    new HomeKitDataPoint("carbon-dioxide-sensor-detected", new Guid("e7411db7-5745-40f6-9099-35a2ac034614"), "APPLE_HOMEKIT_SERVER.CO2_SENSOR.DETECTED", () => HapCharacteristics.CarbonDioxideDetected, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("carbon-dioxide-sensor-level", new Guid("138756f0-fb5c-4e36-a137-1d9dc2462268"), "APPLE_HOMEKIT_SERVER.CO2_SENSOR.LEVEL", () => HapCharacteristics.CarbonDioxideLevel, NodeDataType.Double, false),
                    new HomeKitDataPoint("carbon-dioxide-sensor-low-battery", new Guid("1fa2efa9-a8f4-4f2a-8182-814a57e3d69d"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("carbon-dioxide-sensor-battery-level", new Guid("8152ebfa-02cb-4808-8e36-84a21f5045ed"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("carbon-dioxide-sensor-fault", new Guid("28220ba2-74ca-4edc-afaf-2771e17fdf31"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("air-quality-sensor-folder", "APPLE_HOMEKIT_SERVER.AIR_QUALITY_SENSOR", new Guid("16cbb1eb-6711-4d8e-b074-bf8206bfdaf0"), AccessoryFactory.AirQualitySensorType,
                new[]
                {
                    new HomeKitDataPoint("air-quality-sensor-air-quality", new Guid("8af3378d-3467-4102-906e-221af3ae0b42"), "APPLE_HOMEKIT_SERVER.AIR_QUALITY_SENSOR.AIR_QUALITY", () => HapCharacteristics.AirQuality, NodeDataType.Integer, true),
                    new HomeKitDataPoint("air-quality-sensor-pm25", new Guid("385670a2-f198-4593-a7ca-3367a2957fa0"), "APPLE_HOMEKIT_SERVER.AIR_QUALITY_SENSOR.PM25", () => HapCharacteristics.Pm25Density, NodeDataType.Double, false),
                    new HomeKitDataPoint("air-quality-sensor-pm10", new Guid("9e8a8deb-211b-449b-8f11-39e0f8e8b001"), "APPLE_HOMEKIT_SERVER.AIR_QUALITY_SENSOR.PM10", () => HapCharacteristics.Pm10Density, NodeDataType.Double, false),
                    new HomeKitDataPoint("air-quality-sensor-voc", new Guid("e29859ce-f849-4fdc-b009-3411293b0496"), "APPLE_HOMEKIT_SERVER.AIR_QUALITY_SENSOR.VOC", () => HapCharacteristics.VocDensity, NodeDataType.Double, false),
                    new HomeKitDataPoint("air-quality-sensor-low-battery", new Guid("27241719-98e6-444a-a0dc-b2fa2717d073"), "APPLE_HOMEKIT_SERVER.COMMON.LOW_BATTERY", () => HapCharacteristics.StatusLowBattery, NodeDataType.Boolean, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("air-quality-sensor-battery-level", new Guid("3213917c-3195-480a-93aa-d75113c7d140"), "APPLE_HOMEKIT_SERVER.COMMON.BATTERY_LEVEL", () => HapCharacteristics.BatteryLevel, NodeDataType.Integer, false, HomeKitServiceTarget.Battery),
                    new HomeKitDataPoint("air-quality-sensor-fault", new Guid("58576cc6-8cbf-443a-b4f5-f38ec1ef302b"), "APPLE_HOMEKIT_SERVER.COMMON.FAULT", () => HapCharacteristics.StatusFault, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("fan-folder", "APPLE_HOMEKIT_SERVER.FAN", new Guid("9776e6b3-c5c9-42c7-b20b-cb5374e80b5e"), AccessoryFactory.FanType,
                new[]
                {
                    new HomeKitDataPoint("fan-on", new Guid("05f44c95-918a-4f92-9189-b6b916aee774"), "APPLE_HOMEKIT_SERVER.FAN.ON", () => HapCharacteristics.On, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("fan-speed", new Guid("d3113023-0fe5-4176-aaa6-5c953afcd449"), "APPLE_HOMEKIT_SERVER.FAN.SPEED", () => HapCharacteristics.RotationSpeed, NodeDataType.Double, false)
                }),
            new HomeKitAccessoryType("thermostat-folder", "APPLE_HOMEKIT_SERVER.THERMOSTAT", new Guid("70257d72-0b2a-4888-a25c-de7828adead7"), AccessoryFactory.ThermostatType,
                new[]
                {
                    new HomeKitDataPoint("thermostat-current-state", new Guid("bb0a8415-8b5e-4fd0-ac74-f1d5dd14f884"), "APPLE_HOMEKIT_SERVER.THERMOSTAT.CURRENT_STATE", () => HapCharacteristics.CurrentHeatingCoolingState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("thermostat-target-state", new Guid("c96340c4-c14a-4691-b152-8ed770a8f1a0"), "APPLE_HOMEKIT_SERVER.THERMOSTAT.TARGET_STATE", () => HapCharacteristics.TargetHeatingCoolingState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("thermostat-current-temperature", new Guid("13b50b53-7f8d-480b-8ded-767ae9e7a75b"), "APPLE_HOMEKIT_SERVER.THERMOSTAT.CURRENT_TEMPERATURE", () => HapCharacteristics.CurrentTemperature, NodeDataType.Double, true),
                    new HomeKitDataPoint("thermostat-target-temperature", new Guid("953f27cb-cb83-4677-83e8-211d252ecfa0"), "APPLE_HOMEKIT_SERVER.THERMOSTAT.TARGET_TEMPERATURE", () => HapCharacteristics.TargetTemperature, NodeDataType.Double, true),
                    new HomeKitDataPoint("thermostat-humidity", new Guid("b3133830-3b0e-4616-a5a6-3979fd982777"), "APPLE_HOMEKIT_SERVER.THERMOSTAT.HUMIDITY", () => HapCharacteristics.CurrentRelativeHumidity, NodeDataType.Double, false)
                },
                _ => new[] { HapCharacteristics.TemperatureDisplayUnits }),
            new HomeKitAccessoryType("garage-door-folder", "APPLE_HOMEKIT_SERVER.GARAGE_DOOR", new Guid("e9d4a367-4732-439b-aa19-f8f018918733"), AccessoryFactory.GarageDoorOpenerType,
                new[]
                {
                    new HomeKitDataPoint("garage-door-current-state", new Guid("91ce2a38-e067-443f-b6e7-af5b4f2d90f7"), "APPLE_HOMEKIT_SERVER.GARAGE_DOOR.CURRENT_STATE", () => HapCharacteristics.CurrentDoorState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("garage-door-target-state", new Guid("85dd4b6b-2df2-44c4-b310-cf18412ae815"), "APPLE_HOMEKIT_SERVER.GARAGE_DOOR.TARGET_STATE", () => HapCharacteristics.TargetDoorState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("garage-door-obstruction", new Guid("83361629-8226-478b-8e18-79108d9710ef"), "APPLE_HOMEKIT_SERVER.GARAGE_DOOR.OBSTRUCTION", () => HapCharacteristics.ObstructionDetected, NodeDataType.Boolean, true)
                }),
            new HomeKitAccessoryType("lock-folder", "APPLE_HOMEKIT_SERVER.LOCK", new Guid("cc0f6adc-7204-4ce5-9153-08203c929000"), AccessoryFactory.LockMechanismType,
                new[]
                {
                    new HomeKitDataPoint("lock-current-state", new Guid("93aa5a55-2008-41bc-b278-f898e37ee98a"), "APPLE_HOMEKIT_SERVER.LOCK.CURRENT_STATE", () => HapCharacteristics.LockCurrentState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("lock-target-state", new Guid("2281bb25-bf87-44a1-a102-993206b3f78b"), "APPLE_HOMEKIT_SERVER.LOCK.TARGET_STATE", () => HapCharacteristics.LockTargetState, NodeDataType.Boolean, true)
                }),
            new HomeKitAccessoryType("door-folder", "APPLE_HOMEKIT_SERVER.DOOR", new Guid("de7e067a-e109-43d7-9a4d-e03d0676778b"), AccessoryFactory.DoorType,
                new[]
                {
                    new HomeKitDataPoint("door-current-position", new Guid("3f7395c1-4108-4cb7-bb7c-84da87c207aa"), "APPLE_HOMEKIT_SERVER.DOOR.CURRENT_POSITION", () => HapCharacteristics.CurrentPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("door-target-position", new Guid("7926e801-a2c7-420a-8f15-15f4fd3ac298"), "APPLE_HOMEKIT_SERVER.DOOR.TARGET_POSITION", () => HapCharacteristics.TargetPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("door-position-state", new Guid("5310d2d6-21ad-42c9-baa5-86423a2befcb"), "APPLE_HOMEKIT_SERVER.DOOR.POSITION_STATE", () => HapCharacteristics.PositionState, NodeDataType.Integer, true)
                }),
            new HomeKitAccessoryType("window-folder", "APPLE_HOMEKIT_SERVER.WINDOW", new Guid("e3858cb4-e686-45a8-aa9c-01f4c0e8e4b7"), AccessoryFactory.WindowType,
                new[]
                {
                    new HomeKitDataPoint("window-current-position", new Guid("c87441cd-beb8-4017-bfce-383605926f28"), "APPLE_HOMEKIT_SERVER.WINDOW.CURRENT_POSITION", () => HapCharacteristics.CurrentPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("window-target-position", new Guid("b6f04f1b-9d93-486b-bf94-28d18f25da6c"), "APPLE_HOMEKIT_SERVER.WINDOW.TARGET_POSITION", () => HapCharacteristics.TargetPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("window-position-state", new Guid("aca4fc68-af0c-4eb5-b3da-8b3cc80ed1d6"), "APPLE_HOMEKIT_SERVER.WINDOW.POSITION_STATE", () => HapCharacteristics.PositionState, NodeDataType.Integer, true)
                }),
            new HomeKitAccessoryType("window-covering-folder", "APPLE_HOMEKIT_SERVER.WINDOW_COVERING", new Guid("d0485cb2-33ea-4cd2-a6e7-efc796125c3d"), AccessoryFactory.WindowCoveringType,
                new[]
                {
                    new HomeKitDataPoint("window-covering-current-position", new Guid("84b29ce2-da5b-4252-bdfa-333dabef7967"), "APPLE_HOMEKIT_SERVER.WINDOW_COVERING.CURRENT_POSITION", () => HapCharacteristics.CurrentPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("window-covering-target-position", new Guid("215711c4-0fad-4074-bb22-9cda1fc09c5d"), "APPLE_HOMEKIT_SERVER.WINDOW_COVERING.TARGET_POSITION", () => HapCharacteristics.TargetPosition, NodeDataType.Integer, true),
                    new HomeKitDataPoint("window-covering-position-state", new Guid("6517d2ea-5506-4abb-b6b4-92b0896fae03"), "APPLE_HOMEKIT_SERVER.WINDOW_COVERING.POSITION_STATE", () => HapCharacteristics.PositionState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("window-covering-hold-position", new Guid("57e2715e-1773-411d-949c-faf5531e3fa3"), "APPLE_HOMEKIT_SERVER.WINDOW_COVERING.HOLD_POSITION", () => HapCharacteristics.HoldPosition, NodeDataType.Boolean, false)
                }),
            new HomeKitAccessoryType("stateless-switch-folder", "APPLE_HOMEKIT_SERVER.STATELESS_SWITCH", new Guid("e6b3c405-6eab-4c3b-a9bf-28c18c78f386"), AccessoryFactory.StatelessProgrammableSwitchType,
                new[]
                {
                    new HomeKitDataPoint("stateless-switch-event", new Guid("f33af648-117a-4a7c-8820-760424709619"), "APPLE_HOMEKIT_SERVER.STATELESS_SWITCH.EVENT", () => HapCharacteristics.ProgrammableSwitchEvent, NodeDataType.Integer, true)
                }),
            new HomeKitAccessoryType("valve-folder", "APPLE_HOMEKIT_SERVER.VALVE", new Guid("6af364d8-4fb8-420c-9c20-4ca8ee53358c"), AccessoryFactory.ValveType,
                new[]
                {
                    new HomeKitDataPoint("valve-active", new Guid("65e2181c-51b3-4a32-bdb9-6f0358fd90e0"), "APPLE_HOMEKIT_SERVER.VALVE.ACTIVE", () => HapCharacteristics.Active, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("valve-in-use", new Guid("f29bbe4a-6655-42ea-8499-e09270ceac66"), "APPLE_HOMEKIT_SERVER.VALVE.IN_USE", () => HapCharacteristics.InUse, NodeDataType.Boolean, true),
                    new HomeKitDataPoint("valve-set-duration", new Guid("fa009722-30d6-4a56-b722-5512934fc0d4"), "APPLE_HOMEKIT_SERVER.VALVE.SET_DURATION", () => HapCharacteristics.SetDuration, NodeDataType.Integer, false),
                    new HomeKitDataPoint("valve-remaining-duration", new Guid("2a7c190d-2823-49de-a24f-4c027a4232a5"), "APPLE_HOMEKIT_SERVER.VALVE.REMAINING_DURATION", () => HapCharacteristics.RemainingDuration, NodeDataType.Integer, false)
                },
                folder => new[] { HapCharacteristics.ValveType(folder?.GetProperty(HomeKitFactory.ValveTypePropertyKey)?.ValueInt ?? 0) }),
            new HomeKitAccessoryType("security-system-folder", "APPLE_HOMEKIT_SERVER.SECURITY_SYSTEM", new Guid("861bd60f-b4cc-423a-87ac-b96ef37e3d3f"), AccessoryFactory.SecuritySystemType,
                new[]
                {
                    new HomeKitDataPoint("security-system-current-state", new Guid("079a3b9e-4180-41db-85b0-037b64463d48"), "APPLE_HOMEKIT_SERVER.SECURITY_SYSTEM.CURRENT_STATE", () => HapCharacteristics.SecuritySystemCurrentState, NodeDataType.Integer, true),
                    new HomeKitDataPoint("security-system-target-state", new Guid("ad90a566-99e4-4e47-96e3-9679bee8ec82"), "APPLE_HOMEKIT_SERVER.SECURITY_SYSTEM.TARGET_STATE", () => HapCharacteristics.SecuritySystemTargetState, NodeDataType.Integer, true)
                })
        };

        public static HomeKitAccessoryType GetByKey(string folderKey) => AccessoryTypes.SingleOrDefault(a => a.Key == folderKey);
    }

    public enum HomeKitValveType
    {
        [EnumName("APPLE_HOMEKIT_SERVER.VALVE.VALVE_TYPE.GENERIC")]
        Generic = 0,
        [EnumName("APPLE_HOMEKIT_SERVER.VALVE.VALVE_TYPE.IRRIGATION")]
        Irrigation = 1,
        [EnumName("APPLE_HOMEKIT_SERVER.VALVE.VALVE_TYPE.SHOWER_HEAD")]
        ShowerHead = 2,
        [EnumName("APPLE_HOMEKIT_SERVER.VALVE.VALVE_TYPE.WATER_FAUCET")]
        WaterFaucet = 3
    }

    /// <summary>
    /// HomeKit accessory type used for switch controls.
    /// </summary>
    public enum HomeKitSwitchControlType
    {
        [EnumName("APPLE_HOMEKIT_SERVER.SWITCH_CONTROL_TYPE.SWITCH")]
        Switch = 0,
        [EnumName("APPLE_HOMEKIT_SERVER.SWITCH_CONTROL_TYPE.OUTLET")]
        Outlet = 1,
        [EnumName("APPLE_HOMEKIT_SERVER.SWITCH_CONTROL_TYPE.LIGHT")]
        Light = 2,
        [EnumName("APPLE_HOMEKIT_SERVER.SWITCH_CONTROL_TYPE.FAN")]
        Fan = 3
    }
}
