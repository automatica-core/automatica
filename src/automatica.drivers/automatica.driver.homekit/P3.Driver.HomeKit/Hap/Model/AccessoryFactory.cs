namespace P3.Driver.HomeKit.Hap.Model
{
    public static class AccessoryFactory
    {
        public const string AccessoryInformationType = "3E";
        public const string HapInformationProtocol = "A2";
        public const string LightBulbType = "43";
        public const string OutletType = "47";
        public const string ContactSensorType = "80";
        public const string SwitchType = "49";
        public const string TemperatureSensorType = "8A";
        public const string WindowCoveringType = "8C";

        public static Accessory CreateLightBulbAccessory(ulong aid, string name, string manufacturer, string serial, bool? value, int? dimValue)
        {
            var a2 = new Accessory
            {
                Id = aid
            };
            
            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);

            a2.Specific = CreateLightBulb(a2, 7, name, value, dimValue);
            a2.Services.Add(a2.Specific);


            return a2;
        }

        public static WindowCovering CreateWindowCovering(ulong aid, string name, string manufacturer, string serial, int? currentPosition)
        {
            var a2 = new WindowCovering
            {
                Id = aid
            };

            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);

            a2.Specific = CreateWindowCovering(a2, 7, currentPosition);
            a2.Services.Add(a2.Specific);


            return a2;
        }

        public static Accessory CreateOutletAccessory(ulong aid, string name, string manufacturer, string serial, bool value)
        {
            var a2 = new Accessory
            {
                Id = aid
            };
            
            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);

            a2.Services.Add(CreateOutlet(a2, 7, value));


            return a2;
        }
        public static Accessory CreateContactSensorAccessory(ulong aid, string name, string manufacturer, string serial, int value)
        {
            var a2 = new Accessory
            {
                Id = aid
            };

            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);
            
            a2.Services.Add(CreateContactSensor(a2, 7, value));


            return a2;
        }
        public static Accessory CreateSwitchAccessory(ulong aid, string name, string manufacturer, string serial, bool value)
        {
            var a2 = new Accessory
            {
                Id = aid
            };


            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);
            
            a2.Specific = CreateSwitch(a2, 7, value);
            a2.Services.Add(a2.Specific);


            return a2;
        }


        public static Accessory CreateTemperatureSensorAccessory(ulong aid, string name, string manufacturer,
            string serial, double value)
        {
            var a2 = new Accessory
            {
                Id = aid
            };

            a2.AccessoryInfo = CreateAccessoryInfo(a2, 1, name, manufacturer, serial);
            a2.Services.Add(a2.AccessoryInfo);

            a2.Services.Add(CreateTemperatureSensor(a2, 7, value));

            return a2;
        }

        public static Service CreateAccessoryInfo(Accessory accessory, int id, string name, string manufacturer, string serial)
        {
            var service = new Service(accessory)
            {
                Type = AccessoryInformationType,
                Id = id
            };


            service.Characteristics.Add(CharacteristicFactory.CreateName(service, name, 2));
            service.Characteristics.Add(CharacteristicFactory.CreateManufacturer(service, manufacturer, 3));
            service.Characteristics.Add(CharacteristicFactory.CreateSerial(service, serial, 4));
            service.Characteristics.Add(CharacteristicFactory.CreateModel(service, serial, 5));
            service.Characteristics.Add(CharacteristicFactory.CreateIdentify(service, 6));

            return service;
        }

        public static Service CreateHapServiceInformation(Accessory accessory, int id)
        {
            var service = new Service(accessory)
            {
                Type = HapInformationProtocol,
                Id = id
            };

            service.Characteristics.Add(CharacteristicFactory.CreateVersion(service, "1.1.0", 9));

            return service;
        }

        public static Service CreateAccessoryInfo(Accessory accessory, int id, string name, string manufacturer, string serial, string firmwareRevision)
        {
            var service = CreateAccessoryInfo(accessory, id, name, manufacturer, serial);
            service.Characteristics.Add(CharacteristicFactory.CreateFirmwareRevision(service, firmwareRevision, 7));

            return service;
        }

        public static Service CreateLightBulb(Accessory accessory, int id, string name, bool? value, int? dimValue)
        {
            var service = new Service(accessory)
            {
                Type = LightBulbType,
                Id = id
            };

            var bulbC = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            bulbC.Value = value;

            var bulbB = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.BrightnessType, value, 9), "int");
            bulbB.Value = dimValue;

            service.Characteristics.Add(bulbC);
            service.Characteristics.Add(bulbB);

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

        public static Service CreateWindowCovering(WindowCovering accessory, int id, int? currentPosition)
        {
            var service = new Service(accessory)
            {
                Type = WindowCoveringType,
                Id = id
            };

            var targetPos = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.TargetPositionType, currentPosition, 8), "int");
            targetPos.Value = currentPosition;
            accessory.TargetPosition = targetPos;

            var currentPos = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.CurrentPositionType, currentPosition, 9), "int", false);
            currentPos.Value = currentPosition;
            accessory.CurrentPosition = currentPos;

            var positionType = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.PositionStateType, 2, 10), "int", false);
            accessory.PositionType = positionType;

            service.Characteristics.Add(targetPos);
            service.Characteristics.Add(currentPos);
            service.Characteristics.Add(positionType);

            return service;
        }

        public static Service CreateOutlet(Accessory accessory, int id, bool value)
        {
            var service = new Service(accessory)
            {
                Type = OutletType,
                Id = id
            };

            var on = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            on.Value = false;

            var inUse = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OutletInUseType, true, 9), "bool", false);
            inUse.Value = true;

            service.Characteristics.Add(on);
            service.Characteristics.Add(inUse);

            return service;
        }

        public static Service CreateContactSensor(Accessory accessory, int id, int value)
        {
            var service = new Service(accessory)
            {
                Type = ContactSensorType,
                Id = id
            };

            var state = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.ContactSensorStateType, value, 8), "uint8", false);
            state.Value = 0;

            service.Characteristics.Add(state);

            return service;
        }
        public static Service CreateSwitch(Accessory accessory, int id, bool value)
        {
            var service = new Service(accessory)
            {
                Type = SwitchType,
                Id = id
            };

            var state = SetCharacteristicOptions(CharacteristicFactory.Create<bool>(service, CharacteristicBase.OnType, value, 8), "bool");
            state.Value = false;

            service.Characteristics.Add(state);

            return service;
        }


        public static Service CreateTemperatureSensor(Accessory accessory, int id, double value)
        {
            var service = new Service(accessory)
            {
                Type = TemperatureSensorType,
                Id = id
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

            if (c.EventBasedNotification.HasValue && c.EventBasedNotification.Value)
                c.Permissions.Add("ev");
            c.Format = format;

            return c;
        }
    }
}
