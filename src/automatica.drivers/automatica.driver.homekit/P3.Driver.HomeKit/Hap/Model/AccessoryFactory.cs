﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace P3.Driver.HomeKit.Hap.Model
{
    public static class AccessoryFactory
    {
        public const string AccessoryInformationType = "3E";
        public const string LightBulbType = "43";
        public const string OutletType = "47";
        public const string ContactSensorType = "80";
        public const string SwitchType = "49";
        public const string TemperatureSensorType = "8A";

        public static Accessory CreateLightBulbAccessory(string name, string manufacturer, string serial, bool value)
        {
            var a2 = new Accessory
            {
                Id = 0
            };

            a2.Services.Add(CreateAccessoryInfo(a2, 1, name, manufacturer, serial));
            a2.Services.Add(CreateLightBulb(a2, value));


            return a2;
        }
        public static Accessory CreateOutletAccessory(string name, string manufacturer, string serial, bool value)
        {
            var a2 = new Accessory
            {
                Id = 0
            };

            a2.Services.Add(CreateAccessoryInfo(a2, 1, name, manufacturer, serial));
            a2.Services.Add(CreateOutlet(a2, value));


            return a2;
        }
        public static Accessory CreateContactSensorAccessory(string name, string manufacturer, string serial, int value)
        {
            var a2 = new Accessory
            {
                Id = 0
            };

            a2.Services.Add(CreateAccessoryInfo(a2, 1, name, manufacturer, serial));
            a2.Services.Add(CreateContactSensor(a2, value));


            return a2;
        }
        public static Accessory CreateSwitchAccessory(string name, string manufacturer, string serial, bool value)
        {
            var a2 = new Accessory
            {
                Id = 0
            };

            a2.Services.Add(CreateAccessoryInfo(a2, 1, name, manufacturer, serial));
            a2.Services.Add(CreateSwitch(a2, value));


            return a2;
        }
        public static Accessory CreateTemperatureSensorAccessory(string name, string manufacturer, string serial, double value)
        {
            var a2 = new Accessory
            {
                Id = 0
            };

            a2.Services.Add(CreateAccessoryInfo(a2, 1, name, manufacturer, serial));
            a2.Services.Add(CreateTemperatureSensor(a2, value));


            return a2;
        }

        public static Service CreateAccessoryInfo(Accessory accessory, int id, string name, string manufacturer, string serial)
        {
            var service = new Service(accessory)
            {
                Type = AccessoryInformationType,
                Id = id
            };


            service.Characteristics.Add(CharacteristicFactory.CreateIdentify(service, 2));
            service.Characteristics.Add(CharacteristicFactory.CreateManufacturer(service, manufacturer, 3));
            service.Characteristics.Add(CharacteristicFactory.CreateModel(service, name, 4));
            service.Characteristics.Add(CharacteristicFactory.CreateName(service, name, 5));
            service.Characteristics.Add(CharacteristicFactory.CreateSerial(service, serial, 6));

            return service;
        }

        public static Service CreateLightBulb(Accessory accessory, bool value)
        {
            var service = new Service(accessory)
            {
                Type = LightBulbType,
                Id = 7
            };

            var bulbC = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            bulbC.Value = false;


            service.Characteristics.Add(bulbC);

            return service;
        }

        public static Characteristic CreateBrightness(this Accessory accessory)
        {
            return accessory.Create<double>(CharacteristicBase.BrightnessType, 0, "int");
        }
        public static Characteristic CreateHue(this Accessory accessory)
        {
            return accessory.Create<float>(CharacteristicBase.HueType, 0, "float");
        }

        public static Characteristic Create<T>(this Accessory accessory,  string type, object value, string format, bool canWrite=false)
        {
            var service = accessory.Services[1];
            int id = service.Characteristics[service.Characteristics.Count - 1].Id + 1;

            var characteristic = CharacteristicFactory.Create<T>(accessory.Services[1], type, value, id);
            SetCharacteristicOptions(characteristic, format, canWrite);
            accessory.Services[1].Characteristics.Add(characteristic);

            return characteristic;
        }

        public static Service CreateOutlet(Accessory accessory, bool value)
        {
            var service = new Service(accessory)
            {
                Type = OutletType,
                Id = 7
            };

            var on = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            on.Value = false;

            var inUse = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OutletInUseType, true, 9), "bool", false);
            inUse.Value = true;

            service.Characteristics.Add(on);
            service.Characteristics.Add(inUse);

            return service;
        }

        public static Service CreateContactSensor(Accessory accessory, int value)
        {
            var service = new Service(accessory)
            {
                Type = ContactSensorType,
                Id = 7
            };

            var state = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.ContactSensorStateType, value, 8), "uint8", false);
            state.Value = 0;

            service.Characteristics.Add(state);

            return service;
        }
        public static Service CreateSwitch(Accessory accessory, bool value)
        {
            var service = new Service(accessory)
            {
                Type = SwitchType,
                Id = 7
            };

            var state = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            state.Value = false;

            service.Characteristics.Add(state);

            return service;
        }


        public static Service CreateTemperatureSensor(Accessory accessory, double value)
        {
            var service = new Service(accessory)
            {
                Type = TemperatureSensorType,
                Id = 7
            };

            var state = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.CurrentTemperatureType, value, 8), "float", false);
            state.Value = 0;

            service.Characteristics.Add(state);

            return service;
        }


        private static Characteristic SetCharacteristicOptions(Characteristic c, string format, bool canWrite=true)
        {
            c.Permissions.Clear();
            c.Permissions.Add("pr");
            if (canWrite)
            {
                c.Permissions.Add("pw");
            }

            c.Permissions.Add("ev");
            c.Format = format;

            return c;
        }
    }
}
