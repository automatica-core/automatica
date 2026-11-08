using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.BurglarAlarm;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.AlarmChain.AlarmChainLogicFactory;

namespace P3.Logic.Surveillance.AlarmChain
{
    public class AlarmChainState : SecurityStateBase
    {
        public DateTimeOffset? Start { get; set; }
        /// <summary>
        /// 1..stages while the chain runs, -1 urgent (all stages), 0 idle
        /// </summary>
        public int Stage { get; set; }
        public DateTimeOffset? StageSince { get; set; }
        public int Repetition { get; set; }
        public bool Emergency { get; set; }
        /// <summary>
        /// The emergency output was switched on by the AEs input (not by the repetitions)
        /// </summary>
        public bool EmergencyFromInput { get; set; }
        public DateTimeOffset? Stop { get; set; }
        public string ConfirmCause { get; set; }
        public DateTimeOffset? ConfirmTime { get; set; }
    }

    /// <summary>
    /// Alarm chain modelled on the Loxone "Alarm Chain", see <see cref="AlarmChainLogicFactory"/>. Stage n carries the alarm
    /// text for one reaction time and hands over to the next (the previous text output goes back to empty). After the last
    /// stage the chain repeats; after the maximum repetitions the emergency services output gets the emergency text.
    /// Deviations from Loxone: the number of stages is a parameter (Loxone skips unconnected outputs, which a logic cannot
    /// see), and one alarm text template serves all stages.
    /// </summary>
    public class AlarmChainLogic : SecurityLogicBase<AlarmChainState>
    {
        public const string DefaultAlarmText = "<vn>: alarm <vt1>";
        public const string DefaultEmergencyText = "<vn>: alarm <vt1> - nobody reacted";

        public const string CmdAcknowledge = "acknowledge";
        private static readonly string[] Commands = { CmdAcknowledge };

        private static readonly Regex Placeholder = new("<(vn|vt[123]|v[123](\\.\\d)?|vton|vcn|vca|vtes)>", RegexOptions.Compiled);

        private readonly object[] _texts = new object[3];
        private bool _alarm;
        private bool _urgent;
        private bool _emergencyInput;
        private bool _confirmHeld;
        private bool _offHeld;

        /// <summary>
        /// Ca or Off held: the chain cannot start
        /// </summary>
        private bool Locked => _confirmHeld || _offHeld;

        public AlarmChainLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "alarm-chain";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        private int Stages => Math.Clamp(ParamInt(F.ParamStages, 3), 1, F.MaxStages);
        private TimeSpan ReactionTime => ParamSeconds(F.ParamReactionTime, 60);
        private bool Running => State.Start.HasValue;

        protected override void OnStarted(bool restored)
        {
            UpdateOutputs(true);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (template == F.InText1 || template == F.InText2 || template == F.InText3)
            {
                _texts[template == F.InText1 ? 0 : template == F.InText2 ? 1 : 2] = value;
                if (Running)
                {
                    // the active texts follow the values
                    UpdateOutputs(true);
                }
                return false;
            }

            var who = SourceName(source);
            var on = ToBool(value);
            var changed = false;

            if (template == F.InOff || template == F.InConfirm)
            {
                var rising = Rising(template, source, value);
                if (template == F.InConfirm)
                {
                    _confirmHeld = on;
                }
                else
                {
                    _offHeld = on;
                }
                if (rising)
                {
                    changed = Confirm(who ?? (template == F.InOff ? "Off" : "Ca"));
                }
            }
            else if (template == F.InAlarm)
            {
                var was = _alarm;
                _alarm = on;
                if (on && !was)
                {
                    changed = StartChain(false);
                }
                else if (!on && was && !_urgent)
                {
                    changed = StopChain();
                }
            }
            else if (template == F.InUrgent)
            {
                var was = _urgent;
                _urgent = on;
                if (on && !was)
                {
                    changed = StartChain(true);
                }
                else if (!on && was)
                {
                    changed = _alarm ? StartChain(false) : StopChain();
                }
            }
            else if (template == F.InEmergency)
            {
                var was = _emergencyInput;
                _emergencyInput = on;
                if (on && !was && !Locked)
                {
                    if (!Running)
                    {
                        State.Start = Now;
                        State.Stage = 0;
                        AddLog("alarm-start", who);
                    }
                    State.Emergency = true;
                    State.EmergencyFromInput = true;
                    AddLog("emergency", who);
                    changed = true;
                }
                else if (!on && was && !_alarm && !_urgent && State.EmergencyFromInput)
                {
                    changed = StopChain();
                }
            }

            UpdateOutputs();
            return changed;
        }

        private bool StartChain(bool urgent)
        {
            if (Locked)
            {
                return false;
            }

            // a running chain keeps its stage (e.g. the alarm input replayed after a restart)
            if (Running && (urgent ? State.Stage == -1 : State.Stage > 0 || State.Emergency))
            {
                return false;
            }

            if (!Running)
            {
                State.Start = Now;
                State.Repetition = 0;
                State.Emergency = false;
                State.EmergencyFromInput = false;
                AddLog("alarm-start");
            }

            State.Stage = urgent ? -1 : 1;
            State.StageSince = Now;
            UpdateOutputs(true);
            return true;
        }

        private bool StopChain()
        {
            if (!Running)
            {
                return false;
            }

            State.Start = null;
            State.Stage = 0;
            State.StageSince = null;
            State.Repetition = 0;
            State.Emergency = false;
            State.EmergencyFromInput = false;
            State.Stop = Now;
            AddLog("alarm-stop");
            return true;
        }

