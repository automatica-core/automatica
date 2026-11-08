using System;
using System.Collections.Generic;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Energy.LoadManager
{
    /// <summary>
    /// Wires the <see cref="LoadManagerEngine"/> to the logic interfaces
    /// </summary>
    public class LoadManagerRule : EnergyLogicBase
    {
        private readonly Dictionary<Guid, (int Load, int Field)> _loadInterfaces = new();

        public LoadManagerRule(ILogicContext context) : base(context)
        {
            for (var load = 1; load <= LoadManagerEngine.LoadCount; load++)
            {
                for (var field = LoadManagerLogicFactory.FieldStatus; field <= LoadManagerLogicFactory.FieldPower; field++)
                {
                    _loadInterfaces[LoadManagerLogicFactory.LoadInterface(load, field)] = (load - 1, field);
                }
            }
        }

        internal LoadManagerEngine Engine { get; } = new();

        protected override void ApplyParameter(Guid template, object value)
        {
            var settings = Engine.Settings;
            if (template == LoadManagerLogicFactory.ParamMaxPower)
            {
                settings.MaxPower = EnergyConvert.ToNonNegative(value, LoadManagerLogicFactory.DefaultMaxPower);
            }
            else if (template == LoadManagerLogicFactory.ParamHysteresis)
            {
                settings.Hysteresis = EnergyConvert.ToNonNegative(value, LoadManagerLogicFactory.DefaultHysteresis);
            }
            else if (template == LoadManagerLogicFactory.ParamShedDelay)
            {
                settings.ShedDelay = Seconds(value, LoadManagerLogicFactory.DefaultShedDelay);
            }
            else if (template == LoadManagerLogicFactory.ParamReleaseDelay)
            {
                settings.ReleaseDelay = Seconds(value, LoadManagerLogicFactory.DefaultReleaseDelay);
            }
            else if (_loadInterfaces.TryGetValue(template, out var target))
            {
                var load = Engine.Loads[target.Load];
                if (target.Field == LoadManagerLogicFactory.FieldPriority)
                {
                    load.Priority = (int)Math.Round(EnergyConvert.ToDouble(value, target.Load + 1));
                }
                else if (target.Field == LoadManagerLogicFactory.FieldPower)
                {
                    load.Power = EnergyConvert.ToNonNegative(value, 0);
                }
            }

            // a linked max power must act at once, not only with the next power value
            if (Running && template == LoadManagerLogicFactory.ParamMaxPower)
            {
                EvaluateAndDispatch();
            }
        }

        protected override void ApplyInput(Guid template, object value)
        {
            if (template == LoadManagerLogicFactory.InputPower)
            {
                Engine.Power = EnergyConvert.ToDouble(value);
            }
            else if (_loadInterfaces.TryGetValue(template, out var target) && target.Field == LoadManagerLogicFactory.FieldStatus)
            {
                Engine.Loads[target.Load].Active = value == null ? null : EnergyConvert.ToBool(value);
            }
        }

        protected override void EvaluateAndDispatch()
        {
            Engine.Evaluate(Now());

            Dispatch(LoadManagerLogicFactory.OutputOverload, Engine.Overload);
            Dispatch(LoadManagerLogicFactory.OutputReserve, Math.Round(Engine.Reserve, 3));
            for (var i = 0; i < LoadManagerEngine.LoadCount; i++)
            {
                Dispatch(LoadManagerLogicFactory.LoadInterface(i + 1, LoadManagerLogicFactory.FieldLock), Engine.Loads[i].Shed);
            }
        }
    }
}
