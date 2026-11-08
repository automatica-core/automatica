using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Lightning.Common
{
    /// <summary>
    /// The interface template ids every timed switch has
    /// </summary>
    public sealed record TimedSwitchIds(Guid On, Guid Off, Guid Output, Guid OnDuration, Guid WarningTime, Guid WarningDuration, PresenceSimulationIds PresenceSimulation);

    /// <summary>
    /// A light that is switched on for a time (or permanently) with a switch-off pre-warning: <see cref="WarningTime"/> seconds before
    /// the end the output goes to 0 for <see cref="WarningDuration"/> (parameter in milliseconds) (a short blink), then on again until the end.
    /// Base of the stairwell light switch and the comfort switch; also brings the presence simulation.
    /// </summary>
    public abstract class TimedSwitchLogic : ClockedLogic
    {
        internal const double DefaultOnDuration = 180;
        internal const double DefaultWarningTime = 15;
        /// <summary>milliseconds (a double default like 0.5 is parsed with the server culture by the core)</summary>
        internal const int DefaultWarningDurationMs = 500;

        private readonly TimedSwitchIds _ids;
        private RuleInterfaceInstance _output;

        private bool _on;
        private bool _permanent;
        private DateTime? _offAt;
        private DateTime? _warnAt;
        private DateTime? _warnEndAt;

        protected TimedSwitchLogic(ILogicContext context, TimedSwitchIds ids) : base(context)
        {
            _ids = ids;
            PresenceSimulation = new PresenceSimulationBinding(ids.PresenceSimulation);
        }

        internal PresenceSimulationBinding PresenceSimulation { get; }

        internal double OnDuration { get; private set; } = DefaultOnDuration;
        internal double WarningTime { get; private set; } = DefaultWarningTime;
        internal double WarningDuration { get; private set; } = DefaultWarningDurationMs / 1000.0;

        internal bool IsOn => _on;
        internal bool IsPermanent => _permanent;
        internal DateTime? OffAt => _offAt;

        /// <summary>
        /// The interfaces every timed switch has (inputs On/Off at sort 2/3, the output, the times and the presence simulation)
        /// </summary>
        internal static void CreateCommonTemplates(ILogicTemplateFactory factory, Guid logicGuid, string prefix, TimedSwitchIds ids)
        {
            factory.CreateLogicInterfaceTemplate(ids.On, $"{prefix}.INPUT.ON.NAME", $"{prefix}.INPUT.ON.DESCRIPTION", "on", logicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(ids.Off, $"{prefix}.INPUT.OFF.NAME", $"{prefix}.INPUT.OFF.DESCRIPTION", "off", logicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(ids.Output, $"{prefix}.OUTPUT.NAME", $"{prefix}.OUTPUT.DESCRIPTION", "output", logicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);

            factory.CreateParameterLogicInterfaceTemplate(ids.OnDuration, $"{prefix}.PARAM.ON_DURATION.NAME", $"{prefix}.PARAM.ON_DURATION.DESCRIPTION", "onDuration", logicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOnDuration, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.WarningTime, $"{prefix}.PARAM.WARNING_TIME.NAME", $"{prefix}.PARAM.WARNING_TIME.DESCRIPTION", "warningTime", logicGuid, 10, RuleInterfaceParameterDataType.Double, DefaultWarningTime, false);
            factory.CreateParameterLogicInterfaceTemplate(ids.WarningDuration, $"{prefix}.PARAM.WARNING_DURATION.NAME", $"{prefix}.PARAM.WARNING_DURATION.DESCRIPTION", "warningDuration", logicGuid, 11, RuleInterfaceParameterDataType.Integer, DefaultWarningDurationMs, false);

            PresenceSimulationBinding.CreateTemplates(factory, logicGuid, ids.PresenceSimulation, 100, 100);
        }

        protected override void OnStart()
        {
            _output = Interface(_ids.Output);
            PresenceSimulation.Attach(Context.Dispatcher, Context.RuleInstance.ObjId);
        }

        protected override void ApplyParameter(Guid template, object value)
        {
            if (PresenceSimulation.ApplyParameter(template, value))
            {
                return;
            }
            if (template == _ids.OnDuration)
            {
                OnDuration = Math.Max(LightingValues.ToDouble(value, DefaultOnDuration), 0.1);
            }
            else if (template == _ids.WarningTime)
            {
                WarningTime = Math.Max(LightingValues.ToDouble(value, DefaultWarningTime), 0);
            }
            else if (template == _ids.WarningDuration)
            {
                WarningDuration = Math.Max(LightingValues.ToDouble(value, DefaultWarningDurationMs), 0) / 1000.0;
            }
            else
            {
                ApplyOtherParameter(template, value);
            }
        }

        protected virtual void ApplyOtherParameter(Guid template, object value)
        {
        }

        protected sealed override void OnInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (PresenceSimulation.ApplyInput(template, value))
            {
                return;
            }
            if (template == _ids.On)
            {
                if (LightingValues.IsTruthy(value))
                {
                    SwitchOnPermanent(now);
                }
            }
            else if (template == _ids.Off)
            {
                if (LightingValues.IsTruthy(value))
                {
                    SwitchOff(now);
                }
            }
            else
            {
                OnTrigger(template, value, now);
            }
        }

        /// <summary>
        /// The trigger input(s) of the block
        /// </summary>
        protected abstract void OnTrigger(Guid template, object value, DateTime now);

        /// <summary>
        /// On for <see cref="OnDuration"/> (restarts the time), a permanent light stays permanent
        /// </summary>
        protected void SwitchOnTimed(DateTime now)
        {
            if (_permanent)
            {
                return;
            }
            _on = true;
            _offAt = now.AddSeconds(OnDuration);
            _warnAt = WarningTime > 0 && WarningDuration > 0 && WarningTime < OnDuration ? _offAt.Value.AddSeconds(-WarningTime) : null;
            _warnEndAt = null;
            Changed(now, false);
        }

        protected void SwitchOnPermanent(DateTime now)
        {
            _on = true;
            _permanent = true;
            ClearTimers();
            Changed(now, false);
        }

        protected void SwitchOff(DateTime now, bool simulated = false)
        {
            _on = false;
            _permanent = false;
            ClearTimers();
            Changed(now, simulated);
        }

        private void ClearTimers()
        {
            _offAt = null;
            _warnAt = null;
            _warnEndAt = null;
        }

        private void Changed(DateTime now, bool simulated)
        {
            if (!simulated)
            {
                PresenceSimulation.Record(now, _on);
            }
            Publish();
        }

        private void Publish()
        {
            Output(_output, _on && _warnEndAt == null);
        }

        protected sealed override DateTime? OnTick(DateTime now)
        {
            if (_offAt.HasValue && now >= _offAt.Value)
            {
                SwitchOff(now);
            }
            else
            {
                if (_warnAt.HasValue && now >= _warnAt.Value)
                {
                    _warnAt = null;
                    _warnEndAt = now.AddSeconds(WarningDuration);
                    Publish();
                }
                if (_warnEndAt.HasValue && now >= _warnEndAt.Value)
                {
                    _warnEndAt = null;
                    Publish();
                }
            }

            var step = PresenceSimulation.Step(now);
            if (step.State == true)
            {
                _on = true;
                _permanent = true;
                ClearTimers();
                Changed(now, true);
            }
            else if (step.State == false)
            {
                SwitchOff(now, true);
            }

            var next = Earliest(Earliest(_warnAt, _warnEndAt), Earliest(_offAt, step.Next));
            return Earliest(next, OnOtherTick(now));
        }

        protected virtual DateTime? OnOtherTick(DateTime now) => null;

        protected static DateTime? Earliest(DateTime? a, DateTime? b)
        {
            if (a == null)
            {
                return b;
            }
            if (b == null)
            {
                return a;
            }
            return a < b ? a : b;
        }
    }
}
