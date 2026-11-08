using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.FireWaterAlarm.FireWaterAlarmLogicFactory;

namespace P3.Logic.Surveillance.FireWaterAlarm
{
    public class FireWaterAlarmState : SecurityStateBase
    {
        public DateTimeOffset? AlarmStart { get; set; }
        public bool MainAlarm { get; set; }
        /// <summary>
        /// Only the pre-alarm stays (maximum alarm duration without pre-alarm confirmation)
        /// </summary>
        public bool MainEnded { get; set; }
        public bool SignalsSilenced { get; set; }
        public bool TestAlarm { get; set; }
        public string Cause { get; set; }
        public DateTimeOffset? CauseTime { get; set; }

        /// <summary>
        /// "fire" or "water", from the sensor that started the alarm
        /// </summary>
        public string AlarmKind { get; set; }

        /// <summary>
        /// Service mode started from the visualization, until this time
        /// </summary>
        public DateTimeOffset? ServiceUntil { get; set; }

        /// <summary>
        /// Service mode from the parameter (seconds &gt; 1): it started at this time
        /// </summary>
        public DateTimeOffset? ParameterServiceStart { get; set; }
        public int ParameterServiceSeconds { get; set; }
    }

    /// <summary>
    /// Fire and water alarm modelled on the Loxone "Fire and Water Alarm", see <see cref="FireWaterAlarmLogicFactory"/>.
    /// It is always armed (there is no disarm, only the service mode); the state survives restarts.
    /// </summary>
    public class FireWaterAlarmLogic : SecurityLogicBase<FireWaterAlarmState>
    {
        public const string CmdAcknowledge = "acknowledge";
        public const string CmdSilence = "silence";
        public const string CmdServiceStart = "service-start";
        public const string CmdServiceStop = "service-stop";

        private static readonly string[] Commands = { CmdAcknowledge, CmdSilence, CmdServiceStart, CmdServiceStop };

        private sealed class Sensor
        {
            public Guid Template;
            public string Name;
            public bool Active;
        }

        private readonly Dictionary<Guid, Sensor> _sensors = new();
        private bool _locked;

        public FireWaterAlarmLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "fire";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        internal FireWaterAlarmState CurrentState => State;

        protected override void OnStarted(bool restored)
        {
            _sensors.Clear();
            SyncParameterService();
            UpdateOutputs(true);
        }

        protected override void OnParameterChanged(Guid template)
        {
            if (template == F.ParamServiceMode)
            {
                SyncParameterService();
                Persist();
            }
            UpdateOutputs();
        }

        /// <summary>
        /// Service mode parameter: 0 = off, 1 = permanently on, &gt; 1 = on for that many seconds from when it was set
        /// </summary>
        private void SyncParameterService()
        {
            var seconds = ParamInt(F.ParamServiceMode, 0);
            if (seconds > 1)
            {
                if (State.ParameterServiceSeconds != seconds || State.ParameterServiceStart == null)
                {
                    State.ParameterServiceStart = Now;
                    State.ParameterServiceSeconds = seconds;
                }
            }
            else
            {
                State.ParameterServiceStart = null;
                State.ParameterServiceSeconds = 0;
            }
        }

        internal bool ServiceActive
        {
            get
            {
                if (ParamInt(F.ParamServiceMode, 0) == 1)
                {
                    return true;
                }
                if (State.ParameterServiceStart is { } start && Now - start < TimeSpan.FromSeconds(State.ParameterServiceSeconds))
                {
                    return true;
                }
                return State.ServiceUntil is { } until && Now < until;
            }
        }

        private bool IsSensorInput(Guid template) =>
            template == F.InSmoke || template == F.InWater || template == F.InArcFault || template == F.InTemperature;

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (IsSensorInput(template))
            {
                var active = template == F.InTemperature
                    ? ToDouble(value, double.MinValue) >= ParamDouble(F.ParamMaxTemperature, 43)
                    : ToBool(value);
                return OnSensor(template, source, active);
            }

            if (template == F.InOff)
            {
                _locked = ToBool(value);
                var changed = Rising(template, source, value) && Reset("acknowledged", SourceName(source) ?? "Off");
                UpdateOutputs();
                return changed;
            }

            if (!Rising(template, source, value))
            {
                return false;
            }

