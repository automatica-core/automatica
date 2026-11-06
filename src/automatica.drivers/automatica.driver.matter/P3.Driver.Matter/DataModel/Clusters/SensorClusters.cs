using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel.Clusters
{
    internal static class TlvNullable
    {
        public static void PutNullableInt(this TlvWriter w, byte? tag, long? value)
        {
            if (value.HasValue)
            {
                w.PutInt(tag, value.Value);
            }
            else
            {
                w.PutNull(tag);
            }
        }

        public static void PutNullableUInt(this TlvWriter w, byte? tag, ulong? value)
        {
            if (value.HasValue)
            {
                w.PutUInt(tag, value.Value);
            }
            else
            {
                w.PutNull(tag);
            }
        }

        public static void PutNullableFloat(this TlvWriter w, byte? tag, float? value)
        {
            if (value.HasValue)
            {
                w.PutFloat(tag, value.Value);
            }
            else
            {
                w.PutNull(tag);
            }
        }
    }

    /// <summary>Boolean State (0x0045): contact, water leak and rain sensors.</summary>
    public sealed class BooleanStateCluster : ClusterServer
    {
        public const uint Id = 0x0045;
        public const uint AttributeStateValue = 0x0000;

        private bool _state;

        public BooleanStateCluster() : base(Id, 1)
        {
            Attribute(AttributeStateValue, (w, t, _) => w.PutBool(t, _state));
        }

        public bool State => _state;

        public void SetState(bool state)
        {
            if (state != _state)
            {
                _state = state;
                MarkChanged(AttributeStateValue);
            }
        }
    }

    /// <summary>
    /// The numeric measurement clusters (illuminance, temperature, pressure, flow, humidity): MeasuredValue, MinMeasuredValue,
    /// MaxMeasuredValue. The raw value is in the cluster's unit (see the factory methods).
    /// </summary>
    public sealed class MeasurementCluster : ClusterServer
    {
        public const uint IlluminanceId = 0x0400;
        public const uint TemperatureId = 0x0402;
        public const uint PressureId = 0x0403;
        public const uint FlowId = 0x0404;
        public const uint HumidityId = 0x0405;
        public const uint AttributeMeasuredValue = 0x0000;

        private readonly bool _signed;
        private readonly long? _min;
        private readonly long? _max;
        private readonly Func<double, long> _toRaw;
        private long? _value;

        private MeasurementCluster(uint id, ushort revision, bool signed, long? min, long? max, Func<double, long> toRaw) : base(id, revision)
        {
            _signed = signed;
            _min = min;
            _max = max;
            _toRaw = toRaw;
            Attribute(AttributeMeasuredValue, (w, t, _) => Put(w, t, _value));
            Attribute(0x0001, (w, t, _) => Put(w, t, _min));
            Attribute(0x0002, (w, t, _) => Put(w, t, _max));
        }

        /// <summary>Illuminance in lux, encoded as 10000 * log10(lux) + 1.</summary>
        public static MeasurementCluster Illuminance() => new(IlluminanceId, 3, false, 1, 0xFFFE,
            lux => lux <= 0 ? 0 : (long)Math.Round(10000 * Math.Log10(lux) + 1));

        /// <summary>Temperature in °C, encoded in 1/100 °C.</summary>
        public static MeasurementCluster Temperature() => new(TemperatureId, 4, true, -27315, 32767, c => (long)Math.Round(c * 100));

        /// <summary>Pressure in hPa (= 1/10 kPa, the cluster's unit).</summary>
        public static MeasurementCluster Pressure() => new(PressureId, 3, true, -32767, 32767, hPa => (long)Math.Round(hPa));

        /// <summary>Flow in m³/h, encoded in 1/10 m³/h.</summary>
        public static MeasurementCluster Flow() => new(FlowId, 3, false, 0, 0xFFFE, m3h => (long)Math.Round(m3h * 10));

        /// <summary>Relative humidity in %, encoded in 1/100 %.</summary>
        public static MeasurementCluster Humidity() => new(HumidityId, 3, false, 0, 10000, p => (long)Math.Round(p * 100));

        public long? RawValue => _value;

        private void Put(TlvWriter w, byte? tag, long? value)
        {
            if (_signed)
            {
                w.PutNullableInt(tag, value);
            }
            else
            {
                w.PutNullableUInt(tag, value.HasValue ? (ulong)value.Value : null);
            }
        }

        /// <summary>Sets the measured value in the natural unit (null = unknown).</summary>
        public void SetValue(double? value)
        {
            long? raw = value.HasValue && !double.IsNaN(value.Value) ? Math.Clamp(_toRaw(value.Value), _min ?? long.MinValue, _max ?? long.MaxValue) : null;
            if (raw == 0 && Id == IlluminanceId && value > 0)
            {
                raw = 1;
            }

            if (raw != _value)
            {
                _value = raw;
                MarkChanged(AttributeMeasuredValue);
            }
        }

        private uint Id => ClusterId;
    }

    /// <summary>Occupancy Sensing (0x0406), passive infrared.</summary>
    public sealed class OccupancySensingCluster : ClusterServer
    {
        public const uint Id = 0x0406;
        public const uint AttributeOccupancy = 0x0000;

        private bool _occupied;

        public OccupancySensingCluster() : base(Id, 5)
        {
            FeatureMap = 0x02; // PassiveInfrared
            Attribute(AttributeOccupancy, (w, t, _) => w.PutUInt(t, _occupied ? 1u : 0u));
            Attribute(0x0001, (w, t, _) => w.PutUInt(t, 0)); // OccupancySensorType: PIR
            Attribute(0x0002, (w, t, _) => w.PutUInt(t, 0x01)); // OccupancySensorTypeBitmap: PIR
        }

        public void SetOccupied(bool occupied)
        {
            if (occupied != _occupied)
            {
                _occupied = occupied;
                MarkChanged(AttributeOccupancy);
            }
        }
    }

    /// <summary>Air Quality (0x005B): 0 unknown, 1 good, 2 fair, 3 moderate, 4 poor, 5 very poor, 6 extremely poor.</summary>
    public sealed class AirQualityCluster : ClusterServer
    {
        public const uint Id = 0x005B;
        public const uint AttributeAirQuality = 0x0000;

        private byte _quality;

        public AirQualityCluster() : base(Id, 1)
        {
            FeatureMap = 0x0F; // Fair, Moderate, VeryPoor, ExtremelyPoor
            Attribute(AttributeAirQuality, (w, t, _) => w.PutUInt(t, _quality));
        }

        public void SetQuality(int quality)
        {
            var q = (byte)Math.Clamp(quality, 0, 6);
            if (q != _quality)
            {
                _quality = q;
                MarkChanged(AttributeAirQuality);
            }
        }
    }

    public enum ConcentrationUnit : byte
    {
        Ppm = 0,
        Ppb = 1,
        Ppt = 2,
        MilligramsPerCubicMeter = 3,
        MicrogramsPerCubicMeter = 4,
        NanogramsPerCubicMeter = 5,
        ParticlesPerCubicMeter = 6,
        BecquerelsPerCubicMeter = 7
    }

    /// <summary>The concentration measurement clusters (CO2, PM2.5, ...), numeric measurement feature only.</summary>
    public sealed class ConcentrationMeasurementCluster : ClusterServer
    {
        public const uint CarbonMonoxideId = 0x040C;
        public const uint CarbonDioxideId = 0x040D;
        public const uint NitrogenDioxideId = 0x0413;
        public const uint OzoneId = 0x0415;
        public const uint Pm25Id = 0x042A;
        public const uint FormaldehydeId = 0x042B;
        public const uint Pm1Id = 0x042C;
        public const uint Pm10Id = 0x042D;
        public const uint TvocId = 0x042E;
        public const uint RadonId = 0x042F;
        public const uint AttributeMeasuredValue = 0x0000;

        private float? _value;

        public ConcentrationMeasurementCluster(uint clusterId, ConcentrationUnit unit) : base(clusterId, 3)
        {
            FeatureMap = 0x01; // NumericMeasurement
            Attribute(AttributeMeasuredValue, (w, t, _) => w.PutNullableFloat(t, _value));
            Attribute(0x0001, (w, t, _) => w.PutNullableFloat(t, 0));
            Attribute(0x0002, (w, t, _) => w.PutNull(t));
            Attribute(0x0008, (w, t, _) => w.PutUInt(t, (byte)unit));
            Attribute(0x0009, (w, t, _) => w.PutUInt(t, 0)); // medium: air
        }

        public void SetValue(double? value)
        {
            float? v = value.HasValue && !double.IsNaN(value.Value) ? (float)Math.Max(0, value.Value) : null;
            if (v != _value)
            {
                _value = v;
                MarkChanged(AttributeMeasuredValue);
            }
        }
    }

    /// <summary>Power Source (0x002F) with the battery feature, for battery powered sensors.</summary>
    public sealed class PowerSourceCluster : ClusterServer
    {
        public const uint Id = 0x002F;
        public const uint AttributeBatPercentRemaining = 0x000C;
        public const uint AttributeBatChargeLevel = 0x000E;
        public const uint AttributeBatReplacementNeeded = 0x000F;

        private byte? _percentTimes2;

        public PowerSourceCluster() : base(Id, 2)
        {
            FeatureMap = 0x02; // Battery
            Attribute(0x0000, (w, t, _) => w.PutUInt(t, 1)); // Status: active
            Attribute(0x0001, (w, t, _) => w.PutUInt(t, 0)); // Order
            Attribute(0x0002, (w, t, _) => w.PutString(t, "Battery"));
            Attribute(AttributeBatPercentRemaining, (w, t, _) => w.PutNullableUInt(t, _percentTimes2));
            Attribute(AttributeBatChargeLevel, (w, t, _) => w.PutUInt(t, ChargeLevel));
            Attribute(AttributeBatReplacementNeeded, (w, t, _) => w.PutBool(t, ChargeLevel == 2));
            Attribute(0x0010, (w, t, _) => w.PutUInt(t, 0)); // BatReplaceability: unspecified
            Attribute(0x001F, (w, t, _) =>
            {
                w.StartArray(t);
                if (Endpoint != null)
                {
                    w.PutUInt(null, Endpoint.Id);
                }

                w.EndContainer();
            });
        }

        private byte ChargeLevel => _percentTimes2 switch
        {
            null => 0,
            < 20 => 2,
            < 40 => 1,
            _ => 0
        };

        /// <summary>Battery level in percent (null = unknown).</summary>
        public void SetPercent(double? percent)
        {
            byte? value = percent.HasValue && !double.IsNaN(percent.Value) ? (byte)Math.Clamp(Math.Round(percent.Value * 2), 0, 200) : null;
            if (value == _percentTimes2)
            {
                return;
            }

            var oldLevel = ChargeLevel;
            _percentTimes2 = value;
            MarkChanged(AttributeBatPercentRemaining);
            if (oldLevel != ChargeLevel)
            {
                MarkChanged(AttributeBatChargeLevel);
                MarkChanged(AttributeBatReplacementNeeded);
            }
        }
    }

    /// <summary>Smoke CO Alarm (0x005C). States: 0 normal, 1 warning, 2 critical.</summary>
    public sealed class SmokeCoAlarmCluster : ClusterServer
    {
        public const uint Id = 0x005C;
        public const uint AttributeExpressedState = 0x0000;
        public const uint AttributeSmokeState = 0x0001;
        public const uint AttributeCoState = 0x0002;

        private byte _smoke;
        private byte _co;

        public SmokeCoAlarmCluster(bool smoke, bool co) : base(Id, 1)
        {
            FeatureMap = (smoke ? 0x01u : 0u) | (co ? 0x02u : 0u);
            Attribute(AttributeExpressedState, (w, t, _) => w.PutUInt(t, ExpressedState));
            if (smoke)
            {
                Attribute(AttributeSmokeState, (w, t, _) => w.PutUInt(t, _smoke));
            }

            if (co)
            {
                Attribute(AttributeCoState, (w, t, _) => w.PutUInt(t, _co));
            }

            Attribute(0x0003, (w, t, _) => w.PutUInt(t, 0)); // BatteryAlert: normal
            Attribute(0x0005, (w, t, _) => w.PutBool(t, false)); // TestInProgress
            Attribute(0x0006, (w, t, _) => w.PutBool(t, false)); // HardwareFaultAlert
            Attribute(0x0007, (w, t, _) => w.PutUInt(t, 0)); // EndOfServiceAlert: normal
        }

        // 0 normal, 1 smoke alarm, 2 CO alarm
        private byte ExpressedState => _smoke > 0 ? (byte)1 : _co > 0 ? (byte)2 : (byte)0;

        public void SetSmoke(int state) => Set(ref _smoke, state, AttributeSmokeState);

        public void SetCo(int state) => Set(ref _co, state, AttributeCoState);

        private void Set(ref byte field, int state, uint attribute)
        {
            var value = (byte)Math.Clamp(state, 0, 2);
            if (value == field)
            {
                return;
            }

            var expressed = ExpressedState;
            field = value;
            MarkChanged(attribute);
            if (expressed != ExpressedState)
            {
                MarkChanged(AttributeExpressedState);
            }
        }
    }
}
