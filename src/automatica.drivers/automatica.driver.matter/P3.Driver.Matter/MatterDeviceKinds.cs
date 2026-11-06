using System.Globalization;
using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.DataModel.Clusters;

namespace P3.Driver.Matter
{
    public enum MatterValueType
    {
        Boolean,
        Integer,
        Double,
        String
    }

    [Flags]
    public enum MatterPointDirection
    {
        /// <summary>The value is written by the host (Automatica) and shown by the controllers.</summary>
        ToMatter = 1,

        /// <summary>The value is set by a controller and handed to the host.</summary>
        FromMatter = 2,

        Both = ToMatter | FromMatter
    }

    /// <summary>A value of a bridged device (a node in Automatica).</summary>
    public sealed record MatterDataPoint(string Key, MatterValueType Type, MatterPointDirection Direction, bool Optional = false);

    /// <summary>A kind of bridged device: Matter device type, its data points and how its clusters are built.</summary>
    public sealed class MatterDeviceKind
    {
        internal MatterDeviceKind(string key, string displayName, DeviceType deviceType, MatterDataPoint[] points, Action<DeviceBuilder> build)
        {
            Key = key;
            DisplayName = displayName;
            DeviceType = deviceType;
            Points = points;
            Build = build;
        }

        /// <summary>Stable identifier (used for templates and translations, never change it).</summary>
        public string Key { get; }

        public string DisplayName { get; }
        public DeviceType DeviceType { get; }
        public IReadOnlyList<MatterDataPoint> Points { get; }
        internal Action<DeviceBuilder> Build { get; }

        public MatterDataPoint GetPoint(string key) => Points.FirstOrDefault(p => p.Key == key);
    }

    internal sealed class DeviceBuilder
    {
        private readonly ISet<string> _enabled;

        public DeviceBuilder(Endpoint endpoint, BridgedGenericDevice device, ISet<string> enabled)
        {
            Endpoint = endpoint;
            Device = device;
            _enabled = enabled;
        }

        public Endpoint Endpoint { get; }
        public BridgedGenericDevice Device { get; }

        public bool Has(string point) => _enabled.Contains(point);

        public T Add<T>(T cluster) where T : ClusterServer
        {
            Endpoint.Add(cluster);
            return cluster;
        }

        public void OnSet(string point, Action<object> setter) => Device.RegisterSetter(point, setter);

        public Task Request(string point, object value) => Device.RequestAsync(point, value);

        /// <summary>Adds the Power Source cluster when the device has a battery data point.</summary>
        public void Battery()
        {
            if (!Has(MatterDeviceKinds.BatteryPoint))
            {
                return;
            }

            var power = Add(new PowerSourceCluster());
            Endpoint.DeviceTypes.Add(DataModel.DeviceType.PowerSource);
            OnSet(MatterDeviceKinds.BatteryPoint, v => power.SetPercent(MatterValues.ToNullableDouble(v)));
        }
    }

    internal static class MatterValues
    {
        public static bool ToBool(object value) => value switch
        {
            null => false,
            bool b => b,
            string s => bool.TryParse(s, out var b) ? b : double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0
        };

        public static double? ToNullableDouble(object value) => value switch
        {
            null => null,
            bool b => b ? 1 : 0,
            string s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };

        public static double ToDouble(object value) => ToNullableDouble(value) ?? 0;

        public static int ToInt(object value) => (int)Math.Round(ToDouble(value));
    }

    /// <summary>The device kinds a host can publish through <see cref="BridgedGenericDevice"/>.</summary>
    public static class MatterDeviceKinds
    {
        public const string BatteryPoint = "battery";

        private static readonly MatterDataPoint Battery = new(BatteryPoint, MatterValueType.Double, MatterPointDirection.ToMatter, true);

        public static readonly MatterDeviceKind OnOffLight = new("on-off-light", "On/Off Light", DeviceType.OnOffLight,
            new[] { OnOffPoint() }, b => Lighting(b, false, false));

        public static readonly MatterDeviceKind DimmableLight = new("dimmable-light", "Dimmable Light", DeviceType.DimmableLight,
            new[] { OnOffPoint(), LevelPoint() }, b => Lighting(b, true, false));

        public static readonly MatterDeviceKind ColorTemperatureLight = new("color-temperature-light", "Color Temperature Light", DeviceType.ColorTemperatureLight,
            new[] { OnOffPoint(), LevelPoint(), new MatterDataPoint("color-temperature", MatterValueType.Integer, MatterPointDirection.Both) },
            b =>
            {
                Lighting(b, true, false);
                Color(b, false);
            });

