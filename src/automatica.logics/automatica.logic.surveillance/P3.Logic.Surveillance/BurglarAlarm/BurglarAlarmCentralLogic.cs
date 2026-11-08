using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using F = P3.Logic.Surveillance.BurglarAlarm.BurglarAlarmCentralLogicFactory;

namespace P3.Logic.Surveillance.BurglarAlarm
{
    public class BurglarAlarmCentralState : SecurityStateBase
    {
    }

    /// <summary>
    /// Fans the commands out to the burglar alarms picked in the "alarms" parameter. It has no alarm logic of its own.
    /// A toggle disarms all when any of them is armed (or arming), otherwise it arms all.
    /// </summary>
    public class BurglarAlarmCentralLogic : SecurityLogicBase<BurglarAlarmCentralState>
    {
        private static readonly string[] Commands =
        {
            BurglarAlarmLogic.CmdArm, BurglarAlarmLogic.CmdArmNoPresence, BurglarAlarmLogic.CmdArmDelayed,
            BurglarAlarmLogic.CmdArmDelayedNoPresence, BurglarAlarmLogic.CmdDisarm, BurglarAlarmLogic.CmdAcknowledge
        };

        private bool _peripheryDisabled;

        public BurglarAlarmCentralLogic(ILogicContext context, ISecurityStateStore store) : base(context, store)
        {
        }

        public override string SecurityKind => "burglar-central";
        public override IReadOnlyCollection<string> SecurityCommands => Commands;

        internal List<BurglarAlarmLogic> Targets()
        {
            var ids = ControlReactions.Ids(Param(F.ParamAlarms));
            if (ids.Count == 0 || Context.ControlContext == null)
            {
                return new List<BurglarAlarmLogic>();
            }

            var controls = Context.ControlContext.GetAsync(ids.ToList()).GetAwaiter().GetResult() ?? new List<IControl>();
            return controls.OfType<BurglarAlarmLogic>().ToList();
        }

        protected override void OnStarted(bool restored)
        {
            UpdateOutputs(Targets(), true);
        }

        protected override bool OnTick(DateTimeOffset now)
        {
            UpdateOutputs(Targets());
            return false;
        }

        private void UpdateOutputs(List<BurglarAlarmLogic> targets, bool force = false)
        {
            SetOutput(F.OutArmedCount, targets.Count(a => a.IsArmed), force);
            SetOutput(F.OutAlarm, targets.Any(a => a.IsAlarm), force);
        }

        protected override bool OnInput(Guid template, object value, IDispatchable source)
        {
            if (template == F.InDisablePeriphery)
            {
                _peripheryDisabled = ToBool(value);
                return false;
            }

            if (!Rising(template, source, value))
            {
                return false;
            }

            var who = SourceName(source);
            string command = null;

            if (template == F.InOff)
            {
                command = BurglarAlarmLogic.CmdDisarm;
            }
            else if (template == F.InConfirm)
            {
                command = BurglarAlarmLogic.CmdAcknowledge;
            }
            else if (!_peripheryDisabled)
            {
                if (template == F.InToggle || template == F.InToggleNoPresence)
                {
                    var anyArmed = Targets().Any(a => a.IsArmed || a.CurrentState.ArmingUntil.HasValue);
                    command = anyArmed ? BurglarAlarmLogic.CmdDisarm
                        : template == F.InToggle ? BurglarAlarmLogic.CmdArm : BurglarAlarmLogic.CmdArmNoPresence;
                }
                else if (template == F.InArm) command = BurglarAlarmLogic.CmdArm;
                else if (template == F.InArmNoPresence) command = BurglarAlarmLogic.CmdArmNoPresence;
                else if (template == F.InArmDelayed) command = BurglarAlarmLogic.CmdArmDelayed;
                else if (template == F.InArmDelayedNoPresence) command = BurglarAlarmLogic.CmdArmDelayedNoPresence;
            }

            if (command == null)
            {
                return false;
            }

            Send(command, who);
            return true;
        }

        private void Send(string command, string who)
        {
            AddLog("central-" + command, who);
            var targets = Targets();
            // the alarms are commanded once this logic's lock is released
            Defer(() =>
            {
                foreach (var alarm in targets)
                {
                    alarm.CentralCommand(command, who);
                }

                lock (Sync)
                {
                    UpdateOutputs(targets);
                }
                Flush();
            });
        }

        protected override SecurityCommandResult OnCommand(string command, IReadOnlyDictionary<string, string> arguments, SecurityCommandContext context)
        {
            if (Targets().Count == 0)
            {
                return SecurityCommandResult.NotPossible;
            }

            Send(command, context.UserName);
            return SecurityCommandResult.Ok;
        }

        protected override object BuildState(bool includeAdminData)
        {
            var targets = Targets();
            var armed = targets.Count(a => a.IsArmed);
            var arming = targets.Count(a => a.CurrentState.ArmingUntil.HasValue);
            var alarms = targets.Count(a => a.IsAlarm);

            string status, text, severity, icon;
            var actions = new List<object>();
            if (alarms > 0)
            {
                status = "alarm";
                text = "SURVEILLANCE.VISU.STATUS.ALARM";
                severity = "alarm";
                icon = "bell";
                actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdAcknowledge, "SURVEILLANCE.VISU.ACKNOWLEDGE_ALL", "primary"));
                actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdDisarm, "SURVEILLANCE.VISU.DISARM_ALL", "danger", true));
            }
            else if (armed > 0 || arming > 0)
            {
                status = armed == targets.Count ? "armed" : arming > 0 ? "arming" : "partly-armed";
                text = armed == targets.Count ? "SURVEILLANCE.VISU.STATUS.ARMED" : arming > 0 ? "SURVEILLANCE.VISU.STATUS.ARMING" : "SURVEILLANCE.VISU.STATUS.PARTLY_ARMED";
                severity = arming > 0 ? "warning" : "info";
                icon = "shield-halved";
                actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdDisarm, "SURVEILLANCE.VISU.DISARM_ALL", "danger", true));
                if (armed < targets.Count && arming == 0)
                {
                    actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdArmDelayed, "SURVEILLANCE.VISU.ARM_DELAYED_ALL", "default"));
                }
            }
            else
            {
                status = "disarmed";
                text = "SURVEILLANCE.VISU.STATUS.DISARMED";
                severity = "ok";
                icon = "shield";
                if (targets.Count > 0)
                {
                    actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdArmDelayed, "SURVEILLANCE.VISU.ARM_DELAYED_ALL", "primary"));
                    actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdArm, "SURVEILLANCE.VISU.ARM_ALL", "default"));
                    actions.Add(BurglarAlarmLogic.Action(BurglarAlarmLogic.CmdArmNoPresence, "SURVEILLANCE.VISU.ARM_NP_ALL", "default"));
                }
            }

            var countdown = targets.Select(a => a.CurrentState.ArmingUntil).Where(a => a.HasValue).OrderByDescending(a => a).FirstOrDefault();
            var details = targets.Select(a => (object)new
            {
                label = a.Name,
                value = a.IsAlarm ? "SURVEILLANCE.VISU.STATUS.ALARM"
                    : a.CurrentState.ArmingUntil.HasValue ? "SURVEILLANCE.VISU.STATUS.ARMING"
                    : a.IsArmed ? "SURVEILLANCE.VISU.STATUS.ARMED" : "SURVEILLANCE.VISU.STATUS.DISARMED",
                translate = true
            }).ToList();

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
