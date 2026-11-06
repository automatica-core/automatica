using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel.Clusters
{
    public enum ThermostatSystemMode : byte
    {
        Off = 0,
        Auto = 1,
        Cool = 3,
        Heat = 4
    }

    /// <summary>Thermostat (0x0201) with the heating and optionally the cooling feature. Temperatures are in °C.</summary>
    public sealed class ThermostatCluster : ClusterServer
    {
        public const uint Id = 0x0201;
        public const uint AttributeLocalTemperature = 0x0000;
        public const uint AttributeOccupiedCoolingSetpoint = 0x0011;
        public const uint AttributeOccupiedHeatingSetpoint = 0x0012;
        public const uint AttributeSystemMode = 0x001C;
        public const uint AttributeRunningState = 0x0029;

        private const short MinHeat = 500;
        private const short MaxHeat = 3500;
        private const short MinCool = 1000;
        private const short MaxCool = 3500;

        private readonly bool _cooling;
        private short? _localTemperature;
        private short _heatingSetpoint = 2100;
        private short _coolingSetpoint = 2600;
        private ThermostatSystemMode _systemMode = ThermostatSystemMode.Heat;
        private ushort _runningState;

        public ThermostatCluster(bool cooling) : base(Id, 6)
        {
            _cooling = cooling;
            FeatureMap = 0x01u | (cooling ? 0x02u : 0u);

            Attribute(AttributeLocalTemperature, (w, t, _) => w.PutNullableInt(t, _localTemperature));
            Attribute(0x0003, (w, t, _) => w.PutInt(t, MinHeat));
            Attribute(0x0004, (w, t, _) => w.PutInt(t, MaxHeat));
            Attribute(AttributeOccupiedHeatingSetpoint, (w, t, _) => w.PutInt(t, _heatingSetpoint), (v, _, _) =>
                ApplySetpoint(false, (short)Clamp(v.AsInt(), MinHeat, MaxHeat, true), true));
            Attribute(0x0015, (w, t, _) => w.PutInt(t, MinHeat));
            Attribute(0x0016, (w, t, _) => w.PutInt(t, MaxHeat));
            if (cooling)
            {
                Attribute(0x0005, (w, t, _) => w.PutInt(t, MinCool));
                Attribute(0x0006, (w, t, _) => w.PutInt(t, MaxCool));
                Attribute(AttributeOccupiedCoolingSetpoint, (w, t, _) => w.PutInt(t, _coolingSetpoint), (v, _, _) =>
                    ApplySetpoint(true, (short)Clamp(v.AsInt(), MinCool, MaxCool, true), true));
                Attribute(0x0017, (w, t, _) => w.PutInt(t, MinCool));
                Attribute(0x0018, (w, t, _) => w.PutInt(t, MaxCool));
            }

            // 2 = heating only, 4 = cooling and heating
            Attribute(0x001B, (w, t, _) => w.PutUInt(t, cooling ? 4u : 2u), (v, _, _) =>
            {
                if (v.AsUInt() != (cooling ? 4u : 2u))
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(AttributeSystemMode, (w, t, _) => w.PutUInt(t, (byte)_systemMode), (v, _, _) =>
            {
                var mode = (ThermostatSystemMode)v.AsUInt();
                if (!IsSupported(mode))
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                ApplyMode(mode, true);
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(AttributeRunningState, (w, t, _) => w.PutUInt(t, _runningState));

            Command(0x00, async (_, f) =>
            {
                var mode = f.GetUInt(0);
                var amount = (short)(f.GetInt(1) * 10);
                if (mode is 0 or 2)
                {
                    var target = (short)Math.Clamp(_heatingSetpoint + amount, MinHeat, MaxHeat);
                    ApplySetpoint(false, target, true);
                }

                if (mode is 1 or 2 && _cooling)
                {
                    var target = (short)Math.Clamp(_coolingSetpoint + amount, MinCool, MaxCool);
                    ApplySetpoint(true, target, true);
                }

                await Task.CompletedTask;
                return InvokeResult.Success();
            });
        }

        /// <summary>Raised when a controller changes a setpoint: (cooling, °C).</summary>
        public Action<bool, double> SetpointRequested { get; set; }

        /// <summary>Raised when a controller changes the system mode.</summary>
        public Action<ThermostatSystemMode> ModeRequested { get; set; }

        private bool IsSupported(ThermostatSystemMode mode) => mode switch
        {
            ThermostatSystemMode.Off or ThermostatSystemMode.Heat => true,
            ThermostatSystemMode.Cool => _cooling,
            _ => false
        };

        private static long Clamp(long value, long min, long max, bool strict)
        {
            if (strict && (value < min || value > max))
            {
                throw new MatterStatusException(StatusCode.ConstraintError);
            }

            return Math.Clamp(value, min, max);
        }

        private void ApplySetpoint(bool cooling, short value, bool fromController)
        {
            ref var field = ref cooling ? ref _coolingSetpoint : ref _heatingSetpoint;
            if (field != value)
            {
                field = value;
                MarkChanged(cooling ? AttributeOccupiedCoolingSetpoint : AttributeOccupiedHeatingSetpoint);
            }

            if (fromController)
            {
                SetpointRequested?.Invoke(cooling, value / 100.0);
            }
        }

        private void ApplyMode(ThermostatSystemMode mode, bool fromController)
        {
            if (mode != _systemMode)
            {
                _systemMode = mode;
                MarkChanged(AttributeSystemMode);
            }

            if (fromController)
            {
                ModeRequested?.Invoke(mode);
            }
        }

        public void SetLocalTemperature(double? celsius)
        {
            short? value = celsius.HasValue && !double.IsNaN(celsius.Value) ? (short)Math.Clamp(Math.Round(celsius.Value * 100), -27315, 32767) : null;
            if (value != _localTemperature)
            {
                _localTemperature = value;
                MarkChanged(AttributeLocalTemperature);
            }
        }

        public void SetHeatingSetpoint(double celsius) => ApplySetpoint(false, (short)Math.Clamp(Math.Round(celsius * 100), MinHeat, MaxHeat), false);

        public void SetCoolingSetpoint(double celsius)
        {
            if (_cooling)
            {
                ApplySetpoint(true, (short)Math.Clamp(Math.Round(celsius * 100), MinCool, MaxCool), false);
            }
        }

        public void SetSystemMode(ThermostatSystemMode mode)
        {
            if (IsSupported(mode))
            {
                ApplyMode(mode, false);
            }
        }

        public void SetRunning(bool heating, bool cooling)
        {
            var value = (ushort)((heating ? 0x01 : 0) | (cooling && _cooling ? 0x02 : 0));
            if (value != _runningState)
            {
                _runningState = value;
                MarkChanged(AttributeRunningState);
            }
        }

        public bool IsHeatingActive => (_runningState & 0x01) != 0;
        public bool IsCoolingActive => (_runningState & 0x02) != 0;
    }

    /// <summary>Fan Control (0x0202), fan mode sequence Off/Low/Medium/High plus a percent setting.</summary>
    public sealed class FanControlCluster : ClusterServer
    {
        public const uint Id = 0x0202;
        public const uint AttributeFanMode = 0x0000;
        public const uint AttributePercentSetting = 0x0002;
        public const uint AttributePercentCurrent = 0x0003;

        private byte _mode;
        private byte? _percentSetting = 0;
        private byte _percentCurrent;

        public FanControlCluster() : base(Id, 4)
        {
            Attribute(AttributeFanMode, (w, t, _) => w.PutUInt(t, _mode), (v, _, _) =>
            {
                var mode = (byte)v.AsUInt();
                byte percent = mode switch
                {
                    0 => 0,
                    1 => 33,
                    2 => 66,
                    3 or 4 => 100,
                    5 => _percentSetting is > 0 ? _percentSetting.Value : (byte)100,
                    _ => throw new MatterStatusException(StatusCode.ConstraintError)
                };
                ApplyPercent(percent, true);
            });
            Attribute(0x0001, (w, t, _) => w.PutUInt(t, 0)); // Off/Low/Med/High
            Attribute(AttributePercentSetting, (w, t, _) => w.PutNullableUInt(t, _percentSetting), (v, _, _) =>
            {
                if (v.IsNull)
                {
                    return;
                }

                var p = v.AsUInt();
                if (p > 100)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                ApplyPercent((byte)p, true);
            });
            Attribute(AttributePercentCurrent, (w, t, _) => w.PutUInt(t, _percentCurrent));
        }

        /// <summary>Raised with the requested speed in percent (0 = off).</summary>
        public Action<int> SpeedRequested { get; set; }

        private static byte ModeFor(byte percent) => percent switch
        {
            0 => 0,
            <= 33 => 1,
            <= 66 => 2,
            _ => 3
        };

        private void ApplyPercent(byte percent, bool fromController)
        {
            if (percent != _percentSetting)
            {
                _percentSetting = percent;
                MarkChanged(AttributePercentSetting);
            }

            var mode = ModeFor(percent);
            if (mode != _mode)
            {
                _mode = mode;
                MarkChanged(AttributeFanMode);
            }

            if (fromController)
            {
                SpeedRequested?.Invoke(percent);
            }
        }

        /// <summary>Updates the actual speed (percent) from the device.</summary>
        public void SetSpeed(int percent)
        {
            var p = (byte)Math.Clamp(percent, 0, 100);
            if (p != _percentCurrent)
            {
                _percentCurrent = p;
                MarkChanged(AttributePercentCurrent);
            }

            ApplyPercent(p, false);
        }
    }

    /// <summary>Door Lock (0x0101) without credentials: lock state plus the (timed) lock and unlock commands.</summary>
    public sealed class DoorLockCluster : ClusterServer
    {
        public const uint Id = 0x0101;
        public const uint AttributeLockState = 0x0000;

        private byte? _lockState = 1; // locked
        private byte _operatingMode;

        public DoorLockCluster() : base(Id, 7)
        {
            Attribute(AttributeLockState, (w, t, _) => w.PutNullableUInt(t, _lockState));
            Attribute(0x0001, (w, t, _) => w.PutUInt(t, 0)); // LockType: dead bolt
            Attribute(0x0002, (w, t, _) => w.PutBool(t, true)); // ActuatorEnabled
            Attribute(0x0025, (w, t, _) => w.PutUInt(t, _operatingMode), (v, _, _) =>
            {
                var mode = (byte)v.AsUInt();
                if (mode is not (0 or 3))
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                _operatingMode = mode;
                MarkChanged(0x0025);
            });
            Attribute(0x0026, (w, t, _) => w.PutUInt(t, 0xFFF6)); // normal and no-remote-lock-unlock supported

            Command(0x00, (_, _) => Request(true), timed: true);
            Command(0x01, (_, _) => Request(false), timed: true);
        }

        /// <summary>Invoked with true to lock and false to unlock.</summary>
        public Func<bool, Task> LockRequested { get; set; }

        private async Task<InvokeResult> Request(bool locked)
        {
            if (_operatingMode == 3)
            {
                return InvokeResult.Error(StatusCode.Failure);
            }

            if (LockRequested != null)
            {
                await LockRequested(locked);
            }

            SetLocked(locked);
            return InvokeResult.Success();
        }

        public void SetLocked(bool? locked)
        {
            byte? state = locked switch
            {
                true => 1,
                false => 2,
                null => null // unknown; 0 would mean NotFullyLocked
            };
            if (state != _lockState)
            {
                _lockState = state;
                MarkChanged(AttributeLockState);
            }
        }
    }

    /// <summary>Valve Configuration and Control (0x0081), open/close only.</summary>
    public sealed class ValveCluster : ClusterServer
    {
        public const uint Id = 0x0081;
        public const uint AttributeCurrentState = 0x0004;
        public const uint AttributeTargetState = 0x0005;

        private byte? _current = 0;
        private byte? _target;
        private uint? _defaultOpenDuration;

        public ValveCluster() : base(Id, 1)
        {
            Attribute(0x0000, (w, t, _) => w.PutNullableUInt(t, _current == 1 ? _defaultOpenDuration : null)); // OpenDuration
            Attribute(0x0001, (w, t, _) => w.PutNullableUInt(t, _defaultOpenDuration), (v, _, _) =>
            {
                _defaultOpenDuration = v.IsNull ? null : (uint?)v.AsUInt();
                MarkChanged(0x0001);
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(0x0003, (w, t, _) => w.PutNull(t)); // RemainingDuration
            Attribute(AttributeCurrentState, (w, t, _) => w.PutNullableUInt(t, _current));
            Attribute(AttributeTargetState, (w, t, _) => w.PutNullableUInt(t, _target));

            Command(0x00, (_, _) => Request(true));
            Command(0x01, (_, _) => Request(false));
        }

        /// <summary>Invoked with true to open and false to close.</summary>
        public Func<bool, Task> OpenRequested { get; set; }

        private async Task<InvokeResult> Request(bool open)
        {
            SetTarget(open ? (byte)1 : (byte)0);
            if (OpenRequested != null)
            {
                await OpenRequested(open);
            }

            return InvokeResult.Success();
        }

        private void SetTarget(byte? target)
        {
            if (target != _target)
            {
                _target = target;
                MarkChanged(AttributeTargetState);
            }
        }

        public void SetOpen(bool open)
        {
            byte state = open ? (byte)1 : (byte)0;
            if (state != _current)
            {
                _current = state;
                MarkChanged(AttributeCurrentState);
                MarkChanged(0x0000);
            }

            SetTarget(null);
        }
    }
}
