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

        internal CharacteristicBase(Type defaultType)
        {
            DefaultType = defaultType;
            Value = GetDefault(defaultType);
        }

        [JsonProperty("iid")]
        public int Id { get; set; }

        [JsonProperty("aid")]
        public virtual int? AccessoryId { get; set; }

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
        public int AccessoryNumber { get; set; }
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

        [JsonIgnore] public override int? AccessoryId => null;
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
