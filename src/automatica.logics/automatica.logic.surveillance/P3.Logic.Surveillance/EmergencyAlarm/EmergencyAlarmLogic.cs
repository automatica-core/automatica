using System;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.EmergencyAlarm.EmergencyAlarmLogicFactory;

namespace P3.Logic.Surveillance.EmergencyAlarm
{
    public class EmergencyAlarmState : SecurityStateBase
    {
        public DateTimeOffset? AlarmStart { get; set; }
        public bool Escalated { get; set; }
        public string Cause { get; set; }
        public string ConfirmCause { get; set; }
    }

    /// <summary>
    /// Emergency (help call) alarm modelled on the Loxone "Emergency Alarm", see <see cref="EmergencyAlarmLogicFactory"/>.
    /// "Tg" is push and hold: held for the alarm time it raises the alarm, held for the confirmation time while the alarm is
    /// on it acknowledges it. Escalation (not in Loxone's block): the "Esc" output turns on when nobody acknowledged in time,
    /// to start e.g. an alarm chain or a caller.
    /// </summary>
    public class EmergencyAlarmLogic : SecurityLogicBase<EmergencyAlarmState>
    {
        public const string CmdActivate = "activate";
        public const string CmdAcknowledge = "acknowledge";

        private static readonly string[] Commands = { CmdActivate, CmdAcknowledge };

        private sealed class Press
        {
            public DateTimeOffset Since;
            public string Name;
            public bool Handled;
        }

        private readonly Dictionary<Guid, Press> _presses = new();
        private bool _locked;

        public EmergencyAlarmLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "emergency";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        protected override TimeSpan? TickInterval => TimeSpan.FromMilliseconds(250);

        internal bool IsAlarm => State.AlarmStart.HasValue;

        protected override void OnStarted(bool restored)
        {
            _presses.Clear();
            UpdateOutputs(true);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            var who = SourceName(source);

            if (template == F.InToggleHold)
            {
                var id = source?.Id ?? template;
                var rising = Rising(template, source, value);
                if (rising)
                {
                    _presses[id] = new Press { Since = Now, Name = who };
                    return false;
                }

                if (!ToBool(value) && _presses.Remove(id, out var press) && !press.Handled)
                {
                    // released: the hold time may have passed between two ticks
                    var changed = EvaluateHold(press, Now);
                    UpdateOutputs();
                    return changed;
                }
                return false;
            }

            if (template == F.InOff)
            {
                _locked = ToBool(value);
                var changed = Rising(template, source, value) && Confirm(who ?? "Off");
                UpdateOutputs();
                return changed;
            }

            if (!Rising(template, source, value))
            {
                return false;
            }

            var result = template == F.InActivate ? Activate(who) : template == F.InConfirm && Confirm(who);
            UpdateOutputs();
            return result;
        }

        private bool EvaluateHold(Press press, DateTimeOffset now)
        {
            var held = now - press.Since;
            if (IsAlarm)
            {
                if (held >= ParamSeconds(F.ParamHoldConfirm, 2))
                {
                    press.Handled = true;
                    return Confirm(press.Name);
                }
            }
            else if (held >= ParamSeconds(F.ParamHoldAlarm, 4))
            {
                press.Handled = true;
                return Activate(press.Name);
            }
            return false;
        }

        private bool Activate(string who)
        {
            if (_locked || IsAlarm)
            {
                return false;
            }

            State.AlarmStart = Now;
            State.Escalated = false;
            State.Cause = who ?? "";
            State.ConfirmCause = "";
            AddLog("emergency", who);
            PulseOutput(F.OutAlarmOn);
            Notify("SURVEILLANCE.NOTIFY.EMERGENCY_ALARM", who, NotificationSeverity.Error);
            return true;
        }

        private bool Confirm(string who)
        {
            if (!IsAlarm)
            {
                return false;
            }

            State.AlarmStart = null;
            State.Escalated = false;
            State.ConfirmCause = who ?? "";
            AddLog("acknowledged", who);
            PulseOutput(F.OutAlarmOff);
            return true;
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            var changed = false;
            foreach (var press in _presses.Values)
            {
                if (!press.Handled)
                {
                    changed |= EvaluateHold(press, now);
                }
            }

            var escalation = ParamSeconds(F.ParamEscalation, 0);
            if (State.AlarmStart is { } start && !State.Escalated && escalation > TimeSpan.Zero && now - start >= escalation)
            {
                State.Escalated = true;
                AddLog("escalated", State.Cause);
                changed = true;
            }

            UpdateOutputs();
            return changed;
        }

        private void UpdateOutputs(bool force = false)
        {
            SetOutput(F.OutAlarm, IsAlarm, force);
            SetOutput(F.OutEscalated, IsAlarm && State.Escalated, force);
            SetOutput(F.OutCause, State.Cause ?? "", force);
            SetOutput(F.OutConfirmCause, State.ConfirmCause ?? "", force);
            if (force)
            {
                SetOutput(F.OutAlarmOn, false, true);
                SetOutput(F.OutAlarmOff, false, true);
            }
        }

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            var ok = command == CmdActivate ? Activate(context.UserName) : Confirm(context.UserName);
            UpdateOutputs();
            return ok ? SecurityCommandResult.Ok : SecurityCommandResult.NotPossible;
        }

        protected override object BuildState(bool includeAdminData)
        {
            var actions = new List<object>();
            var details = new List<object>();
            if (IsAlarm)
            {
                actions.Add(BurglarAlarmLogic.Action(CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE", "primary"));
                if (!string.IsNullOrEmpty(State.Cause))
                {
                    details.Add(new { label = "SURVEILLANCE.VISU.DETAIL.CAUSE", value = State.Cause, translate = false });
                }
            }
            else if (!_locked)
            {
                actions.Add(BurglarAlarmLogic.Action(CmdActivate, "SURVEILLANCE.VISU.CALL_HELP", "danger", true));
            }

            return new
            {
                status = IsAlarm ? (State.Escalated ? "escalated" : "alarm") : "ready",
                statusText = IsAlarm ? (State.Escalated ? "SURVEILLANCE.VISU.STATUS.ESCALATED" : "SURVEILLANCE.VISU.STATUS.EMERGENCY") : "SURVEILLANCE.VISU.STATUS.READY",
                severity = IsAlarm ? "alarm" : "ok",
                icon = IsAlarm ? "truck-medical" : "hand-holding-medical",
                countdownUntil = (DateTimeOffset?)null,
                details,
                actions,
                log = LogForVisu()
            };
        }
    }
}
