using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Driver.HomeKit.Hap.Model
{
    public class CharacteristicBase
    {
        [JsonIgnore]
        public Type DefaultType { get; }
        public const string NameCharacteristicType = "23";
        public const string ModelCharacteristicType = "21";
        public const string ManufacturerCharacteristicType = "20";
        public const string IdentifyCharacteristicType = "14";
        public const string SerialCharacteristicType = "30";
        public const string OnType = "25";
        public const string OutletInUseType = "26";
        public const string BrightnessType = "8";
        public const string HueType = "13";
        public const string ContactSensorStateType = "6A";
        public const string CurrentTemperatureType = "11";
        public const string FirmwareRevision = "52";
        public const string Version = "37";

        public const string TargetPositionType = "7C";
        public const string CurrentPositionType = "6D";
        public const string CurrentVerticalTiltAngleType = "6E";
        public const string TargetVerticalTiltAngleType = "7D";
        
        public const string HoldPositionType = "6F";
        public const string PositionStateType = "72";
        
        public const string CurrentHorizontalTiltAngleType = "6C";
        public const string TargetHorizontalTiltAngleType = "7B";

        public const string CurrentRelativeHumidityType = "10";
        public const string CurrentAmbientLightLevelType = "6B";
        public const string MotionDetectedType = "22";
        public const string OccupancyDetectedType = "71";
        public const string LeakDetectedType = "70";
        public const string SmokeDetectedType = "76";
        public const string CarbonMonoxideDetectedType = "69";
        public const string CarbonMonoxideLevelType = "90";
        public const string CarbonDioxideDetectedType = "92";
        public const string CarbonDioxideLevelType = "93";
        public const string AirQualityType = "95";
        public const string Pm25DensityType = "C6";
        public const string Pm10DensityType = "C7";
        public const string VocDensityType = "C8";
        public const string RotationSpeedType = "29";
        public const string CurrentHeatingCoolingStateType = "0F";
        public const string TargetHeatingCoolingStateType = "33";
        public const string TargetTemperatureType = "35";
        public const string TemperatureDisplayUnitsType = "36";
        public const string CurrentDoorStateType = "0E";
        public const string TargetDoorStateType = "32";
        public const string ObstructionDetectedType = "24";
        public const string LockCurrentStateType = "1D";
        public const string LockTargetStateType = "1E";
        public const string ProgrammableSwitchEventType = "73";
        public const string ActiveType = "B0";
        public const string InUseType = "D2";
        public const string ValveTypeType = "D5";
        public const string SetDurationType = "D3";
        public const string RemainingDurationType = "D4";
        public const string SecuritySystemCurrentStateType = "66";
        public const string SecuritySystemTargetStateType = "67";
        public const string BatteryLevelType = "68";
        public const string ChargingStateType = "8F";
        public const string StatusLowBatteryType = "79";
        public const string StatusFaultType = "77";
        public const string StatusTamperedType = "7A";
        

        internal CharacteristicBase(Type defaultType)
        {
            DefaultType = defaultType;
            Value = GetDefault(defaultType);
        }

        [JsonProperty("iid")]
        public int Id { get; set; }

        [JsonProperty("aid")]
        public virtual ulong? AccessoryId { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }


        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }

    public class SentCharacteristic : Characteristic
    {
        [JsonProperty("aid")]
        public ulong AccessoryNumber { get; set; }
    }

    public class Characteristic : CharacteristicBase
    {
        [JsonIgnore]
        public Service Service { get; }

        [JsonIgnore]
        public Accessory Accessory => Service.Accessory;

        public Characteristic() : base(typeof(object))
        {
            //should only be used from json converter
        }

        internal Characteristic(Type defaultType) : base(defaultType)
        {
            
        }

        internal Characteristic(Service service, Type defaultType) : this(defaultType)
        {
            Service = service;
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("perms")]
        public List<string> Permissions { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; } = "string";

        [JsonProperty("ev")]
        public bool? EventBasedNotification { get; set; }

        [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
        public string Unit { get; set; }

        [JsonProperty("minValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? MinValue { get; set; }

        [JsonProperty("maxValue", NullValueHandling = NullValueHandling.Ignore)]
        public double? MaxValue { get; set; }

        [JsonProperty("minStep", NullValueHandling = NullValueHandling.Ignore)]
        public double? MinStep { get; set; }

        [JsonProperty("valid-values", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> ValidValues { get; set; }

        [JsonIgnore] public override ulong? AccessoryId => null;
    }

    public static class CharacteristicFactory
    {
        public static Characteristic Create<T>(Service service, string type, object value, int id)
        {
            return new Characteristic(service, typeof(T))
            {
                Type = type,
                Value = value,
                Permissions = new List<string>
                {
                    "pr"
                },
                Id = id,
                EventBasedNotification = true
            };
        }


        public static Characteristic CreateName(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.NameCharacteristicType, value, id);
        }

        public static Characteristic CreateModel(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.ModelCharacteristicType, value, id);
        }

        public static Characteristic CreateManufacturer(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.ManufacturerCharacteristicType, value, id);
        }
        public static Characteristic CreateSerial(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.SerialCharacteristicType, value, id);
        }

        public static Characteristic CreateVersion(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.Version, value, id);
        }
        public static Characteristic CreateFirmwareRevision(Service service, string value, int id)
        {
            return Create<string>(service, CharacteristicBase.FirmwareRevision, value, id);
        }
        public static Characteristic CreateIdentify(Service service, int id)
        {
            var c = Create<string>(service, CharacteristicBase.IdentifyCharacteristicType, null, id);

            c.Format = "bool";
            c.Permissions[0] = "pw";
            return c;
        }
    }
}