            var result = false;
            if (template == F.InConfirm)
            {
                result = Reset("acknowledged", SourceName(source));
            }
            else if (template == F.InConfirmSignals)
            {
                result = Silence(SourceName(source));
            }

            UpdateOutputs();
            return result;
        }

        private bool OnSensor(Guid template, IDispatchable source, bool active)
        {
            var id = source?.Id ?? template;
            if (!_sensors.TryGetValue(id, out var sensor))
            {
                sensor = new Sensor { Template = template };
                _sensors[id] = sensor;
            }

            var rising = active && !sensor.Active;
            sensor.Active = active;
            sensor.Name = SourceName(source) ?? sensor.Name ?? DefaultName(template);

            var changed = false;
            if (rising && !_locked)
            {
                changed = Trigger(sensor);
            }

            UpdateOutputs();
            return changed;
        }

        private static string DefaultName(Guid template)
        {
            if (template == F.InSmoke) return "S";
            if (template == F.InWater) return "W";
            if (template == F.InArcFault) return "F";
            return "T";
        }

        private static string EventOf(Guid template) => template == F.InWater ? "water" : "fire";

        private bool Trigger(Sensor sensor)
        {
            State.Cause = sensor.Name;
            State.CauseTime = Now;

            if (ServiceActive)
            {
                State.TestAlarm = true;
                AddLog("alarm-test", sensor.Name);
                return true;
            }

            if (State.AlarmStart.HasValue)
            {
                AddLog(EventOf(sensor.Template), sensor.Name);
                return true;
            }

            State.AlarmStart = Now;
            State.MainAlarm = false;
            State.MainEnded = false;
            State.SignalsSilenced = false;
            State.AlarmKind = EventOf(sensor.Template);
            AddLog(EventOf(sensor.Template), sensor.Name);
            Notify(sensor.Template == F.InWater ? "SURVEILLANCE.NOTIFY.WATER_ALARM" : "SURVEILLANCE.NOTIFY.FIRE_ALARM", sensor.Name, NotificationSeverity.Error);

            if (sensor.Template != F.InWater)
            {
                // fire: light up and open the blinds (escape routes)
                ControlReactions.ApplyAsync(Context, ControlReactions.Ids(Param(F.ParamLights)), ControlReaction.LightsOn);
                ControlReactions.ApplyAsync(Context, ControlReactions.Ids(Param(F.ParamBlinds)), ControlReaction.BlindsUp);
            }
            return true;
        }

        private bool Silence(string who)
        {
            if (!State.AlarmStart.HasValue || State.SignalsSilenced)
            {
                return false;
            }

            State.SignalsSilenced = true;
            AddLog("silenced", who);
            return true;
        }

        private bool Reset(string eventKey, string who)
        {
            if (!State.AlarmStart.HasValue && !State.TestAlarm)
            {
                return false;
            }

            State.AlarmStart = null;
            State.MainAlarm = false;
            State.MainEnded = false;
            State.SignalsSilenced = false;
            State.TestAlarm = false;
            AddLog(eventKey, who);
            return true;
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;
            if (State.AlarmStart is { } start)
            {
                if (!State.MainAlarm && !State.MainEnded && now - start >= ParamSeconds(F.ParamMainDelay, 120))
                {
                    State.MainAlarm = true;
                    // the main alarm sounds again, even if the pre-alarm signal was silenced
                    State.SignalsSilenced = false;
                    AddLog("main-alarm", State.Cause);
                    changed = true;
                }

                var max = ParamSeconds(F.ParamMaxAlarm, 300);
                if (max > TimeSpan.Zero && now - start >= max && !State.MainEnded)
                {
                    if (ParamBool(F.ParamPreAlarmConfirm))
                    {
                        Reset("auto-acknowledged", null);
                    }
                    else
                    {
                        State.MainAlarm = false;
                        State.MainEnded = true;
                        AddLog("max-alarm");
                    }
                    changed = true;
                }
            }

            if (State.ServiceUntil is { } until && now >= until)
            {
                State.ServiceUntil = null;
                AddLog("service-end");
                changed = true;
            }

            UpdateOutputs();
            return changed;
        }

