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
        public const string HumiditySensorType = "82";
        public const string LightSensorType = "84";
        public const string MotionSensorType = "85";
        public const string OccupancySensorType = "86";
        public const string LeakSensorType = "83";
        public const string SmokeSensorType = "87";
        public const string CarbonMonoxideSensorType = "7F";
        public const string CarbonDioxideSensorType = "97";
        public const string AirQualitySensorType = "8D";
        public const string FanType = "40";
        public const string ThermostatType = "4A";
        public const string GarageDoorOpenerType = "41";
        public const string LockMechanismType = "45";
        public const string DoorType = "81";
        public const string WindowType = "8B";
        public const string StatelessProgrammableSwitchType = "89";
        public const string ValveType = "D0";
        public const string SecuritySystemType = "7E";
        public const string BatteryServiceType = "96";
        public const string SpeakerType = "113";

        /// <summary>
        /// Instance id of the primary service of accessories created by this factory, the accessory information service uses 1-6.
        /// </summary>
        public const int PrimaryServiceId = 7;

        /// <summary>
        /// Creates an accessory with the accessory information service and one primary service with the given (mandatory) characteristics.
        /// </summary>
        public static Accessory CreateServiceAccessory(ulong aid, string name, string manufacturer, string serial, string serviceType,
            params CharacteristicDefinition[] characteristics)
        {
            var accessory = new Accessory
            {
                Id = aid
            };

            accessory.AccessoryInfo = CreateAccessoryInfo(accessory, 1, name, manufacturer, serial);
            accessory.Services.Add(accessory.AccessoryInfo);

            accessory.Specific = new Service(accessory)
            {
                Type = serviceType,
                Id = PrimaryServiceId
            };
            accessory.Services.Add(accessory.Specific);

            foreach (var characteristic in characteristics)
            {
                accessory.Specific.AddCharacteristic(characteristic);
            }

            return accessory;
        }

        /// <summary>
        /// Next free instance id, services and characteristics share the instance id space of an accessory.
        /// </summary>
        public static int NextInstanceId(this Accessory accessory)
        {
            var max = 0;
            foreach (var service in accessory.Services)
            {
                max = System.Math.Max(max, service.Id);
                foreach (var characteristic in service.Characteristics)
                {
                    max = System.Math.Max(max, characteristic.Id);
                }
            }

            return max + 1;
        }

        public static Service AddService(this Accessory accessory, string serviceType, params CharacteristicDefinition[] characteristics)
        {
            var service = new Service(accessory)
            {
                Type = serviceType,
                Id = accessory.NextInstanceId()
            };
            accessory.Services.Add(service);

            foreach (var characteristic in characteristics)
            {
                service.AddCharacteristic(characteristic);
            }

            return service;
        }

        /// <summary>
        /// Returns the characteristic of the given type or adds it (optional characteristics are only added when they are used).
        /// </summary>
        public static Characteristic GetOrAddCharacteristic(this Service service, CharacteristicDefinition definition)
        {
            foreach (var characteristic in service.Characteristics)
            {
                if (characteristic.Type == definition.Type)
                {
                    return characteristic;
                }
            }

            return service.AddCharacteristic(definition);
        }

        public static Characteristic AddCharacteristic(this Service service, CharacteristicDefinition definition)
        {
            var characteristic = definition.Create(service, service.Accessory.NextInstanceId());
            service.Characteristics.Add(characteristic);
            return characteristic;
        }

        /// <summary>
        /// Returns the battery service of the accessory or adds it (battery level, charging state and low battery are mandatory).
        /// </summary>
        public static Service GetOrAddBatteryService(this Accessory accessory)
        {
            foreach (var service in accessory.Services)
            {
                if (service.Type == BatteryServiceType)
                {
                    return service;
                }
            }

            return accessory.AddService(BatteryServiceType, HapCharacteristics.BatteryLevel, HapCharacteristics.ChargingState, HapCharacteristics.StatusLowBattery);
        }

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

        public static AccessoryInfoService CreateAccessoryInfo(Accessory accessory, int id, string name, string manufacturer, string serial)
        {
            var service = new AccessoryInfoService(accessory)
            {
                Type = AccessoryInformationType,
                Id = id
            };


            service.Name = CharacteristicFactory.CreateName(service, name, 2);
            service.Characteristics.Add(service.Name);
            
            service.Characteristics.Add(CharacteristicFactory.CreateManufacturer(service, manufacturer, 3));
            
            service.Serial = CharacteristicFactory.CreateSerial(service, serial, 4);
            service.Characteristics.Add(service.Serial);
            
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

        public static AccessoryInfoService CreateAccessoryInfo(Accessory accessory, int id, string name, string manufacturer, string serial, string firmwareRevision)
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
            // brightness and hue are set by the controller => paired write
            return accessory.Create<double>(CharacteristicBase.BrightnessType, 0, "int", true);
        }
        public static Characteristic CreateHue(this Accessory accessory)
        {
            return accessory.Create<float>(CharacteristicBase.HueType, 0, "float", true);
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
