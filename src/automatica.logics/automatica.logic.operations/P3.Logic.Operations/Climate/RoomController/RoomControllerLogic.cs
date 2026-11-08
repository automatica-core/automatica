using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.RoomController.RoomControllerLogicFactory;

namespace P3.Logic.Operations.Climate.RoomController
{
    /// <summary>
    /// Current temperature mode (output Os), same numbers as the Loxone block
    /// </summary>
    public enum TemperatureMode
    {
        Off = -1,
        Eco = 0,
        Comfort = 1,
        BuildingProtection = 2,
        ManualTarget = 3
    }

    /// <summary>
    /// Intelligent room controller, modelled on the Loxone block. It keeps the room between a heating and a cooling setpoint and
    /// switches between heating and cooling on its own.
    /// <list type="bullet">
    /// <item>Mode: -1 off, 0 automatic, 1 automatic heating only, 2 automatic cooling only, 3 fixed setpoint (Tt), 4 fixed setpoint heating only,
    /// 5 fixed setpoint cooling only. Unlinked = 0.</item>
    /// <item>Automatic modes: Comfort (Tch / Tcc), Eco (Tch - Teh / Tcc + Tec) or building protection (Tfp / Thp). The default mode
    /// (no schedule in this block - link a timer to C or E) is a parameter. C, E and Bp start their mode on the rising edge and extend it by
    /// Cet / EBpet after the falling edge (EBpet = 0: until the next override); the last one started wins. Presence P keeps Comfort
    /// while on and Pet after it, unless Eco or building protection was started. A rising edge on Off cancels these timers, Off = 1 locks the block.</item>
    /// <item>An open window (Dwc) switches to building protection after Ddwc, as long as the outdoor temperature (To) works against the
    /// current heating/cooling (always when To is unknown).</item>
    /// <item>Tcomf (the visualization) sets the comfort heating temperature; comfort cooling keeps its distance to it.</item>
    /// <item>H / C / HC are valve positions 0..100 % of a PI controller (proportional band Pb in K, integral Ki in % per K and second),
    /// D is the signed demand for a central source (+ heating, - cooling). Sm from a central source (1 heating, 2 cooling, 3 none,
    /// 0/unlinked = any) closes the valves of the other mode, D is not affected. Eh / Ec shift the comfort and eco setpoints by Texc.</item>
    /// </list>
    /// </summary>
    public class RoomControllerLogic : ClimateLogic
    {
        internal const double BoostDeviation = 1.5;
        internal const double ShadingHysteresis = 0.4;
        internal const double MinimumGap = 0.5;

        private readonly PiController _heatPi = new();
        private readonly PiController _coolPi = new();

        private TemperatureMode? _override;
        private DateTime? _overrideUntil;
        private bool _overrideHeld;
        private bool _presenceHeld;
        private DateTime? _presenceUntil;
        private DateTime? _windowOpenSince;

        private int _hcm;
        private double _heatValve;
        private double _coolValve;
        private bool _shading;

        public RoomControllerLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 5;

        internal TemperatureMode CurrentMode => ResolveMode();
        internal int HeatingCoolingMode => _hcm;

        protected override void OnInput(Guid template, object value, object previous)
        {
            var rising = ClimateValue.ToBool(value) && !ClimateValue.ToBool(previous);
            var falling = !ClimateValue.ToBool(value) && ClimateValue.ToBool(previous);

            if (template == F.InComfort)
            {
                Override(TemperatureMode.Comfort, rising, falling, Param(F.ParamComfortExtend, F.DefaultComfortExtend));
            }
            else if (template == F.InEco)
            {
                Override(TemperatureMode.Eco, rising, falling, Param(F.ParamEcoExtend, F.DefaultEcoExtend));
            }
            else if (template == F.InBuildingProtection)
            {
                Override(TemperatureMode.BuildingProtection, rising, falling, Param(F.ParamEcoExtend, F.DefaultEcoExtend));
            }
            else if (template == F.InPresence)
            {
                if (rising)
                {
                    _presenceHeld = true;
                    _presenceUntil = null;
                }
                else if (falling)
                {
                    _presenceHeld = false;
                    _presenceUntil = Now.AddSeconds(Math.Max(0, Param(F.ParamPresenceExtend, F.DefaultPresenceExtend)));
                }
            }
            else if (template == F.InOff && rising)
            {
                _override = null;
                _overrideUntil = null;
                _overrideHeld = false;
                _presenceUntil = null;
            }
            else if (template == F.InWindow)
            {
                if (ClimateValue.ToBool(value))
                {
                    _windowOpenSince ??= Now;
                }
                else
                {
                    _windowOpenSince = null;
                }
            }
        }

