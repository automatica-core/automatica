using System;
using System.Collections.Generic;
using System.Linq;

namespace P3.Driver.HomeKit.Hap.Model
{
    /// <summary>
    /// HAP characteristic formats (HAP spec chapter 6.7.4)
    /// </summary>
    public static class CharacteristicFormat
    {
        public const string Bool = "bool";
        public const string UInt8 = "uint8";
        public const string UInt16 = "uint16";
        public const string UInt32 = "uint32";
        public const string Int = "int";
        public const string Float = "float";
        public const string String = "string";
    }

    /// <summary>
    /// HAP characteristic units
    /// </summary>
    public static class CharacteristicUnit
    {
        public const string Celsius = "celsius";
        public const string Percentage = "percentage";
        public const string Lux = "lux";
        public const string Seconds = "seconds";
        public const string ArcDegrees = "arcdegrees";
    }

    /// <summary>
    /// Describes a HAP characteristic (type, format, permissions and metadata) so it can be created for any service.
    /// </summary>
    public sealed class CharacteristicDefinition
    {
        public CharacteristicDefinition(string type, string format, bool canWrite, object defaultValue, double? minValue = null, double? maxValue = null,
            double? minStep = null, string unit = null, IEnumerable<int> validValues = null, bool canRead = true, bool readsAsNull = false)
        {
            Type = type;
            Format = format;
            CanWrite = canWrite;
            CanRead = canRead;
            ReadsAsNull = readsAsNull;
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
            MinStep = minStep;
            Unit = unit;
            ValidValues = validValues?.ToList();
        }

        public string Type { get; }
        public string Format { get; }
        public bool CanWrite { get; }

        /// <summary>
        /// False for write only characteristics (e.g. <see cref="CharacteristicBase.HoldPositionType"/>).
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        /// True for event characteristics (e.g. <see cref="CharacteristicBase.ProgrammableSwitchEventType"/>) which are always read as null.
        /// </summary>
        public bool ReadsAsNull { get; }

        public object DefaultValue { get; }
        public double? MinValue { get; }
        public double? MaxValue { get; }
        public double? MinStep { get; }
        public string Unit { get; }
        public IReadOnlyList<int> ValidValues { get; }

        public Type ValueType
        {
            get
            {
                if (!CanRead || ReadsAsNull)
                {
                    return typeof(object); // read returns null
                }

                switch (Format)
                {
                    case CharacteristicFormat.Bool:
                        return typeof(bool);
                    case CharacteristicFormat.Float:
                        return typeof(double);
                    case CharacteristicFormat.String:
                        return typeof(string);
                    case CharacteristicFormat.UInt32:
                        return typeof(long);
                    default:
                        return typeof(int);
                }
            }
        }

        public Characteristic Create(Service service, int id)
        {
            var permissions = new List<string>();
            if (CanRead)
            {
                permissions.Add("pr");
            }

            if (CanWrite)
            {
                permissions.Add("pw");
            }

            if (CanRead)
            {
                permissions.Add("ev");
            }

            return new Characteristic(service, ValueType)
            {
                Type = Type,
                Id = id,
                Format = Format,
                Permissions = permissions,
                EventBasedNotification = CanRead,
                Value = DefaultValue,
                MinValue = MinValue,
                MaxValue = MaxValue,
                MinStep = MinStep,
                Unit = Unit,
                ValidValues = ValidValues?.ToList()
            };
        }

