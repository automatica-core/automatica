using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.BurglarAlarm.BurglarAlarmLogicFactory;

namespace P3.Logic.Surveillance.BurglarAlarm
{
    public enum BurglarArmMode
    {
        Disarmed = 0,
        /// <summary>
        /// Presence (motion) sensors are active
        /// </summary>
        WithPresence = 1,
        WithoutPresence = 2
    }

    public class BurglarAlarmState : SecurityStateBase
    {
        public BurglarArmMode Mode { get; set; }

        /// <summary>
        /// Delayed arming in progress until this time, with the mode it arms into
        /// </summary>
        public DateTimeOffset? ArmingUntil { get; set; }
        public BurglarArmMode ArmingMode { get; set; }

        public DateTimeOffset? AlarmStart { get; set; }
        /// <summary>
        /// The maximum alarm duration ran out: only the silent alarm stays on
        /// </summary>
        public bool MaxReached { get; set; }
        public bool TestAlarm { get; set; }
        public string Cause { get; set; }
        public DateTimeOffset? CauseTime { get; set; }

        /// <summary>
        /// Window/door sources that were open when the alarm was armed with "arm open contacts": they are ignored until
        /// they change
        /// </summary>
        public List<Guid> IgnoredContacts { get; set; } = new();

        /// <summary>
        /// Distinct presence sensors seen so far (the second-presence rule needs more than one)
        /// </summary>
        public List<Guid> KnownPresenceSensors { get; set; } = new();

        public bool AlarmActive => AlarmStart.HasValue;
    }

    /// <summary>
    /// Burglar alarm modelled on the Loxone "Burglar Alarm" block, see <see cref="BurglarAlarmLogicFactory"/>.
    /// The state (armed, arming, alarm, cause, log) is kept across restarts.
    /// </summary>
    public class BurglarAlarmLogic : SecurityLogicBase<BurglarAlarmState>
    {
        public const int DefaultArmingDelay = 600;

        public const string CmdArm = "arm";
        public const string CmdArmNoPresence = "arm-np";
        public const string CmdArmDelayed = "arm-delayed";
        public const string CmdArmDelayedNoPresence = "arm-delayed-np";
        public const string CmdDisarm = "disarm";
        public const string CmdAcknowledge = "acknowledge";

        private static readonly string[] Commands = { CmdArm, CmdArmNoPresence, CmdArmDelayed, CmdArmDelayedNoPresence, CmdDisarm, CmdAcknowledge };

        private static readonly Guid[] LevelOutputs = { F.OutSilent, F.OutAudible, F.OutVisual, F.OutInternal, F.OutExternal, F.OutRemote };
        private static readonly Guid[] LevelDelays = { F.ParamSilentDelay, F.ParamAudibleDelay, F.ParamVisualDelay, F.ParamInternalDelay, F.ParamExternalDelay, F.ParamRemoteDelay };
        private static readonly double[] LevelDefaults = { 0, 20, 40, 90, 150, 300 };
        private static readonly string[] LevelNames = { "silent", "audible", "visual", "internal", "external", "remote" };

        private sealed class Sensor
        {
            public Guid Template;
            public string Name;
            public bool Active;
            public DateTimeOffset? ReleasedAt;
        }

        private readonly Dictionary<Guid, Sensor> _sensors = new();
        private bool _locked;
        private bool _peripheryDisabled;
        private DateTimeOffset _startedAt;
        private bool _restoredArmed;
        private DateTimeOffset? _firstPresenceAt;
        private Guid? _firstPresenceSource;
        private bool _lightsOn;

        public BurglarAlarmLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "burglar";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        internal BurglarAlarmState CurrentState => State;

        private static bool IsContact(Guid template) => template == F.InWindow || template == F.InDoor;

        private static bool IsSensor(Guid template) =>
            template == F.InPresence || template == F.InGlassBreakage || template == F.InWindow || template == F.InDoor || template == F.InOther;

        #region lifecycle