        private void UpdateOutputs(bool force = false)
        {
            var pre = State.AlarmStart.HasValue;
            var main = pre && State.MainAlarm;
            SetOutput(F.OutPreAlarm, pre, force);
            SetOutput(F.OutMainAlarm, main, force);
            SetOutput(F.OutPreAlarmSignal, pre && !State.SignalsSilenced, force);
            SetOutput(F.OutMainAlarmSignal, main && !State.SignalsSilenced, force);
            SetOutput(F.OutActiveSensors, _sensors.Values.Count(a => a.Active), force);
            SetOutput(F.OutTest, State.TestAlarm, force);
            SetOutput(F.OutCause, State.Cause ?? "", force);
            SetOutput(F.OutTime, State.CauseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "", force);
        }

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            bool ok;
            switch (command)
            {
                case CmdAcknowledge:
                    ok = Reset("acknowledged", context.UserName);
                    break;
                case CmdSilence:
                    ok = Silence(context.UserName);
                    break;
                case CmdServiceStart:
                    State.ServiceUntil = Now + ParamSeconds(F.ParamServiceDuration, 1800);
                    AddLog("service-start", context.UserName);
                    ok = true;
                    break;
                case CmdServiceStop:
                    ok = State.ServiceUntil.HasValue;
                    if (ok)
                    {
                        State.ServiceUntil = null;
                        AddLog("service-end", context.UserName);
                    }
                    break;
                default:
                    ok = false;
                    break;
            }

            UpdateOutputs();
            return ok ? SecurityCommandResult.Ok : SecurityCommandResult.NotPossible;
        }

        protected override object BuildState(bool includeAdminData)
        {
            string status, text, severity, icon;
            var actions = new List<object>();
            var water = State.AlarmKind == "water";

            if (State.AlarmStart.HasValue)
            {
                status = State.MainAlarm ? "main-alarm" : "pre-alarm";
                text = State.MainAlarm ? "SURVEILLANCE.VISU.STATUS.MAIN_ALARM" : "SURVEILLANCE.VISU.STATUS.PRE_ALARM";
                severity = "alarm";
                icon = water ? "droplet" : "fire";
                if (!State.SignalsSilenced)
                {
                    actions.Add(BurglarAlarmLogic.Action(CmdSilence, "SURVEILLANCE.VISU.SILENCE", "default"));
                }
                actions.Add(BurglarAlarmLogic.Action(CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE", "primary"));
            }
            else if (State.TestAlarm)
            {
                status = "test";
                text = "SURVEILLANCE.VISU.STATUS.ALARM_TEST";
                severity = "warning";
                icon = "vial";
                actions.Add(BurglarAlarmLogic.Action(CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE", "primary"));
            }
            else if (ServiceActive)
            {
                status = "service";
                text = "SURVEILLANCE.VISU.STATUS.SERVICE";
                severity = "warning";
                icon = "screwdriver-wrench";
                if (State.ServiceUntil.HasValue)
                {
                    actions.Add(BurglarAlarmLogic.Action(CmdServiceStop, "SURVEILLANCE.VISU.SERVICE_STOP", "primary"));
                }
            }
            else
            {
                status = "ready";
                text = "SURVEILLANCE.VISU.STATUS.READY";
                severity = "ok";
                icon = "fire-extinguisher";
                actions.Add(BurglarAlarmLogic.Action(CmdServiceStart, "SURVEILLANCE.VISU.SERVICE_START", "default", true));
            }

            var details = new List<object>();
            if (!string.IsNullOrEmpty(State.Cause))
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.CAUSE", value = State.Cause, translate = false });
            }
            var active = _sensors.Values.Where(a => a.Active).Select(a => a.Name).ToList();
            if (active.Count > 0)
            {
                details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.ACTIVE_SENSORS", value = string.Join(", ", active), translate = false });
            }

            DateTimeOffset? countdown = null;
            if (State.AlarmStart is { } start && !State.MainAlarm && !State.MainEnded)
            {
                countdown = start + ParamSeconds(F.ParamMainDelay, 120);
            }
            else if (!State.AlarmStart.HasValue && State.ServiceUntil.HasValue)
            {
                countdown = State.ServiceUntil;
            }

            return new
            {
                status,
                statusText = text,
                severity,
                icon,
                countdownUntil = countdown,
                details,
                actions,
                log = LogForVisu()
            };
        }
    }
}