        public static readonly MatterDeviceKind ExtendedColorLight = new("extended-color-light", "Extended Color Light", DeviceType.ExtendedColorLight,
            new[]
            {
                OnOffPoint(), LevelPoint(), new MatterDataPoint("color", MatterValueType.String, MatterPointDirection.Both),
                new MatterDataPoint("color-temperature", MatterValueType.Integer, MatterPointDirection.Both, true)
            },
            b =>
            {
                Lighting(b, true, false);
                Color(b, true);
            });

        public static readonly MatterDeviceKind OnOffPlugInUnit = new("on-off-plug", "On/Off Plug-in Unit", DeviceType.OnOffPlugInUnit,
            new[] { OnOffPoint() }, b => Lighting(b, false, true));

        public static readonly MatterDeviceKind DimmablePlugInUnit = new("dimmable-plug", "Dimmable Plug-in Unit", DeviceType.DimmablePlugInUnit,
            new[] { OnOffPoint(), LevelPoint() }, b => Lighting(b, true, true));

        public static readonly MatterDeviceKind WindowCovering = new("window-covering", "Window Covering", DeviceType.WindowCovering,
            new[]
            {
                new MatterDataPoint("position", MatterValueType.Integer, MatterPointDirection.Both),
                new MatterDataPoint("open", MatterValueType.Boolean, MatterPointDirection.FromMatter, true),
                new MatterDataPoint("close", MatterValueType.Boolean, MatterPointDirection.FromMatter, true),
                new MatterDataPoint("stop", MatterValueType.Boolean, MatterPointDirection.FromMatter, true)
            },
            b =>
            {
                var covering = b.Add(new WindowCoveringCluster());
                covering.CommandRequested = async (command, target) =>
                {
                    switch (command)
                    {
                        case CoveringCommand.Open when b.Has("open"):
                            await b.Request("open", true);
                            break;
                        case CoveringCommand.Close when b.Has("close"):
                            await b.Request("close", true);
                            break;
                        case CoveringCommand.Stop:
                            if (b.Has("stop"))
                            {
                                await b.Request("stop", true);
                            }

                            break;
                        default:
                            await b.Request("position", (int)Math.Round((target ?? 0) / 100.0));
                            break;
                    }
                };
                b.OnSet("position", v =>
                {
                    var percent = MatterValues.ToNullableDouble(v);
                    covering.SetState(percent.HasValue ? (ushort)Math.Clamp(Math.Round(percent.Value * 100), 0, 10000) : null, CoveringMovement.Stopped);
                });
            });

        public static readonly MatterDeviceKind ContactSensor = BooleanSensor("contact-sensor", "Contact Sensor", DeviceType.ContactSensor, "contact");
        public static readonly MatterDeviceKind WaterLeakDetector = BooleanSensor("water-leak-detector", "Water Leak Detector", DeviceType.WaterLeakDetector, "leak");
        public static readonly MatterDeviceKind RainSensor = BooleanSensor("rain-sensor", "Rain Sensor", DeviceType.RainSensor, "rain");

        public static readonly MatterDeviceKind OccupancySensor = new("occupancy-sensor", "Occupancy Sensor", DeviceType.OccupancySensor,
            new[] { new MatterDataPoint("occupied", MatterValueType.Boolean, MatterPointDirection.ToMatter), Battery },
            b =>
            {
                var cluster = b.Add(new OccupancySensingCluster());
                b.OnSet("occupied", v => cluster.SetOccupied(MatterValues.ToBool(v)));
                b.Battery();
            });

        public static readonly MatterDeviceKind LightSensor = Measurement("light-sensor", "Light Sensor", DeviceType.LightSensor, "illuminance", MeasurementCluster.Illuminance);
        public static readonly MatterDeviceKind TemperatureSensor = Measurement("temperature-sensor", "Temperature Sensor", DeviceType.TemperatureSensor, "temperature", MeasurementCluster.Temperature);
        public static readonly MatterDeviceKind HumiditySensor = Measurement("humidity-sensor", "Humidity Sensor", DeviceType.HumiditySensor, "humidity", MeasurementCluster.Humidity);
        public static readonly MatterDeviceKind PressureSensor = Measurement("pressure-sensor", "Pressure Sensor", DeviceType.PressureSensor, "pressure", MeasurementCluster.Pressure);
        public static readonly MatterDeviceKind FlowSensor = Measurement("flow-sensor", "Flow Sensor", DeviceType.FlowSensor, "flow", MeasurementCluster.Flow);

