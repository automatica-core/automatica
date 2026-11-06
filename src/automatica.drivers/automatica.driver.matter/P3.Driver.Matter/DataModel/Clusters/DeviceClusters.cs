using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel.Clusters
{
    public sealed class BridgedDeviceBasicInformationCluster : ClusterServer
    {
        public const uint Id = 0x0039;
        public const uint AttributeNodeLabel = 0x05;
        public const uint AttributeReachable = 0x11;

        private string _nodeLabel;
        private bool _reachable = true;

        public BridgedDeviceBasicInformationCluster(string uniqueId, string nodeLabel, string vendorName, string productName) : base(Id, 3)
        {
            _nodeLabel = Truncate(nodeLabel);
            Attribute(0x01, (w, t, _) => w.PutString(t, Truncate(vendorName)));
            Attribute(0x03, (w, t, _) => w.PutString(t, Truncate(productName)));
            Attribute(AttributeNodeLabel, (w, t, _) => w.PutString(t, _nodeLabel), (v, _, _) =>
            {
                var label = v.AsString();
                if (label.Length > 32)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                _nodeLabel = label;
            }, writePrivilege: AclPrivilege.Manage);
            Attribute(AttributeReachable, (w, t, _) => w.PutBool(t, _reachable));
            Attribute(0x12, (w, t, _) => w.PutString(t, uniqueId.Length > 32 ? uniqueId[..32] : uniqueId));
        }

        private static string Truncate(string s) => string.IsNullOrEmpty(s) ? "" : s.Length > 32 ? s[..32] : s;

        public void SetNodeLabel(string label)
        {
            label = Truncate(label);
            if (label != _nodeLabel)
            {
                _nodeLabel = label;
                MarkChanged(AttributeNodeLabel);
            }
        }

        public void SetReachable(bool reachable)
        {
            if (reachable != _reachable)
            {
                _reachable = reachable;
                MarkChanged(AttributeReachable);
            }
        }
    }

    public sealed class IdentifyCluster : ClusterServer
    {
        public const uint Id = 0x0003;
        private ushort _identifyTime;
        private CancellationTokenSource _timer;

        public IdentifyCluster() : base(Id, 4)
        {
            Attribute(0x00, (w, t, _) => w.PutUInt(t, _identifyTime), (v, _, _) => Start((ushort)v.AsUInt(), false));
            Attribute(0x01, (w, t, _) => w.PutUInt(t, 0));
            Command(0x00, (_, f) =>
            {
                Start((ushort)f.GetUInt(0), true);
                return InvokeResult.SuccessTask();
            });
            Command(0x40, (_, _) => InvokeResult.SuccessTask());
        }

        public event Action<ushort> IdentifyRequested;

        private void Start(ushort seconds, bool notify)
        {
            _timer?.Cancel();
            _identifyTime = seconds;
            if (notify)
            {
                MarkChanged(0x00);
            }

            IdentifyRequested?.Invoke(seconds);
            if (seconds == 0)
            {
                return;
            }

            var cts = _timer = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(seconds), cts.Token);
                    _identifyTime = 0;
                    MarkChanged(0x00);
                }
                catch (OperationCanceledException)
                {
                    // restarted
                }
            });
        }
    }

    public sealed class GroupsCluster : ClusterServer
    {
        public const uint Id = 0x0004;

        public GroupsCluster() : base(Id, 4)
        {
            Attribute(0x00, (w, t, _) => w.PutUInt(t, 0));
            Command(0x00, (_, f) => GroupStatus(0x00, StatusCode.ResourceExhausted, f.GetUInt(0)), 0x00, AclPrivilege.Manage);
            Command(0x01, (_, f) => Task.FromResult(InvokeResult.Response(0x01, w =>
            {
                w.PutUInt(0, (byte)StatusCode.NotFound);
                w.PutUInt(1, f.GetUInt(0));
                w.PutString(2, "");
            })), 0x01);
            Command(0x02, (_, _) => Task.FromResult(InvokeResult.Response(0x02, w =>
            {
                w.PutUInt(0, 0);
                w.StartArray(1);
                w.EndContainer();
            })), 0x02);
            Command(0x03, (_, f) => GroupStatus(0x03, StatusCode.NotFound, f.GetUInt(0)), 0x03, AclPrivilege.Manage);
            Command(0x04, (_, _) => InvokeResult.SuccessTask(), privilege: AclPrivilege.Manage);
            Command(0x05, (_, _) => Task.FromResult(InvokeResult.Error(StatusCode.ResourceExhausted)), privilege: AclPrivilege.Manage);
        }

        private static Task<InvokeResult> GroupStatus(uint responseId, StatusCode status, ulong groupId) =>
            Task.FromResult(InvokeResult.Response(responseId, w =>
            {
                w.PutUInt(0, (byte)status);
                w.PutUInt(1, groupId);
            }));
    }

    public sealed class OnOffCluster : ClusterServer
    {
        public const uint Id = 0x0006;
        public const uint AttributeOnOff = 0x0000;

        private bool _onOff;
        private ushort _onTime;
        private ushort _offWaitTime;
        private byte? _startUpOnOff;

        public OnOffCluster() : base(Id, 6)
        {
            FeatureMap = 0x01;
            Attribute(AttributeOnOff, (w, t, _) => w.PutBool(t, _onOff));
            Attribute(0x4000, (w, t, _) => w.PutBool(t, true));
            Attribute(0x4001, (w, t, _) => w.PutUInt(t, _onTime), (v, _, _) => _onTime = (ushort)v.AsUInt());
            Attribute(0x4002, (w, t, _) => w.PutUInt(t, _offWaitTime), (v, _, _) => _offWaitTime = (ushort)v.AsUInt());
            Attribute(0x4003, (w, t, _) => PutNullableUInt(w, t, _startUpOnOff), (v, _, _) =>
            {
                if (v.IsNull)
                {
                    _startUpOnOff = null;
                    return;
                }

                var value = (byte)v.AsUInt();
                if (value > 2)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                _startUpOnOff = value;
            }, writePrivilege: AclPrivilege.Manage);

            Command(0x00, (_, _) => Request(false));
            Command(0x01, (_, _) => Request(true));
            Command(0x02, (_, _) => Request(!_onOff));
            Command(0x40, (_, _) => Request(false));
            Command(0x41, (_, _) => Request(true));
            Command(0x42, (_, f) => (f.GetUInt(0) & 0x01) != 0 && !_onOff ? InvokeResult.SuccessTask() : Request(true));
        }

        public bool OnOff => _onOff;

        /// <summary>Invoked when a controller switches the device; the state is updated when it completes.</summary>
        public Func<bool, Task> OnOffRequested { get; set; }

        private async Task<InvokeResult> Request(bool on)
        {
            if (OnOffRequested != null)
            {
                await OnOffRequested(on);
            }

            SetOnOff(on);
            return InvokeResult.Success();
        }

        public void SetOnOff(bool on)
        {
            if (on != _onOff)
            {
                _onOff = on;
                MarkChanged(AttributeOnOff);
            }
        }
    }

    public sealed class LevelControlCluster : ClusterServer
    {
        public const uint Id = 0x0008;
        public const uint AttributeCurrentLevel = 0x0000;
        public const byte MinLevel = 1;
        public const byte MaxLevel = 254;

        private byte? _currentLevel = MaxLevel;
        private byte _options;
        private ushort _onOffTransitionTime;
        private byte? _onLevel;
        private byte? _startUpLevel;

        public LevelControlCluster() : base(Id, 5)
        {
            FeatureMap = 0x03;
            Attribute(AttributeCurrentLevel, (w, t, _) => PutNullableUInt(w, t, _currentLevel));
            Attribute(0x0001, (w, t, _) => w.PutUInt(t, 0));
            Attribute(0x0002, (w, t, _) => w.PutUInt(t, MinLevel));
            Attribute(0x0003, (w, t, _) => w.PutUInt(t, MaxLevel));
            Attribute(0x000F, (w, t, _) => w.PutUInt(t, _options), (v, _, _) => _options = (byte)(v.AsUInt() & 0x03));
            Attribute(0x0010, (w, t, _) => w.PutUInt(t, _onOffTransitionTime), (v, _, _) => _onOffTransitionTime = (ushort)v.AsUInt());
            Attribute(0x0011, (w, t, _) => PutNullableUInt(w, t, _onLevel), (v, _, _) => _onLevel = v.IsNull ? null : ClampLevel(v.AsUInt()));
            Attribute(0x4000, (w, t, _) => PutNullableUInt(w, t, _startUpLevel), (v, _, _) => _startUpLevel = v.IsNull ? null : (byte)v.AsUInt(),
                writePrivilege: AclPrivilege.Manage);

            Command(0x00, (_, f) => MoveToLevel(ClampLevel(f.GetUInt(0)), false, f));
            Command(0x01, (_, f) => Move(f.GetUInt(0) == 0, false, f));
            Command(0x02, (_, f) => Step(f.GetUInt(0) == 0, (byte)f.GetUInt(1), false, f));
            Command(0x03, (_, _) => InvokeResult.SuccessTask());
            Command(0x04, (_, f) => MoveToLevel(ClampLevel(f.GetUInt(0)), true, f));
            Command(0x05, (_, f) => Move(f.GetUInt(0) == 0, true, f));
            Command(0x06, (_, f) => Step(f.GetUInt(0) == 0, (byte)f.GetUInt(1), true, f));
            Command(0x07, (_, _) => InvokeResult.SuccessTask());
        }

        public byte? CurrentLevel => _currentLevel;

        /// <summary>Invoked with the target level (1..254) and whether the OnOff state follows the level.</summary>
        public Func<byte, bool, Task> LevelRequested { get; set; }

        private static byte ClampLevel(ulong level) => (byte)Math.Clamp(level, MinLevel, MaxLevel);

        private bool IsOff => Endpoint?.GetCluster<OnOffCluster>() is { OnOff: false };

        private bool ShouldExecute(bool withOnOff, TlvElement f)
        {
            if (withOnOff || !IsOff)
            {
                return true;
            }

            var mask = (byte)(f.GetUIntOrNull(2) ?? 0);
            var overrideValue = (byte)(f.GetUIntOrNull(3) ?? 0);
            var options = (byte)((_options & ~mask) | (overrideValue & mask));
            return (options & 0x01) != 0;
        }

        private async Task<InvokeResult> MoveToLevel(byte level, bool withOnOff, TlvElement f)
        {
            if (!ShouldExecute(withOnOff, f))
            {
                return InvokeResult.Success();
            }

            if (LevelRequested != null)
            {
                await LevelRequested(level, withOnOff);
            }

            SetLevel(level);
            if (withOnOff)
            {
                Endpoint?.GetCluster<OnOffCluster>()?.SetOnOff(level > MinLevel);
            }

            return InvokeResult.Success();
        }

        private Task<InvokeResult> Move(bool up, bool withOnOff, TlvElement f)
        {
            return MoveToLevel(up ? MaxLevel : MinLevel, withOnOff, f);
        }

        private Task<InvokeResult> Step(bool up, byte stepSize, bool withOnOff, TlvElement f)
        {
            var current = _currentLevel ?? MinLevel;
            var target = up ? Math.Min(MaxLevel, current + stepSize) : Math.Max(MinLevel, current - stepSize);
            return MoveToLevel((byte)target, withOnOff, f);
        }

        public void SetLevel(byte level)
        {
            level = ClampLevel(level);
            if (level != _currentLevel)
            {
                _currentLevel = level;
                MarkChanged(AttributeCurrentLevel);
            }
        }

        public static byte PercentToLevel(int percent) => (byte)Math.Clamp((int)Math.Round(percent * MaxLevel / 100.0), MinLevel, MaxLevel);

        public static int LevelToPercent(byte level) => (int)Math.Clamp(Math.Round(level * 100.0 / MaxLevel), 0, 100);
    }

    public enum CoveringMovement : byte
    {
        Stopped = 0,
        Opening = 1,
        Closing = 2
    }

    public enum CoveringCommand
    {
        Open,
        Close,
        Stop,
        GoToPosition
    }

    public sealed class WindowCoveringCluster : ClusterServer
    {
        public const uint Id = 0x0102;
        public const uint AttributeOperationalStatus = 0x000A;
        public const uint AttributeTargetLift = 0x000B;
        public const uint AttributeCurrentLift = 0x000E;
        public const uint AttributeCurrentLiftPercentage = 0x0008;

        private ushort? _current;
        private ushort? _target;
        private CoveringMovement _movement;
        private byte _mode;

        public WindowCoveringCluster() : base(Id, 5)
        {
            FeatureMap = 0x01 | 0x04;
            Attribute(0x0000, (w, t, _) => w.PutUInt(t, 0));
            Attribute(0x0007, (w, t, _) => w.PutUInt(t, 0x01 | 0x08));
            Attribute(AttributeCurrentLiftPercentage, (w, t, _) => PutNullableUInt(w, t, _current.HasValue ? (ulong)(_current.Value / 100) : null));
            Attribute(AttributeOperationalStatus, (w, t, _) => w.PutUInt(t, (byte)((byte)_movement | ((byte)_movement << 2))));
            Attribute(AttributeTargetLift, (w, t, _) => PutNullableUInt(w, t, _target));
            Attribute(0x000D, (w, t, _) => w.PutUInt(t, 0));
            Attribute(AttributeCurrentLift, (w, t, _) => PutNullableUInt(w, t, _current));
            Attribute(0x0017, (w, t, _) => w.PutUInt(t, _mode), (v, _, _) => _mode = (byte)(v.AsUInt() & 0x0F), writePrivilege: AclPrivilege.Manage);

            Command(0x00, (_, _) => Request(CoveringCommand.Open, 0));
            Command(0x01, (_, _) => Request(CoveringCommand.Close, 10000));
            Command(0x02, (_, _) => Request(CoveringCommand.Stop, null));
            Command(0x05, (_, f) =>
            {
                var value = f.GetUInt(0);
                if (value > 10000)
                {
                    throw new MatterStatusException(StatusCode.ConstraintError);
                }

                return Request(CoveringCommand.GoToPosition, (ushort)value);
            });
        }

        /// <summary>Invoked with the command and the target in 1/100 percent (0 = open, 10000 = closed).</summary>
        public Func<CoveringCommand, ushort?, Task> CommandRequested { get; set; }

        private async Task<InvokeResult> Request(CoveringCommand command, ushort? target)
        {
            if (CommandRequested != null)
            {
                await CommandRequested(command, target);
            }

            if (command == CoveringCommand.Stop)
            {
                SetTarget(_current);
            }
            else
            {
                SetTarget(target);
            }

            return InvokeResult.Success();
        }

        private void SetTarget(ushort? target)
        {
            if (target != _target)
            {
                _target = target;
                MarkChanged(AttributeTargetLift);
            }
        }

        /// <summary>Updates the reported state (position in 1/100 percent, 0 = fully open).</summary>
        public void SetState(ushort? position, CoveringMovement movement)
        {
            if (position != _current)
            {
                var oldPercent = _current / 100;
                _current = position;
                MarkChanged(AttributeCurrentLift);
                if (oldPercent != position / 100)
                {
                    MarkChanged(AttributeCurrentLiftPercentage);
                }
            }

            if (movement != _movement)
            {
                _movement = movement;
                MarkChanged(AttributeOperationalStatus);
                if (movement == CoveringMovement.Stopped)
                {
                    SetTarget(_current);
                }
            }
        }
    }
}