        /// <summary>
        /// Converts a value into the representation of this characteristic (format, range and step).
        /// </summary>
        public object ConvertValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            switch (Format)
            {
                case CharacteristicFormat.Bool:
                    return Convert.ToBoolean(value);
                case CharacteristicFormat.String:
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                case CharacteristicFormat.Float:
                {
                    var d = Clamp(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                    if (MinStep.HasValue && MinStep.Value > 0)
                    {
                        var origin = MinValue ?? 0;
                        d = origin + Math.Round((d - origin) / MinStep.Value, MidpointRounding.AwayFromZero) * MinStep.Value;
                        d = Math.Round(Clamp(d), 10);
                    }

                    return d;
                }
                default:
                {
                    var d = value is bool b ? (b ? 1 : 0) : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    var l = (long)Math.Round(Clamp(d), MidpointRounding.AwayFromZero);
                    l = Math.Clamp(l, FormatMin, FormatMax);

                    if (ValidValues != null && ValidValues.Count > 0 && !ValidValues.Contains((int)l))
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value, $"Valid values for characteristic {Type} are {string.Join(",", ValidValues)}");
                    }

                    return Format == CharacteristicFormat.UInt32 ? l : (object)(int)l;
                }
            }
        }

        private long FormatMin => Format == CharacteristicFormat.Int ? int.MinValue : 0;

        private long FormatMax
        {
            get
            {
                switch (Format)
                {
                    case CharacteristicFormat.UInt8:
                        return byte.MaxValue;
                    case CharacteristicFormat.UInt16:
                        return ushort.MaxValue;
                    case CharacteristicFormat.UInt32:
                        return uint.MaxValue;
                    default:
                        return int.MaxValue;
                }
            }
        }

        private double Clamp(double d)
        {
            if (double.IsNaN(d))
            {
                return MinValue ?? 0;
            }

            if (MinValue.HasValue && d < MinValue.Value)
            {
                d = MinValue.Value;
            }

            if (MaxValue.HasValue && d > MaxValue.Value)
            {
                d = MaxValue.Value;
            }

            return d;
        }
    }

    /// <summary>
    /// Definitions of the HAP characteristics (Apple HomeKit Accessory Protocol, Non-Commercial Version R2, chapter 9)
    /// </summary>
    public static class HapCharacteristics
    {
        private static CharacteristicDefinition Detected(string type) =>
            new CharacteristicDefinition(type, CharacteristicFormat.UInt8, false, 0, 0, 1, 1, validValues: new[] { 0, 1 });

        public static CharacteristicDefinition On => new CharacteristicDefinition(CharacteristicBase.OnType, CharacteristicFormat.Bool, true, false);
        public static CharacteristicDefinition OutletInUse => new CharacteristicDefinition(CharacteristicBase.OutletInUseType, CharacteristicFormat.Bool, false, true);

        public static CharacteristicDefinition CurrentTemperature => new CharacteristicDefinition(CharacteristicBase.CurrentTemperatureType, CharacteristicFormat.Float, false, 0d, -270, 100, 0.1, CharacteristicUnit.Celsius);
        public static CharacteristicDefinition TargetTemperature => new CharacteristicDefinition(CharacteristicBase.TargetTemperatureType, CharacteristicFormat.Float, true, 21d, 10, 38, 0.5, CharacteristicUnit.Celsius);
        public static CharacteristicDefinition TemperatureDisplayUnits => new CharacteristicDefinition(CharacteristicBase.TemperatureDisplayUnitsType, CharacteristicFormat.UInt8, true, 0, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition CurrentHeatingCoolingState => new CharacteristicDefinition(CharacteristicBase.CurrentHeatingCoolingStateType, CharacteristicFormat.UInt8, false, 0, 0, 2, 1, validValues: new[] { 0, 1, 2 });
        public static CharacteristicDefinition TargetHeatingCoolingState => new CharacteristicDefinition(CharacteristicBase.TargetHeatingCoolingStateType, CharacteristicFormat.UInt8, true, 0, 0, 3, 1, validValues: new[] { 0, 1, 2, 3 });

        public static CharacteristicDefinition CurrentRelativeHumidity => new CharacteristicDefinition(CharacteristicBase.CurrentRelativeHumidityType, CharacteristicFormat.Float, false, 0d, 0, 100, 1, CharacteristicUnit.Percentage);
        public static CharacteristicDefinition CurrentAmbientLightLevel => new CharacteristicDefinition(CharacteristicBase.CurrentAmbientLightLevelType, CharacteristicFormat.Float, false, 0.0001, 0.0001, 100000, unit: CharacteristicUnit.Lux);

        public static CharacteristicDefinition MotionDetected => new CharacteristicDefinition(CharacteristicBase.MotionDetectedType, CharacteristicFormat.Bool, false, false);
        public static CharacteristicDefinition OccupancyDetected => Detected(CharacteristicBase.OccupancyDetectedType);
        public static CharacteristicDefinition LeakDetected => Detected(CharacteristicBase.LeakDetectedType);
        public static CharacteristicDefinition SmokeDetected => Detected(CharacteristicBase.SmokeDetectedType);
        public static CharacteristicDefinition CarbonMonoxideDetected => Detected(CharacteristicBase.CarbonMonoxideDetectedType);
        public static CharacteristicDefinition CarbonMonoxideLevel => new CharacteristicDefinition(CharacteristicBase.CarbonMonoxideLevelType, CharacteristicFormat.Float, false, 0d, 0, 100);
        public static CharacteristicDefinition CarbonDioxideDetected => Detected(CharacteristicBase.CarbonDioxideDetectedType);
        public static CharacteristicDefinition CarbonDioxideLevel => new CharacteristicDefinition(CharacteristicBase.CarbonDioxideLevelType, CharacteristicFormat.Float, false, 0d, 0, 100000);
        public static CharacteristicDefinition ContactSensorState => Detected(CharacteristicBase.ContactSensorStateType);

        public static CharacteristicDefinition AirQuality => new CharacteristicDefinition(CharacteristicBase.AirQualityType, CharacteristicFormat.UInt8, false, 0, 0, 5, 1, validValues: new[] { 0, 1, 2, 3, 4, 5 });
        public static CharacteristicDefinition Pm25Density => new CharacteristicDefinition(CharacteristicBase.Pm25DensityType, CharacteristicFormat.Float, false, 0d, 0, 1000, 1);
        public static CharacteristicDefinition Pm10Density => new CharacteristicDefinition(CharacteristicBase.Pm10DensityType, CharacteristicFormat.Float, false, 0d, 0, 1000, 1);
        public static CharacteristicDefinition VocDensity => new CharacteristicDefinition(CharacteristicBase.VocDensityType, CharacteristicFormat.Float, false, 0d, 0, 1000, 1);

        public static CharacteristicDefinition RotationSpeed => new CharacteristicDefinition(CharacteristicBase.RotationSpeedType, CharacteristicFormat.Float, true, 0d, 0, 100, 1, CharacteristicUnit.Percentage);

        public static CharacteristicDefinition CurrentDoorState => new CharacteristicDefinition(CharacteristicBase.CurrentDoorStateType, CharacteristicFormat.UInt8, false, 1, 0, 4, 1, validValues: new[] { 0, 1, 2, 3, 4 });
        public static CharacteristicDefinition TargetDoorState => new CharacteristicDefinition(CharacteristicBase.TargetDoorStateType, CharacteristicFormat.UInt8, true, 1, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition ObstructionDetected => new CharacteristicDefinition(CharacteristicBase.ObstructionDetectedType, CharacteristicFormat.Bool, false, false);

        public static CharacteristicDefinition LockCurrentState => new CharacteristicDefinition(CharacteristicBase.LockCurrentStateType, CharacteristicFormat.UInt8, false, 3, 0, 3, 1, validValues: new[] { 0, 1, 2, 3 });
        public static CharacteristicDefinition LockTargetState => new CharacteristicDefinition(CharacteristicBase.LockTargetStateType, CharacteristicFormat.UInt8, true, 1, 0, 1, 1, validValues: new[] { 0, 1 });

        public static CharacteristicDefinition CurrentPosition => new CharacteristicDefinition(CharacteristicBase.CurrentPositionType, CharacteristicFormat.UInt8, false, 0, 0, 100, 1, CharacteristicUnit.Percentage);
        public static CharacteristicDefinition TargetPosition => new CharacteristicDefinition(CharacteristicBase.TargetPositionType, CharacteristicFormat.UInt8, true, 0, 0, 100, 1, CharacteristicUnit.Percentage);
        public static CharacteristicDefinition PositionState => new CharacteristicDefinition(CharacteristicBase.PositionStateType, CharacteristicFormat.UInt8, false, 2, 0, 2, 1, validValues: new[] { 0, 1, 2 });
        public static CharacteristicDefinition HoldPosition => new CharacteristicDefinition(CharacteristicBase.HoldPositionType, CharacteristicFormat.Bool, true, null, canRead: false);

        public static CharacteristicDefinition ProgrammableSwitchEvent => new CharacteristicDefinition(CharacteristicBase.ProgrammableSwitchEventType, CharacteristicFormat.UInt8, false, null, 0, 2, 1, validValues: new[] { 0, 1, 2 }, readsAsNull: true);

        public static CharacteristicDefinition Active => new CharacteristicDefinition(CharacteristicBase.ActiveType, CharacteristicFormat.UInt8, true, 0, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition InUse => new CharacteristicDefinition(CharacteristicBase.InUseType, CharacteristicFormat.UInt8, false, 0, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition ValveType(int valveType) => new CharacteristicDefinition(CharacteristicBase.ValveTypeType, CharacteristicFormat.UInt8, false, valveType, 0, 3, 1, validValues: new[] { 0, 1, 2, 3 });
        public static CharacteristicDefinition SetDuration => new CharacteristicDefinition(CharacteristicBase.SetDurationType, CharacteristicFormat.UInt32, true, 0L, 0, 3600, 1, CharacteristicUnit.Seconds);
        public static CharacteristicDefinition RemainingDuration => new CharacteristicDefinition(CharacteristicBase.RemainingDurationType, CharacteristicFormat.UInt32, false, 0L, 0, 3600, 1, CharacteristicUnit.Seconds);

        public static CharacteristicDefinition SecuritySystemCurrentState => new CharacteristicDefinition(CharacteristicBase.SecuritySystemCurrentStateType, CharacteristicFormat.UInt8, false, 3, 0, 4, 1, validValues: new[] { 0, 1, 2, 3, 4 });
        public static CharacteristicDefinition SecuritySystemTargetState => new CharacteristicDefinition(CharacteristicBase.SecuritySystemTargetStateType, CharacteristicFormat.UInt8, true, 3, 0, 3, 1, validValues: new[] { 0, 1, 2, 3 });

        public static CharacteristicDefinition BatteryLevel => new CharacteristicDefinition(CharacteristicBase.BatteryLevelType, CharacteristicFormat.UInt8, false, 100, 0, 100, 1, CharacteristicUnit.Percentage);
        public static CharacteristicDefinition ChargingState => new CharacteristicDefinition(CharacteristicBase.ChargingStateType, CharacteristicFormat.UInt8, false, 2, 0, 2, 1, validValues: new[] { 0, 1, 2 });
        public static CharacteristicDefinition StatusLowBattery => new CharacteristicDefinition(CharacteristicBase.StatusLowBatteryType, CharacteristicFormat.UInt8, false, 0, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition StatusFault => new CharacteristicDefinition(CharacteristicBase.StatusFaultType, CharacteristicFormat.UInt8, false, 0, 0, 1, 1, validValues: new[] { 0, 1 });
        public static CharacteristicDefinition StatusTampered => new CharacteristicDefinition(CharacteristicBase.StatusTamperedType, CharacteristicFormat.UInt8, false, 0, 0, 1, 1, validValues: new[] { 0, 1 });
    }
}