        public static readonly MatterDeviceKind AirQualitySensor = new("air-quality-sensor", "Air Quality Sensor", DeviceType.AirQualitySensor,
            new[]
            {
                new MatterDataPoint("air-quality", MatterValueType.Integer, MatterPointDirection.ToMatter),
                new MatterDataPoint("co2", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                new MatterDataPoint("pm25", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                new MatterDataPoint("pm10", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                new MatterDataPoint("tvoc", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                new MatterDataPoint("temperature", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                new MatterDataPoint("humidity", MatterValueType.Double, MatterPointDirection.ToMatter, true),
                Battery
            },
            b =>
            {
                var quality = b.Add(new AirQualityCluster());
                b.OnSet("air-quality", v => quality.SetQuality(MatterValues.ToInt(v)));
                Concentration(b, "co2", ConcentrationMeasurementCluster.CarbonDioxideId, ConcentrationUnit.Ppm);
                Concentration(b, "pm25", ConcentrationMeasurementCluster.Pm25Id, ConcentrationUnit.MicrogramsPerCubicMeter);
                Concentration(b, "pm10", ConcentrationMeasurementCluster.Pm10Id, ConcentrationUnit.MicrogramsPerCubicMeter);
                Concentration(b, "tvoc", ConcentrationMeasurementCluster.TvocId, ConcentrationUnit.Ppb);
                if (b.Has("temperature"))
                {
                    var t = b.Add(MeasurementCluster.Temperature());
                    b.OnSet("temperature", v => t.SetValue(MatterValues.ToNullableDouble(v)));
                }

                if (b.Has("humidity"))
                {
                    var h = b.Add(MeasurementCluster.Humidity());
                    b.OnSet("humidity", v => h.SetValue(MatterValues.ToNullableDouble(v)));
                }

                b.Battery();
            });

        public static readonly MatterDeviceKind SmokeCoAlarm = new("smoke-co-alarm", "Smoke/CO Alarm", DeviceType.SmokeCoAlarm,
            new[]
            {
                new MatterDataPoint("smoke", MatterValueType.Boolean, MatterPointDirection.ToMatter),
                new MatterDataPoint("co", MatterValueType.Boolean, MatterPointDirection.ToMatter, true),
                Battery
            },
            b =>
            {
                var smoke = b.Has("smoke") || !b.Has("co");
                var alarm = b.Add(new SmokeCoAlarmCluster(smoke, b.Has("co")));
                b.OnSet("smoke", v => alarm.SetSmoke(MatterValues.ToBool(v) ? 2 : 0));
                b.OnSet("co", v => alarm.SetCo(MatterValues.ToBool(v) ? 2 : 0));
                b.Battery();
            });

        public static readonly MatterDeviceKind Thermostat = ThermostatKind("thermostat", "Thermostat", false);
        public static readonly MatterDeviceKind HeatingCoolingThermostat = ThermostatKind("thermostat-heat-cool", "Thermostat (heating & cooling)", true);

        public static readonly MatterDeviceKind Fan = new("fan", "Fan", DeviceType.Fan,
            new[] { new MatterDataPoint("speed", MatterValueType.Integer, MatterPointDirection.Both) },
            b =>
            {
                var fan = b.Add(new FanControlCluster());
                fan.SpeedRequested = percent => _ = b.Request("speed", percent);
                b.OnSet("speed", v => fan.SetSpeed(MatterValues.ToInt(v)));
            });

        public static readonly MatterDeviceKind DoorLock = new("door-lock", "Door Lock", DeviceType.DoorLock,
            new[] { new MatterDataPoint("locked", MatterValueType.Boolean, MatterPointDirection.Both) },
            b =>
            {
                var door = b.Add(new DoorLockCluster());
                door.LockRequested = locked => b.Request("locked", locked);
                b.OnSet("locked", v => door.SetLocked(v == null ? null : MatterValues.ToBool(v)));
            });

        public static readonly MatterDeviceKind WaterValve = new("water-valve", "Water Valve", DeviceType.WaterValve,
            new[] { new MatterDataPoint("open", MatterValueType.Boolean, MatterPointDirection.Both) },
            b =>
            {
                var valve = b.Add(new ValveCluster());
                valve.OpenRequested = open => b.Request("open", open);
                b.OnSet("open", v => valve.SetOpen(MatterValues.ToBool(v)));
            });

        public static IReadOnlyList<MatterDeviceKind> All { get; } = new[]
        {
            OnOffLight, DimmableLight, ColorTemperatureLight, ExtendedColorLight, OnOffPlugInUnit, DimmablePlugInUnit, WindowCovering,
            ContactSensor, WaterLeakDetector, RainSensor, OccupancySensor, LightSensor, TemperatureSensor, HumiditySensor, PressureSensor,
            FlowSensor, AirQualitySensor, SmokeCoAlarm, Thermostat, HeatingCoolingThermostat, Fan, DoorLock, WaterValve
        };

        public static MatterDeviceKind Get(string key) => All.FirstOrDefault(k => k.Key == key);

        private static MatterDataPoint OnOffPoint() => new("on-off", MatterValueType.Boolean, MatterPointDirection.Both);

        private static MatterDataPoint LevelPoint() => new("level", MatterValueType.Integer, MatterPointDirection.Both);

        private static void Lighting(DeviceBuilder b, bool dimmable, bool plug)
        {
            b.Add(new GroupsCluster());
            var onOff = b.Add(new OnOffCluster());
            onOff.OnOffRequested = on => b.Request("on-off", on);
            b.OnSet("on-off", v => onOff.SetOnOff(MatterValues.ToBool(v)));
            if (!dimmable)
            {
                return;
            }

            var level = b.Add(new LevelControlCluster());
            level.LevelRequested = async (value, withOnOff) =>
            {
                if (withOnOff && value <= LevelControlCluster.MinLevel)
                {
                    await b.Request("on-off", false);
                    return;
                }

                await b.Request("level", Math.Max(1, LevelControlCluster.LevelToPercent(value)));
                if (withOnOff && !onOff.OnOff)
                {
                    await b.Request("on-off", true);
                }
            };
            b.OnSet("level", v =>
            {
                var percent = MatterValues.ToInt(v);
                if (percent > 0)
                {
                    level.SetLevel(LevelControlCluster.PercentToLevel(percent));
                }
            });
        }

        private static void Color(DeviceBuilder b, bool fullColor)
        {
            var color = b.Add(new ColorControlCluster(fullColor));
            color.ColorRequested = rgb => b.Has("color") ? b.Request("color", rgb) : Task.CompletedTask;
            color.ColorTemperatureRequested = kelvin => b.Has("color-temperature") ? b.Request("color-temperature", kelvin) : Task.CompletedTask;
            b.OnSet("color", v => color.SetRgb(v?.ToString()));
            b.OnSet("color-temperature", v =>
            {
                var kelvin = MatterValues.ToInt(v);
                if (kelvin > 0)
                {
                    color.SetColorTemperature(kelvin);
                }
            });
        }

        private static MatterDeviceKind BooleanSensor(string key, string name, DeviceType type, string point) => new(key, name, type,
            new[] { new MatterDataPoint(point, MatterValueType.Boolean, MatterPointDirection.ToMatter), Battery },
            b =>
            {
                var cluster = b.Add(new BooleanStateCluster());
                b.OnSet(point, v => cluster.SetState(MatterValues.ToBool(v)));
                b.Battery();
            });

        private static MatterDeviceKind Measurement(string key, string name, DeviceType type, string point, Func<MeasurementCluster> create) => new(key, name, type,
            new[] { new MatterDataPoint(point, MatterValueType.Double, MatterPointDirection.ToMatter), Battery },
            b =>
            {
                var cluster = b.Add(create());
                b.OnSet(point, v => cluster.SetValue(MatterValues.ToNullableDouble(v)));
                b.Battery();
            });

        private static void Concentration(DeviceBuilder b, string point, uint clusterId, ConcentrationUnit unit)
        {
            if (!b.Has(point))
            {
                return;
            }

            var cluster = b.Add(new ConcentrationMeasurementCluster(clusterId, unit));
            b.OnSet(point, v => cluster.SetValue(MatterValues.ToNullableDouble(v)));
        }

        private static MatterDeviceKind ThermostatKind(string key, string name, bool cooling)
        {
            var points = new List<MatterDataPoint>
            {
                new("local-temperature", MatterValueType.Double, MatterPointDirection.ToMatter),
                new("heating-setpoint", MatterValueType.Double, MatterPointDirection.Both)
            };
            if (cooling)
            {
                points.Add(new MatterDataPoint("cooling-setpoint", MatterValueType.Double, MatterPointDirection.Both));
            }

            points.Add(new MatterDataPoint("system-mode", MatterValueType.Integer, MatterPointDirection.Both, true));
            points.Add(new MatterDataPoint("heating-active", MatterValueType.Boolean, MatterPointDirection.ToMatter, true));
            if (cooling)
            {
                points.Add(new MatterDataPoint("cooling-active", MatterValueType.Boolean, MatterPointDirection.ToMatter, true));
            }

            return new MatterDeviceKind(key, name, DeviceType.Thermostat, points.ToArray(), b =>
            {
                var thermostat = b.Add(new ThermostatCluster(cooling));
                thermostat.SetpointRequested = (isCooling, celsius) => _ = b.Request(isCooling ? "cooling-setpoint" : "heating-setpoint", celsius);
                thermostat.ModeRequested = mode => _ = b.Request("system-mode", (int)mode);
                b.OnSet("local-temperature", v => thermostat.SetLocalTemperature(MatterValues.ToNullableDouble(v)));
                b.OnSet("heating-setpoint", v => thermostat.SetHeatingSetpoint(MatterValues.ToDouble(v)));
                b.OnSet("cooling-setpoint", v => thermostat.SetCoolingSetpoint(MatterValues.ToDouble(v)));
                b.OnSet("system-mode", v => thermostat.SetSystemMode((ThermostatSystemMode)MatterValues.ToInt(v)));
                b.OnSet("heating-active", v => thermostat.SetRunning(MatterValues.ToBool(v), thermostat.IsCoolingActive));
                b.OnSet("cooling-active", v => thermostat.SetRunning(thermostat.IsHeatingActive, MatterValues.ToBool(v)));
            });
        }
    }

    /// <summary>
    /// A bridged device built from a <see cref="MatterDeviceKind"/>. The host pushes values with <see cref="SetValue"/>
    /// and receives controller requests through <see cref="ValueRequested"/>.
    /// </summary>
    public sealed class BridgedGenericDevice : BridgedDevice
    {
        private readonly Dictionary<string, Action<object>> _setters = new();
        private readonly Dictionary<string, object> _values = new();
        private readonly HashSet<string> _enabled;
        private bool _built;

        /// <param name="enabledPoints">Data points the host provides; null = all points of the kind.</param>
        public BridgedGenericDevice(string uniqueId, string name, MatterDeviceKind kind, IEnumerable<string> enabledPoints = null) : base(uniqueId, name)
        {
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            _enabled = (enabledPoints ?? kind.Points.Select(p => p.Key)).ToHashSet();
            foreach (var mandatory in kind.Points.Where(p => !p.Optional))
            {
                _enabled.Add(mandatory.Key);
            }
        }

        public MatterDeviceKind Kind { get; }

        public IReadOnlyCollection<string> EnabledPoints => _enabled;

        /// <summary>Called with the data point key and the value a controller requested.</summary>
        public Func<string, object, Task> ValueRequested { get; set; }

        internal override DeviceType DeviceType => Kind.DeviceType;

        protected override string ProductName => Kind.DisplayName;

        internal override void AddClusters(Endpoint endpoint)
        {
            AddCommonClusters(endpoint);
            Kind.Build(new DeviceBuilder(endpoint, this, _enabled));
            _built = true;
            List<KeyValuePair<string, object>> pending;
            lock (_values)
            {
                pending = _values.ToList();
            }

            foreach (var (point, value) in pending)
            {
                if (_setters.TryGetValue(point, out var setter))
                {
                    setter(value);
                }
            }
        }

        internal void RegisterSetter(string point, Action<object> setter) => _setters[point] = setter;

        internal Task RequestAsync(string point, object value)
        {
            var handler = ValueRequested;
            return handler == null || !_enabled.Contains(point) ? Task.CompletedTask : handler(point, value);
        }

        /// <summary>Pushes a value of a data point to the controllers.</summary>
        public void SetValue(string point, object value)
        {
            var definition = Kind.GetPoint(point);
            if (definition == null || !definition.Direction.HasFlag(MatterPointDirection.ToMatter))
            {
                return;
            }

            lock (_values)
            {
                _values[point] = value;
            }

            if (_built && _setters.TryGetValue(point, out var setter))
            {
                Locked(() => setter(value));
            }
        }
    }
}