        private bool Confirm(string who)
        {
            if (!Running)
            {
                return false;
            }

            StopChain();
            State.ConfirmCause = who ?? "";
            State.ConfirmTime = Now;
            AddLog("acknowledged", who);
            return true;
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            if (!Running || State.StageSince is not { } since)
            {
                return false;
            }

            var rt = ReactionTime;
            if (rt <= TimeSpan.Zero || now - since < rt)
            {
                return false;
            }

            if (State.Stage == -1)
            {
                // urgent: all stages are sent again every reaction time
                State.StageSince = now;
                UpdateOutputs(true);
                return true;
            }

            if (State.Emergency && !State.EmergencyFromInput)
            {
                return false;
            }

            if (State.Stage < Stages)
            {
                State.Stage++;
                State.StageSince = now;
            }
            else
            {
                State.Repetition++;
                var max = ParamInt(F.ParamMaxRepetitions, 4);
                if (max > 0 && State.Repetition >= max)
                {
                    State.Emergency = true;
                    State.StageSince = null;
                    AddLog("emergency");
                }
                else
                {
                    State.Stage = 1;
                    State.StageSince = now;
                }
            }

            UpdateOutputs(true);
            return true;
        }

        private void UpdateOutputs(bool force = false)
        {
            var urgentOrAll = State.Stage == -1 || (Running && ReactionTime <= TimeSpan.Zero && State.Stage > 0);
            var text = Running ? Format(ParamString(F.ParamAlarmText), DefaultAlarmText) : "";
            for (var i = 0; i < F.MaxStages; i++)
            {
                var active = Running && !(State.Emergency && !State.EmergencyFromInput) &&
                             (urgentOrAll ? i < Stages : State.Stage == i + 1);
                var value = active ? text : "";
                // a newly active stage is always sent, so an unchanged text still reaches a caller
                SetOutput(F.OutStages[i], value, force && active);
            }

            SetOutput(F.OutEmergency, Running && State.Emergency ? Format(ParamString(F.ParamEmergencyText), DefaultEmergencyText) : "", force && State.Emergency);
            SetOutput(F.OutStage, Running ? (State.Emergency && State.Stage == 0 ? 0 : State.Stage) : 0, force);
            SetOutput(F.OutStartTime, Time(State.Start), false);
            SetOutput(F.OutStopTime, Time(State.Stop), false);
            SetOutput(F.OutConfirmCause, State.ConfirmCause ?? "", false);
            SetOutput(F.OutConfirmTime, Time(State.ConfirmTime), false);
        }

        private static string Time(DateTimeOffset? time) =>
            time?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";

        internal string Format(string template, string fallback)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                template = fallback;
            }

            return Placeholder.Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                switch (key)
                {
                    case "vn":
                        return Name ?? "";
                    case "vton":
                        return Time(State.Start);
                    case "vcn":
                        return ParamString(F.ParamCustomerName);
                    case "vca":
                        return ParamString(F.ParamCustomerAddress);
                    case "vtes":
                        return SecondsToEmergency().ToString(CultureInfo.InvariantCulture);
                }

                if (key.StartsWith("vt", StringComparison.Ordinal))
                {
                    return Convert.ToString(_texts[key[2] - '1'], CultureInfo.InvariantCulture) ?? "";
                }

                // <v1> no decimals, <v1.2> two decimals
                var index = key[1] - '1';
                var decimals = key.Length > 2 ? key[3] - '0' : 0;
                var number = ToDouble(_texts[index], double.NaN);
                return double.IsNaN(number)
                    ? Convert.ToString(_texts[index], CultureInfo.InvariantCulture) ?? ""
                    : number.ToString("F" + decimals, CultureInfo.InvariantCulture);
            });
        }

        private int SecondsToEmergency()
        {
            var max = ParamInt(F.ParamMaxRepetitions, 4);
            if (!Running || max <= 0 || State.Stage < 1 || State.StageSince is not { } since)
            {
                return 0;
            }

            var rt = ReactionTime.TotalSeconds;
            var remainingSlots = (max - State.Repetition - 1) * Stages + (Stages - State.Stage);
            var inSlot = Math.Max(0, rt - (Now - since).TotalSeconds);
            return (int)Math.Ceiling(remainingSlots * rt + inSlot);
        }

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            var ok = Confirm(context.UserName);
            UpdateOutputs();
            return ok ? SecurityCommandResult.Ok : SecurityCommandResult.NotPossible;
        }

        protected override object BuildState(bool includeAdminData)
        {
            var actions = new List<object>();
            var details = new List<object>();
            if (Running)
            {
                actions.Add(BurglarAlarmLogic.Action(CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE", "primary"));
                details.Add(new
                {
                    label = "SURVEILLANCE.VISU.DETAIL.STAGE",
                    value = State.Emergency ? "SURVEILLANCE.VISU.STAGE_EMERGENCY" : State.Stage == -1 ? "SURVEILLANCE.VISU.STAGE_ALL" : State.Stage.ToString(CultureInfo.InvariantCulture),
                    translate = State.Emergency || State.Stage == -1
                });
            }

            DateTimeOffset? next = Running && State.StageSince is { } since && ReactionTime > TimeSpan.Zero ? since + ReactionTime : null;
            return new
            {
                status = Running ? (State.Emergency ? "emergency" : "alarm") : "ready",
                statusText = Running ? (State.Emergency ? "SURVEILLANCE.VISU.STATUS.EMERGENCY_SERVICES" : "SURVEILLANCE.VISU.STATUS.ALARM") : "SURVEILLANCE.VISU.STATUS.READY",
                severity = Running ? "alarm" : "ok",
                icon = Running ? "bell" : "bell",
                countdownUntil = next,
                details,
                actions,
                log = LogForVisu()
            };
        }
    }
}