        private void Override(TemperatureMode mode, bool rising, bool falling, double extendSeconds)
        {
            if (rising)
            {
                _override = mode;
                _overrideHeld = true;
                _overrideUntil = null;
            }
            else if (falling && _override == mode)
            {
                _overrideHeld = false;
                // 0 = no end (Comfort without extension ends at once)
                _overrideUntil = extendSeconds > 0 ? Now.AddSeconds(extendSeconds)
                    : mode == TemperatureMode.Comfort ? Now : null;
            }
        }

        private void ExpireTimers()
        {
            var now = Now;
            if (_override.HasValue && !_overrideHeld && _overrideUntil.HasValue && now >= _overrideUntil.Value)
            {
                _override = null;
                _overrideUntil = null;
            }
            if (_presenceUntil.HasValue && now >= _presenceUntil.Value)
            {
                _presenceUntil = null;
            }
        }

        private int Mode => Math.Clamp(InInt(F.InMode, 0), -1, 5);

        private bool Locked => InBool(F.InOff);

        private bool HeatAllowed => Mode is 0 or 1 or 3 or 4;
        private bool CoolAllowed => Mode is 0 or 2 or 3 or 5;

        private bool PresenceActive => !InBool(F.InDisablePresence) && (_presenceHeld || _presenceUntil.HasValue);

        private TemperatureMode ResolveMode()
        {
            ExpireTimers();
            if (Locked || Mode < 0)
            {
                return TemperatureMode.Off;
            }
            if (Mode >= 3)
            {
                return TemperatureMode.ManualTarget;
            }
            if (_override == TemperatureMode.BuildingProtection || WindowProtection())
            {
                return TemperatureMode.BuildingProtection;
            }
            if (_override == TemperatureMode.Eco)
            {
                return TemperatureMode.Eco;
            }
            if (_override == TemperatureMode.Comfort || PresenceActive)
            {
                return TemperatureMode.Comfort;
            }
            return ParamInt(F.ParamDefaultMode, 1) == 0 ? TemperatureMode.Eco : TemperatureMode.Comfort;
        }

        private bool WindowProtection()
        {
            if (!_windowOpenSince.HasValue || (Now - _windowOpenSince.Value).TotalSeconds < Param(F.ParamWindowDelay, F.DefaultWindowDelay))
            {
                return false;
            }
            var outdoor = InNumber(F.InOutdoor);
            var room = InNumber(F.InCurrent);
            if (!outdoor.HasValue || !room.HasValue)
            {
                return true;
            }
            // only when the outside air works against the current mode
            return _hcm switch
            {
                1 => outdoor.Value < room.Value,
                -1 => outdoor.Value > room.Value,
                _ => true
            };
        }

        private (double Heat, double Cool) ComfortTemperatures()
        {
            var heat = Param(F.ParamComfortHeating, F.DefaultComfortHeating);
            var cool = Param(F.ParamComfortCooling, F.DefaultComfortCooling);
            var visu = InNumber(F.InComfortTemperature);
            if (visu.HasValue)
            {
                var gap = Math.Max(MinimumGap, cool - heat);
                heat = visu.Value;
                cool = visu.Value + gap;
            }
            return (heat, Math.Max(cool, heat + MinimumGap));
        }

        /// <summary>
        /// Heating and cooling setpoint of the current temperature mode, including surplus energy offsets
        /// </summary>
        internal (double Heat, double Cool) Setpoints(TemperatureMode mode)
        {
            var (ch, cc) = ComfortTemperatures();
            double heat, cool;
            switch (mode)
            {
                case TemperatureMode.Eco:
                    heat = ch - Math.Abs(Param(F.ParamEcoHeating, F.DefaultEco));
                    cool = cc + Math.Abs(Param(F.ParamEcoCooling, F.DefaultEco));
                    break;
                case TemperatureMode.BuildingProtection:
                case TemperatureMode.Off:
                    return (Param(F.ParamFrostProtection, F.DefaultFrost), Param(F.ParamHeatProtection, F.DefaultHeatProtection));
                case TemperatureMode.ManualTarget:
                {
                    var target = InNumber(F.InTarget) ?? ch;
                    return (target, target + Math.Max(0, Param(F.ParamDeviation, F.DefaultDeviation)));
                }
                default:
                    heat = ch;
                    cool = cc;
                    break;
            }

            var excess = Math.Abs(Param(F.ParamExcessOffset, F.DefaultExcess));
            if (InBool(F.InExcessHeating))
            {
                heat = Math.Min(heat + excess, cool - MinimumGap);
            }
            if (InBool(F.InExcessCooling))
            {
                cool = Math.Max(cool - excess, heat + MinimumGap);
            }
            return (heat, cool);
        }