        protected override void OnStarted(bool restored)
        {
            _sensors.Clear();
            _startedAt = Now;
            _restoredArmed = restored && (State.Mode != BurglarArmMode.Disarmed || State.ArmingUntil.HasValue);
            _firstPresenceAt = null;
            _firstPresenceSource = null;
            _lightsOn = false;
            UpdateOutputs(true);
        }

        /// <summary>
        /// After a restart of an armed alarm, the sensors are ignored for a while (values replayed by the drivers)
        /// </summary>
        private bool InStartDelay => _restoredArmed && Now - _startedAt < ParamSeconds(F.ParamStartDelay, 10);

        #endregion

        #region inputs

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (IsSensor(template))
            {
                return OnSensor(template, source, ToBool(value));
            }

            if (template == F.InDisablePeriphery)
            {
                _peripheryDisabled = ToBool(value);
                return false;
            }

            if (template == F.InOff)
            {
                var on = ToBool(value);
                var rising = Rising(template, source, value);
                _locked = on;
                if (rising)
                {
                    var changed = Disarm(SourceName(source) ?? "Off");
                    UpdateOutputs();
                    return changed;
                }
                UpdateOutputs();
                return false;
            }

            if (!Rising(template, source, value))
            {
                return false;
            }

            var who = SourceName(source);
            var result = false;

            if (template == F.InConfirm)
            {
                result = Acknowledge(who);
            }
            else if (!_peripheryDisabled)
            {
                if (template == F.InToggle || template == F.InToggleNoPresence)
                {
                    result = IsArmedOrArming
                        ? Disarm(who)
                        : Arm(template == F.InToggle ? BurglarArmMode.WithPresence : BurglarArmMode.WithoutPresence, false, who);
                }
                else if (template == F.InArm)
                {
                    result = Arm(BurglarArmMode.WithPresence, false, who);
                }
                else if (template == F.InArmNoPresence)
                {
                    result = Arm(BurglarArmMode.WithoutPresence, false, who);
                }
                else if (template == F.InArmDelayed)
                {
                    result = Arm(BurglarArmMode.WithPresence, true, who);
                }
                else if (template == F.InArmDelayedNoPresence)
                {
                    result = Arm(BurglarArmMode.WithoutPresence, true, who);
                }
            }

            UpdateOutputs();
            return result;
        }

        private bool OnSensor(Guid template, IDispatchable source, bool active)
        {
            var id = source?.Id ?? template;
            if (!_sensors.TryGetValue(id, out var sensor))
            {
                sensor = new Sensor { Template = template, Name = SourceName(source) };
                _sensors[id] = sensor;
            }

            var wasActive = sensor.Active;
            sensor.Active = active;
            sensor.Name = SourceName(source) ?? sensor.Name;
            if (wasActive && !active)
            {
                sensor.ReleasedAt = Now;
            }

            var changed = false;

            if (template == F.InPresence && !State.KnownPresenceSensors.Contains(id))
            {
                State.KnownPresenceSensors.Add(id);
                changed = true;
            }

            // an ignored contact counts again once it changed
            if (IsContact(template) && wasActive != active && State.IgnoredContacts.Remove(id))
            {
                changed = true;
            }

            if (active && State.Mode != BurglarArmMode.Disarmed && !InStartDelay && !State.IgnoredContacts.Contains(id))
            {
                changed |= EvaluateTrigger(template, id, sensor.Name);
            }

            UpdateOutputs();
            return changed;
        }

        private bool EvaluateTrigger(Guid template, Guid sourceId, string name)
        {
            if (template == F.InPresence)
            {
                if (State.Mode != BurglarArmMode.WithPresence)
                {
                    return false;
                }

                // with more than one presence sensor, a second different one must confirm within the window
                var window = ParamSeconds(F.ParamSecondPresence, 900);
                if (window > TimeSpan.Zero && State.KnownPresenceSensors.Count > 1)
                {
                    if (_firstPresenceAt == null || Now - _firstPresenceAt.Value > window || _firstPresenceSource == sourceId)
                    {
                        _firstPresenceAt = Now;
                        _firstPresenceSource = sourceId;
                        return false;
                    }
                }
            }

            return Trigger(name ?? DefaultSensorName(template));
        }

