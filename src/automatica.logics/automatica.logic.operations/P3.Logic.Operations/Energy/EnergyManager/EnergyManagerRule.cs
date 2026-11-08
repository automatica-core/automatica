using System;
using System.Collections.Generic;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Energy.EnergyManager
{
    /// <summary>
    /// Wires the <see cref="EnergyManagerEngine"/> to the logic interfaces
    /// </summary>
    public class EnergyManagerRule : EnergyLogicBase
    {
        private readonly Dictionary<Guid, (int Load, int Field)> _loadInterfaces = new();

        public EnergyManagerRule(ILogicContext context) : base(context)
        {
            for (var load = 1; load <= EnergyManagerEngine.LoadCount; load++)
            {
                for (var field = EnergyManagerLogicFactory.FieldManual; field <= EnergyManagerLogicFactory.FieldMinOff; field++)
                {
                    _loadInterfaces[EnergyManagerLogicFactory.LoadInterface(load, field)] = (load - 1, field);
                }
            }
        }

        internal EnergyManagerEngine Engine { get; } = new();

        protected override void ApplyParameter(Guid template, object value)
        {
            var settings = Engine.Settings;
            if (template == EnergyManagerLogicFactory.ParamInvertGrid)
            {
                settings.InvertGrid = EnergyConvert.ToBool(value);
            }
            else if (template == EnergyManagerLogicFactory.ParamReserve)
            {
                settings.Reserve = EnergyConvert.ToDouble(value, EnergyManagerLogicFactory.DefaultReserve);
            }
            else if (template == EnergyManagerLogicFactory.ParamHysteresis)
            {
                settings.Hysteresis = EnergyConvert.ToNonNegative(value, EnergyManagerLogicFactory.DefaultHysteresis);
            }
            else if (template == EnergyManagerLogicFactory.ParamOnDelay)
            {
                settings.OnDelay = Seconds(value, EnergyManagerLogicFactory.DefaultOnDelay);
            }
            else if (template == EnergyManagerLogicFactory.ParamOffDelay)
            {
                settings.OffDelay = Seconds(value, EnergyManagerLogicFactory.DefaultOffDelay);
            }
            else if (template == EnergyManagerLogicFactory.ParamModulationInterval)
            {
                settings.ModulationInterval = Seconds(value, EnergyManagerLogicFactory.DefaultModulationInterval);
            }
            else if (template == EnergyManagerLogicFactory.ParamMinSoc)
            {
                settings.MinSoc = Math.Clamp(EnergyConvert.ToDouble(value, 0), 0, 100);
            }
            else if (_loadInterfaces.TryGetValue(template, out var target))
            {
                var load = Engine.Loads[target.Load];
                switch (target.Field)
                {
                    case EnergyManagerLogicFactory.FieldPriority:
                        load.Priority = (int)Math.Round(EnergyConvert.ToDouble(value, target.Load + 1));
                        break;
                    case EnergyManagerLogicFactory.FieldOnPower:
                        load.OnPower = EnergyConvert.ToNonNegative(value, 0);
                        break;
                    case EnergyManagerLogicFactory.FieldMaxPower:
                        load.MaxPower = EnergyConvert.ToNonNegative(value, 0);
                        break;
                    case EnergyManagerLogicFactory.FieldType:
                        load.Type = (int)Math.Round(EnergyConvert.ToDouble(value, 0)) == (int)EnergyLoadType.Analog ? EnergyLoadType.Analog : EnergyLoadType.Digital;
                        break;
                    case EnergyManagerLogicFactory.FieldMinOn:
                        load.MinOn = Seconds(value, EnergyManagerLogicFactory.DefaultMinOn);
                        break;
                    case EnergyManagerLogicFactory.FieldMinOff:
                        load.MinOff = Seconds(value, EnergyManagerLogicFactory.DefaultMinOff);
                        break;
                }
            }
        }

        protected override void ApplyInput(Guid template, object value)
        {
            if (template == EnergyManagerLogicFactory.InputGrid)
            {
                Engine.Grid = EnergyConvert.ToDouble(value);
            }
            else if (template == EnergyManagerLogicFactory.InputSurplus)
            {
                Engine.Surplus = EnergyConvert.ToDouble(value);
            }
            else if (template == EnergyManagerLogicFactory.InputBattery)
            {
                Engine.Battery = EnergyConvert.ToDouble(value);
            }
            else if (template == EnergyManagerLogicFactory.InputSoc)
            {
                Engine.Soc = EnergyConvert.ToDouble(value);
            }
            else if (template == EnergyManagerLogicFactory.InputEnable)
            {
                Engine.Enabled = EnergyConvert.ToBool(value, true);
            }
            else if (_loadInterfaces.TryGetValue(template, out var target))
            {
                if (target.Field == EnergyManagerLogicFactory.FieldManual)
                {
                    Engine.States[target.Load].Manual = EnergyConvert.ToBool(value);
                }
                else if (target.Field == EnergyManagerLogicFactory.FieldLock)
                {
                    Engine.States[target.Load].Locked = EnergyConvert.ToBool(value);
                }
            }
        }

        protected override void EvaluateAndDispatch()
        {
            Engine.Evaluate(Now());

            Dispatch(EnergyManagerLogicFactory.OutputAvailable, Math.Round(Engine.Available, 3));
            Dispatch(EnergyManagerLogicFactory.OutputAllocated, Math.Round(Engine.Allocated, 3));
            for (var i = 0; i < EnergyManagerEngine.LoadCount; i++)
            {
                var state = Engine.States[i];
                Dispatch(EnergyManagerLogicFactory.LoadInterface(i + 1, EnergyManagerLogicFactory.FieldSwitch), state.On);
                Dispatch(EnergyManagerLogicFactory.LoadInterface(i + 1, EnergyManagerLogicFactory.FieldSetpoint), state.On ? Math.Round(state.Setpoint, 3) : 0d);
            }
        }
    }
}