        private void UpdateDirection(double actual, (double Heat, double Cool) sp)
        {
            if (actual < sp.Heat && HeatAllowed)
            {
                _hcm = 1;
            }
            else if (actual > sp.Cool && CoolAllowed)
            {
                _hcm = -1;
            }
            else if ((_hcm == 1 && !HeatAllowed) || (_hcm == -1 && !CoolAllowed))
            {
                _hcm = 0;
            }
        }

        protected override void OnTick(double elapsedSeconds)
        {
            var mode = ResolveMode();
            var actual = InNumber(F.InCurrent);
            if (mode == TemperatureMode.Off || !actual.HasValue)
            {
                _heatPi.Reset();
                _coolPi.Reset();
                _heatValve = _coolValve = 0;
                if (mode == TemperatureMode.Off)
                {
                    _hcm = 0;
                }
                return;
            }

            var sp = Setpoints(mode);
            UpdateDirection(actual.Value, sp);

            var kp = 100 / Math.Max(0.1, Param(F.ParamBand, F.DefaultBand));
            var ki = Math.Max(0, Param(F.ParamKi, F.DefaultKi));
            _heatPi.Kp = _coolPi.Kp = kp;
            _heatPi.Ki = _coolPi.Ki = ki;

            if (_hcm == 1)
            {
                _heatValve = _heatPi.Step(sp.Heat - actual.Value, elapsedSeconds);
                _coolPi.Reset();
                _coolValve = 0;
            }
            else if (_hcm == -1)
            {
                _coolValve = _coolPi.Step(actual.Value - sp.Cool, elapsedSeconds);
                _heatPi.Reset();
                _heatValve = 0;
            }
            else
            {
                _heatPi.Reset();
                _coolPi.Reset();
                _heatValve = _coolValve = 0;
            }
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var mode = ResolveMode();
            var (comfortHeat, comfortCool) = ComfortTemperatures();
            Set(outputs, F.OutComfortHeating, comfortHeat, 1);
            Set(outputs, F.OutComfortCooling, comfortCool, 1);
            outputs[F.OutTemperatureMode] = (int)mode;

            var actual = InNumber(F.InCurrent);
            if (mode == TemperatureMode.Off)
            {
                outputs[F.OutHeating] = 0d;
                outputs[F.OutCooling] = 0d;
                outputs[F.OutHeatingCooling] = 0d;
                outputs[F.OutDemand] = 0d;
                outputs[F.OutHeatingCoolingMode] = 0;
                outputs[F.OutBoost] = false;
                outputs[F.OutShading] = false;
                outputs[F.OutError] = false;
                return;
            }

            var sp = Setpoints(mode);
            if (actual.HasValue)
            {
                UpdateDirection(actual.Value, sp);
            }
            if (_hcm == 1 && !HeatAllowed || _hcm == -1 && !CoolAllowed)
            {
                _hcm = 0;
            }

            Set(outputs, F.OutTarget, _hcm == -1 ? sp.Cool : sp.Heat, 1);
            outputs[F.OutHeatingCoolingMode] = _hcm;

            var heat = _hcm == 1 ? _heatValve : 0;
            var cool = _hcm == -1 ? _coolValve : 0;
            Set(outputs, F.OutDemand, heat > 0 ? heat : -cool, 1);

            var source = HasInput(F.InSourceMode) ? InInt(F.InSourceMode, 0) : 0;
            if (source is 2 or 3)
            {
                heat = 0;
            }
            if (source is 1 or 3)
            {
                cool = 0;
            }
            Set(outputs, F.OutHeating, heat, 1);
            Set(outputs, F.OutCooling, cool, 1);
            Set(outputs, F.OutHeatingCooling, heat > 0 ? heat : cool, 1);

            if (!actual.HasValue)
            {
                return;
            }

            var shadingLimit = _hcm == -1 ? Param(F.ParamShadingCooling, F.DefaultShadingCooling) : Param(F.ParamShadingHeating, F.DefaultShadingHeating);
            if (actual.Value > shadingLimit)
            {
                _shading = true;
            }
            else if (actual.Value < shadingLimit - ShadingHysteresis)
            {
                _shading = false;
            }
            outputs[F.OutShading] = _shading;

            var deviation = _hcm == -1 ? actual.Value - sp.Cool : sp.Heat - actual.Value;
            outputs[F.OutBoost] = _hcm != 0 && deviation > BoostDeviation;
            outputs[F.OutError] = actual.Value < Param(F.ParamFrostProtection, F.DefaultFrost) || actual.Value > Param(F.ParamHeatProtection, F.DefaultHeatProtection);
        }
    }
}