        private static string DefaultSensorName(Guid template)
        {
            if (template == F.InPresence) return "P";
            if (template == F.InGlassBreakage) return "Gb";
            if (template == F.InWindow) return "Wc";
            if (template == F.InDoor) return "Dc";
            return "Ot";
        }

        #endregion

        #region state changes

        private bool IsArmedOrArming => State.Mode != BurglarArmMode.Disarmed || State.ArmingUntil.HasValue;

        private bool Arm(BurglarArmMode mode, bool delayed, string who)
        {
            if (_locked)
            {
                return false;
            }

            if (State.Mode == mode && !State.ArmingUntil.HasValue)
            {
                return false;
            }

            var delay = ParamSeconds(F.ParamArmingDelay, DefaultArmingDelay);
            AnnounceOpenContacts();

            if (delayed && delay > TimeSpan.Zero)
            {
                State.ArmingUntil = Now + delay;
                State.ArmingMode = mode;
                AddLog("arming", who);
                return true;
            }

            CompleteArming(mode, who);
            return true;
        }

        private void CompleteArming(BurglarArmMode mode, string who)
        {
            State.ArmingUntil = null;
            State.Mode = mode;
            State.IgnoredContacts.Clear();
            _firstPresenceAt = null;
            _firstPresenceSource = null;
            _restoredArmed = false;
            AddLog(mode == BurglarArmMode.WithPresence ? "armed" : "armed-np", who);

            var ignoreOpen = ParamBool(F.ParamArmOpenContacts);
            foreach (var (id, sensor) in _sensors.Where(a => a.Value.Active).ToList())
            {
                if (IsContact(sensor.Template) && ignoreOpen)
                {
                    State.IgnoredContacts.Add(id);
                    continue;
                }

                if (sensor.Template == F.InPresence && mode != BurglarArmMode.WithPresence)
                {
                    continue;
                }

                // something is open or moving at the moment of arming
                Trigger(sensor.Name ?? DefaultSensorName(sensor.Template));
            }
        }

        private bool Trigger(string cause)
        {
            State.Cause = cause;
            State.CauseTime = Now;

            if (State.AlarmActive)
            {
                AddLog("alarm", cause);
                return true;
            }

            State.AlarmStart = Now;
            State.MaxReached = false;
            State.TestAlarm = ParamBool(F.ParamTestMode);
            AddLog(State.TestAlarm ? "alarm-test" : "alarm", cause);

            if (!State.TestAlarm)
            {
                Notify("SURVEILLANCE.NOTIFY.BURGLAR_ALARM", cause, NotificationSeverity.Error);
                ControlReactions.ApplyAsync(Context, ControlReactions.Ids(Param(F.ParamBlinds)), ControlReaction.BlindsUp);
            }

            return true;
        }

        private bool Acknowledge(string who)
        {
            if (!State.AlarmActive)
            {
                return false;
            }

            EndAlarm();
            AddLog("acknowledged", who);
            return true;
        }

        private bool Disarm(string who)
        {
            if (!IsArmedOrArming && !State.AlarmActive)
            {
                return false;
            }

            EndAlarm();
            State.Mode = BurglarArmMode.Disarmed;
            State.ArmingUntil = null;
            State.IgnoredContacts.Clear();
            _restoredArmed = false;
            AddLog("disarmed", who);
            return true;
        }

        private void EndAlarm()
        {
            State.AlarmStart = null;
            State.MaxReached = false;
            State.TestAlarm = false;
        }

        #endregion

        #region tick and outputs

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;

            if (State.ArmingUntil is { } until && now >= until)
            {
                CompleteArming(State.ArmingMode, null);
                changed = true;
            }

            if (State.AlarmStart is { } start && !State.MaxReached)
            {
                var max = ParamSeconds(F.ParamMaxAlarm, 900);
                if (max > TimeSpan.Zero && now - start >= max)
                {
                    if (ParamBool(F.ParamSilentConfirm))
                    {
                        EndAlarm();
                        AddLog("auto-acknowledged");
                    }
                    else
                    {
                        State.MaxReached = true;
                        AddLog("max-alarm");
                    }
                    changed = true;
                }
            }

            UpdateOutputs();
            return changed;
        }

        private bool LevelOn(int level)
        {
            if (State.AlarmStart is not { } start || State.TestAlarm)
            {
                return false;
            }

            if (State.MaxReached && level > 0)
            {
                return false;
            }

            return Now - start >= ParamSeconds(LevelDelays[level], LevelDefaults[level]);
        }

        private int ActiveSensorCount()
        {
            var extension = ParamSeconds(F.ParamPulseExtension, 0);
            return _sensors.Values.Count(a => a.Active || (a.ReleasedAt is { } released && Now - released < extension));
        }

        private List<Sensor> OpenContacts() => _sensors.Values.Where(a => a.Active && IsContact(a.Template)).ToList();

        private void AnnounceOpenContacts()
        {
            var open = OpenContacts();
            if (open.Count == 0)
            {
                return;
            }

            var text = open.Count == 1
                ? string.Format(Translate("SURVEILLANCE.BURGLAR_ALARM.TEXT.ONE_OPEN", "Attention, {0} is still open!"), open[0].Name ?? "?")
                : Translate("SURVEILLANCE.BURGLAR_ALARM.TEXT.SEVERAL_OPEN", "Attention! Several windows or doors are still open!");
            SetOutput(F.OutOpenContactsText, text, true);
        }

        private string Translate(string key, string fallback)
        {
            try
            {
                var text = Context.LocalizationProvider?.GetTranslation(key);
                return string.IsNullOrEmpty(text) || text == key ? fallback : text;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private int CurrentLevel()
        {
            var level = 0;
            for (var i = 0; i < LevelOutputs.Length; i++)
            {
                if (LevelOn(i))
                {
                    level = i + 1;
                }
            }
            return level;
        }

        private void UpdateOutputs(bool force = false)
        {
            SetOutput(F.OutStatus, (int)State.Mode, force);
            SetOutput(F.OutAlarm, State.AlarmActive && !State.TestAlarm, force);
            for (var i = 0; i < LevelOutputs.Length; i++)
            {
                SetOutput(LevelOutputs[i], LevelOn(i), force);
            }
            SetOutput(F.OutTest, State.AlarmActive && State.TestAlarm, force);
            SetOutput(F.OutActiveSensors, ActiveSensorCount(), force);
            SetOutput(F.OutArmingRemaining, State.ArmingUntil is { } until ? Math.Max(0, (int)Math.Ceiling((until - Now).TotalSeconds)) : 0, force);
            SetOutput(F.OutCause, State.Cause ?? "", force);
            SetOutput(F.OutTime, State.CauseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "", force);
            SetOutput(F.OutOpenContacts, OpenContacts().Count > 0, force);

            // visual alarm: lights on while it lasts
            var visual = LevelOn(2);
            if (visual != _lightsOn)
            {
                _lightsOn = visual;
                ControlReactions.ApplyAsync(Context, ControlReactions.Ids(Param(F.ParamLights)), visual ? ControlReaction.LightsOn : ControlReaction.LightsOff);
            }
        }

        #endregion

        #region central

        internal bool IsArmed => State.Mode != BurglarArmMode.Disarmed;

        internal bool IsAlarm => State.AlarmActive;

        /// <summary>
        /// A command of the burglar alarm central. Not blocked by the "disable periphery" input.
        /// </summary>
        internal void CentralCommand(string command, string who)
        {
            lock (Sync)
            {
                if (!Started)
                {
                    return;
                }

                if (ApplyCommand(command, who))
                {
                    Persist();
                }
                UpdateOutputs();
            }

            Flush();
        }

        private bool ApplyCommand(string command, string who)
        {
            switch (command)
            {
                case CmdArm:
                    return Arm(BurglarArmMode.WithPresence, false, who);
                case CmdArmNoPresence:
                    return Arm(BurglarArmMode.WithoutPresence, false, who);
                case CmdArmDelayed:
                    return Arm(BurglarArmMode.WithPresence, true, who);
                case CmdArmDelayedNoPresence:
                    return Arm(BurglarArmMode.WithoutPresence, true, who);
                case CmdDisarm:
                    return Disarm(who);
                case CmdAcknowledge:
                    return Acknowledge(who);
                default:
                    return false;
            }
        }

        #endregion

        #region visualization

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            if (_locked && command != CmdDisarm && command != CmdAcknowledge)
            {
                return SecurityCommandResult.NotPossible;
            }

            var result = ApplyCommand(command, context.UserName) ? SecurityCommandResult.Ok : SecurityCommandResult.NotPossible;
            UpdateOutputs();
            return result;
        }

        protected override object BuildState(bool includeAdminData)
        {
            string status, text, severity, icon;
            var actions = new List<object>();

            if (State.AlarmActive)
            {
                status = State.TestAlarm ? "test" : "alarm";
                text = State.TestAlarm ? "SURVEILLANCE.VISU.STATUS.ALARM_TEST" : "SURVEILLANCE.VISU.STATUS.ALARM";
                severity = "alarm";
                icon = "bell";
                actions.Add(Action(CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE", "primary"));
                actions.Add(Action(CmdDisarm, "SURVEILLANCE.VISU.DISARM", "danger", true));
            }
            else if (State.ArmingUntil.HasValue)
            {
                status = "arming";
                text = "SURVEILLANCE.VISU.STATUS.ARMING";
                severity = "warning";
                icon = "hourglass-half";
                actions.Add(Action(CmdDisarm, "SURVEILLANCE.VISU.CANCEL", "default"));
            }
            else if (State.Mode != BurglarArmMode.Disarmed)
            {
                status = "armed";
                text = State.Mode == BurglarArmMode.WithPresence ? "SURVEILLANCE.VISU.STATUS.ARMED" : "SURVEILLANCE.VISU.STATUS.ARMED_NP";
                severity = "info";
                icon = "shield-halved";
                actions.Add(Action(CmdDisarm, "SURVEILLANCE.VISU.DISARM", "danger", true));
            }
            else
            {
                status = "disarmed";
                text = "SURVEILLANCE.VISU.STATUS.DISARMED";
                severity = "ok";
                icon = "shield";
                if (!_locked)
                {
                    actions.Add(Action(CmdArmDelayed, "SURVEILLANCE.VISU.ARM_DELAYED", "primary"));
                    actions.Add(Action(CmdArmDelayedNoPresence, "SURVEILLANCE.VISU.ARM_DELAYED_NP", "default"));
                    actions.Add(Action(CmdArm, "SURVEILLANCE.VISU.ARM", "default"));
                    actions.Add(Action(CmdArmNoPresence, "SURVEILLANCE.VISU.ARM_NP", "default"));
                }
            }

            var details = new List<object>();
            if (State.AlarmActive)
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.LEVEL", value = "SURVEILLANCE.VISU.LEVEL." + LevelNameOf(CurrentLevel()), translate = true });
            }
            if (!string.IsNullOrEmpty(State.Cause))
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.CAUSE", value = State.Cause, translate = false });
            }
            var open = OpenContacts();
            if (open.Count > 0)
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.OPEN", value = string.Join(", ", open.Select(a => a.Name ?? "?")), translate = false });
            }
            if (_locked)
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.LOCKED", value = "SURVEILLANCE.VISU.YES", translate = true });
            }

            return new
            {
                status,
                statusText = text,
                severity,
                icon,
                countdownUntil = State.ArmingUntil,
                level = CurrentLevel(),
                details,
                actions,
                log = LogForVisu()
            };
        }

        private static string LevelNameOf(int level) => level == 0 ? "NONE" : LevelNames[level - 1].ToUpperInvariant();

        internal static object Action(string command, string label, string style, bool confirm = false) => new { command, label, style, confirm };

        #endregion
    }
}
