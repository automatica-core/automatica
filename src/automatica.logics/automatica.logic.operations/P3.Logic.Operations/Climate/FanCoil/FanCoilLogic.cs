using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.FanCoil
{
    /// <summary>
    /// Interface ids of a fan coil block - the plain and the fresh air unit share the keys, the ids differ by the logic guid
    /// </summary>
    public sealed class FanCoilIds
    {
        public FanCoilIds(Guid logic)
        {
            Logic = logic;
            Current = ClimateTemplateBuilder.InputId(logic, "tc");
            Target = ClimateTemplateBuilder.InputId(logic, "tt");
            HeatingAvailable = ClimateTemplateBuilder.InputId(logic, "ha");
            CoolingAvailable = ClimateTemplateBuilder.InputId(logic, "ca");
            Window = ClimateTemplateBuilder.InputId(logic, "dwc");
            Off = ClimateTemplateBuilder.InputId(logic, "off");
            Silent = ClimateTemplateBuilder.InputId(logic, "sm");
            Boost = ClimateTemplateBuilder.InputId(logic, "bm");
            PauseTimer = ClimateTemplateBuilder.InputId(logic, "pt");
            Fan = ClimateTemplateBuilder.InputId(logic, "fan");
            RoomMode = ClimateTemplateBuilder.InputId(logic, "hcm");
            Co2 = ClimateTemplateBuilder.InputId(logic, "co2");

            OutHeating = ClimateTemplateBuilder.OutputId(logic, "h");
            OutCooling = ClimateTemplateBuilder.OutputId(logic, "cool");
            OutHeatingCooling = ClimateTemplateBuilder.OutputId(logic, "hc");
            OutFan = ClimateTemplateBuilder.OutputId(logic, "fanout");
            OutFanStep = ClimateTemplateBuilder.OutputId(logic, "fans");
            OutCurrent = ClimateTemplateBuilder.OutputId(logic, "tcout");
            OutTarget = ClimateTemplateBuilder.OutputId(logic, "ttout");
            OutMode = ClimateTemplateBuilder.OutputId(logic, "modeout");
            OutStatus = ClimateTemplateBuilder.OutputId(logic, "s");
            OutDemand = ClimateTemplateBuilder.OutputId(logic, "demand");

            ParamFanMax = ClimateTemplateBuilder.ParamId(logic, "fmax");
            ParamMode = ClimateTemplateBuilder.ParamId(logic, "mode");
            ParamPauseDuration = ClimateTemplateBuilder.ParamId(logic, "ptd");
            ParamFanKp = ClimateTemplateBuilder.ParamId(logic, "fkp");
            ParamFanKi = ClimateTemplateBuilder.ParamId(logic, "fki");
            ParamFanSample = ClimateTemplateBuilder.ParamId(logic, "fst");
            ParamSteps = ClimateTemplateBuilder.ParamId(logic, "steps");
            ParamSilentFan = ClimateTemplateBuilder.ParamId(logic, "silent");
            ParamCo2Target = ClimateTemplateBuilder.ParamId(logic, "co2t");
            ParamCo2Kp = ClimateTemplateBuilder.ParamId(logic, "ckp");
            ParamCo2Ki = ClimateTemplateBuilder.ParamId(logic, "cki");
            ParamCo2Sample = ClimateTemplateBuilder.ParamId(logic, "cst");
            ParamValveKp = ClimateTemplateBuilder.ParamId(logic, "vkp");
            ParamValveKi = ClimateTemplateBuilder.ParamId(logic, "vki");
            ParamValveSample = ClimateTemplateBuilder.ParamId(logic, "vst");
        }

        public Guid Logic { get; }
        public Guid Current { get; }
        public Guid Target { get; }
        public Guid HeatingAvailable { get; }
        public Guid CoolingAvailable { get; }
        public Guid Window { get; }
        public Guid Off { get; }
        public Guid Silent { get; }
        public Guid Boost { get; }
        public Guid PauseTimer { get; }
        public Guid Fan { get; }
        public Guid RoomMode { get; }
        public Guid Co2 { get; }
        public Guid OutHeating { get; }
        public Guid OutCooling { get; }
        public Guid OutHeatingCooling { get; }
        public Guid OutFan { get; }
        public Guid OutFanStep { get; }
        public Guid OutCurrent { get; }
        public Guid OutTarget { get; }
        public Guid OutMode { get; }
        public Guid OutStatus { get; }
        public Guid OutDemand { get; }
        public Guid ParamFanMax { get; }
        public Guid ParamMode { get; }
        public Guid ParamPauseDuration { get; }
        public Guid ParamFanKp { get; }
        public Guid ParamFanKi { get; }
        public Guid ParamFanSample { get; }
        public Guid ParamSteps { get; }
        public Guid ParamSilentFan { get; }
        public Guid ParamCo2Target { get; }
        public Guid ParamCo2Kp { get; }
        public Guid ParamCo2Ki { get; }
        public Guid ParamCo2Sample { get; }
        public Guid ParamValveKp { get; }
        public Guid ParamValveKi { get; }
        public Guid ParamValveSample { get; }

        internal static void CreateTemplates(ILogicTemplateFactory factory, Guid logic, string block, string key, bool freshAir)
        {
            var b = new ClimateTemplateBuilder(factory, logic, block, key, 200, 100);
            var i = 1;
            b.Input("tc", "Tc", i++);
            b.Input("tt", "Tt", i++);
            b.Input("ha", "Ha", i++);
            b.Input("ca", "Ca", i++);
            if (freshAir)
            {
                b.Input("co2", "CO2", i++);
            }
            b.Input("dwc", "Dwc", i++);
            b.Input("off", "Off", i++);
            b.Input("sm", "Sm", i++);
            b.Input("bm", "Bm", i++);
            b.Input("pt", "Pt", i++);
            b.Input("fan", "Fan", i++);
            b.Input("hcm", "HCm", i);

            var o = 1;
            b.Output("h", "H", o++);
            b.Output("cool", "C", o++);
            b.Output("hc", "HC", o++);
            b.Output("fanout", "Fan", o++);
            b.Output("fans", "FanS", o++);
            b.Output("tcout", "Tc", o++);
            b.Output("ttout", "Tt", o++);
            b.Output("modeout", "Mode", o++);
            b.Output("s", "S", o++);
            b.Output("demand", "D", o);

            var p = 1;
            if (freshAir)
            {
                b.Number("co2t", p++, FanCoilLogic.DefaultCo2Target);
            }
            b.Number("fmax", p++, FanCoilLogic.DefaultFanMax);
            b.Enum("mode", p++, 1, (0, "OFF"), (1, "AUTO"), (2, "HEATING"), (3, "COOLING"), (4, "VENTILATION"));
            b.Number("ptd", p++, 7200);
            b.Number("fkp", p++, FanCoilLogic.DefaultKp);
            b.Number("fki", p++, FanCoilLogic.DefaultKi);
            b.Number("fst", p++, FanCoilLogic.DefaultSample);
            if (freshAir)
            {
                b.Number("ckp", p++, FanCoilLogic.DefaultKp);
                b.Number("cki", p++, FanCoilLogic.DefaultKi);
                b.Number("cst", p++, FanCoilLogic.DefaultSample);
                b.Number("vkp", p++, FanCoilLogic.DefaultKp);
                b.Number("vki", p++, FanCoilLogic.DefaultKi);
                b.Number("vst", p++, FanCoilLogic.DefaultSample);
            }
            b.Integer("steps", p++, 3);
            b.Number("silent", p, 10);
            b.VisuTile(ClimateTile.Unit);
        }
    }

    /// <summary>
    /// Fan coil unit controller, plain or with fresh air (CO2), modelled on the Loxone blocks.
    /// Mode: 0 off, 1 auto, 2 heating, 3 cooling, 4 ventilation. In auto the direction comes from a linked room controller (HCm: 1 heating,
    /// -1 cooling) or, standalone, from Tc / Tt (starts 0.5 K off the target, ends when the target is reached).
    /// While there is a temperature demand the valve of the direction is fully open (H / C / HC, 0..100 %) and a PI controller
    /// (FKp, FKi per K and second, sampled every FSt s) sets the fan between 0 and Fmax %. The fresh air unit also watches CO2: with bad air
    /// (above CO2t) its own PI drives the fan and, while heating or cooling, a valve PI (VKp, VKi, VSt) takes over the valve.
    /// The automatic fan changes once per minute by at most 20 %. Fan (input, 0..100 %, -1 auto) sets the fan by hand, Bm runs it at 100 %,
    /// Sm limits it to the silent speed. Ha / Ca (unlinked = available) close the valve and stop the fan of a direction without energy.
    /// Off, an open window (Dwc) or the pause timer (Pt, Ptd s) switch fan and valves off. D is the signed demand for the fan coil central.
    /// </summary>
    public class FanCoilLogic : ClimateLogic
    {
        internal const double DefaultFanMax = 80;
        internal const double DefaultKp = 50;
        internal const double DefaultKi = 0.01;
        internal const double DefaultSample = 60;
        internal const double DefaultCo2Target = 600;
        internal const double StartDeviation = 0.5;
        internal const double FanStepPercent = 20;
        internal const double FanStepSeconds = 60;

        private readonly FanCoilIds _ids;
        private readonly bool _freshAir;
        private readonly PiController _tempPi = new();
        private readonly PiController _co2Pi = new();
        private readonly PiController _valvePi = new();

        private double _tempAcc = double.MaxValue;
        private double _co2Acc = double.MaxValue;
        private double _valveAcc = double.MaxValue;
        private double _fanAcc = double.MaxValue;

        private int _direction;
        private double _tempOut;
        private double _co2Out;
        private double _valveOut;
        private double _autoFan;
        private DateTime? _pauseUntil;

        private double _heat;
        private double _cool;
        private double _fan;
        private bool _active;

        public FanCoilLogic(ILogicContext context, FanCoilIds ids, bool freshAir) : base(context)
        {
            _ids = ids;
            _freshAir = freshAir;
        }

        protected override double TickSeconds => 1;

        internal int Direction => _direction;

        protected override void OnInput(Guid template, object value, object previous)
        {
            if (template == _ids.PauseTimer && ClimateValue.ToBool(value) && !ClimateValue.ToBool(previous))
            {
                _pauseUntil = Now.AddSeconds(Math.Max(0, Param(_ids.ParamPauseDuration, 7200)));
            }
        }

        private int Mode => Math.Clamp(ParamInt(_ids.ParamMode, 1), 0, 4);

        private bool Inactive()
        {
            if (_pauseUntil.HasValue && Now >= _pauseUntil.Value)
            {
                _pauseUntil = null;
            }
            return Mode == 0 || InBool(_ids.Off) || InBool(_ids.Window) || _pauseUntil.HasValue;
        }

        private void ResetControl()
        {
            _tempPi.Reset();
            _co2Pi.Reset();
            _valvePi.Reset();
            _tempAcc = _co2Acc = _valveAcc = _fanAcc = double.MaxValue;
            _direction = 0;
            _tempOut = _co2Out = _valveOut = _autoFan = 0;
            _heat = _cool = _fan = 0;
        }

        private double Error(double target, double current) => _direction > 0 ? target - current : _direction < 0 ? current - target : 0;

        private void UpdateDirection(double? target, double? current)
        {
            switch (Mode)
            {
                case 2:
                    _direction = 1;
                    return;
                case 3:
                    _direction = -1;
                    return;
                case 4:
                    _direction = 0;
                    return;
            }
            if (HasInput(_ids.RoomMode))
            {
                _direction = Math.Sign(InInt(_ids.RoomMode, 0));
                return;
            }
            if (!target.HasValue || !current.HasValue)
            {
                return;
            }
            var deviation = target.Value - current.Value;
            if (_direction == 0)
            {
                _direction = deviation > StartDeviation ? 1 : deviation < -StartDeviation ? -1 : 0;
            }
            else if (Error(target.Value, current.Value) <= 0 && _tempOut <= 0)
            {
                _direction = 0;
            }
        }

        /// <summary>
        /// Runs a PI controller when its sampling time is over; true when it ran
        /// </summary>
        private static bool Sample(ref double accumulator, double elapsed, double sampleSeconds)
        {
            var first = accumulator == double.MaxValue;
            accumulator = first ? 0 : accumulator + elapsed;
            if (!first && accumulator < Math.Max(1, sampleSeconds))
            {
                return false;
            }
            if (!first)
            {
                accumulator -= Math.Max(1, sampleSeconds);
            }
            return true;
        }

        protected override void OnTick(double elapsedSeconds)
        {
            _active = !Inactive();
            if (!_active)
            {
                ResetControl();
                return;
            }

            var target = InNumber(_ids.Target);
            var current = InNumber(_ids.Current);
            var fanMax = Clamp(Param(_ids.ParamFanMax, DefaultFanMax), 0, 100);
            UpdateDirection(target, current);

            var error = target.HasValue && current.HasValue ? Error(target.Value, current.Value) : 0;
            if (_direction == 0)
            {
                _tempPi.Reset();
                _tempOut = 0;
                _tempAcc = double.MaxValue;
            }
            else if (Sample(ref _tempAcc, elapsedSeconds, Param(_ids.ParamFanSample, DefaultSample)))
            {
                _tempPi.Kp = Param(_ids.ParamFanKp, DefaultKp);
                _tempPi.Ki = Param(_ids.ParamFanKi, DefaultKi);
                _tempPi.Max = fanMax;
                _tempOut = _tempPi.Step(error, Param(_ids.ParamFanSample, DefaultSample));
            }
            var tempDemand = _direction != 0 && (error > 0 || _tempOut > 0);

            var badAir = false;
            if (_freshAir)
            {
                var co2 = InNumber(_ids.Co2);
                var co2Target = Param(_ids.ParamCo2Target, DefaultCo2Target);
                if (co2.HasValue && Sample(ref _co2Acc, elapsedSeconds, Param(_ids.ParamCo2Sample, DefaultSample)))
                {
                    _co2Pi.Kp = Param(_ids.ParamCo2Kp, DefaultKp);
                    _co2Pi.Ki = Param(_ids.ParamCo2Ki, DefaultKi);
                    _co2Pi.Max = fanMax;
                    // ppm: the gain is per 100 ppm, like a temperature controller per K
                    _co2Out = _co2Pi.Step((co2.Value - co2Target) / 100, Param(_ids.ParamCo2Sample, DefaultSample));
                }
                badAir = co2.HasValue && (co2.Value > co2Target || _co2Out > 0);

                if (tempDemand && badAir && Sample(ref _valveAcc, elapsedSeconds, Param(_ids.ParamValveSample, DefaultSample)))
                {
                    _valvePi.Kp = Param(_ids.ParamValveKp, DefaultKp);
                    _valvePi.Ki = Param(_ids.ParamValveKi, DefaultKi);
                    _valvePi.Max = 100;
                    _valveOut = _valvePi.Step(error, Param(_ids.ParamValveSample, DefaultSample));
                }
                else if (!(tempDemand && badAir))
                {
                    _valvePi.Reset();
                    _valveOut = 0;
                    _valveAcc = double.MaxValue;
                }
            }

            var available = _direction > 0 ? InBool(_ids.HeatingAvailable, true)
                : _direction < 0 && InBool(_ids.CoolingAvailable, true);
            var valve = !tempDemand || !available ? 0 : badAir ? _valveOut : 100;
            double wanted;
            if (Mode == 4)
            {
                wanted = _freshAir && badAir ? _co2Out : fanMax;
            }
            else if (badAir)
            {
                wanted = _co2Out;
            }
            else
            {
                wanted = tempDemand && available ? _tempOut : 0;
            }

            // the automatic air volume changes once a minute by at most 20 %
            if (Sample(ref _fanAcc, elapsedSeconds, FanStepSeconds))
            {
                _autoFan = Math.Clamp(wanted, _autoFan - FanStepPercent, _autoFan + FanStepPercent);
            }

            var fan = _autoFan;
            var manual = InNumber(_ids.Fan);
            if (manual.HasValue && manual.Value >= 0)
            {
                fan = Math.Min(100, manual.Value);
            }
            if (InBool(_ids.Boost))
            {
                fan = 100;
            }
            if (InBool(_ids.Silent))
            {
                fan = Math.Min(fan, Clamp(Param(_ids.ParamSilentFan, 10), 0, 100));
            }

            _fan = fan;
            _heat = _direction > 0 ? valve : 0;
            _cool = _direction < 0 ? valve : 0;
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var target = InNumber(_ids.Target);
            var current = InNumber(_ids.Current);
            if (current.HasValue)
            {
                Set(outputs, _ids.OutCurrent, current.Value, 1);
            }
            if (target.HasValue)
            {
                Set(outputs, _ids.OutTarget, target.Value, 1);
            }

            var active = _active && !Inactive();
            var heat = active ? _heat : 0;
            var cool = active ? _cool : 0;
            var fan = active ? _fan : 0;

            Set(outputs, _ids.OutHeating, heat, 1);
            Set(outputs, _ids.OutCooling, cool, 1);
            Set(outputs, _ids.OutHeatingCooling, Math.Max(heat, cool), 1);
            Set(outputs, _ids.OutFan, fan, 1);
            var steps = ParamInt(_ids.ParamSteps, 3);
            if (steps > 0)
            {
                outputs[_ids.OutFanStep] = (int)Math.Ceiling(fan / (100.0 / steps) - 1e-9);
            }
            outputs[_ids.OutMode] = active ? Mode : 0;
            outputs[_ids.OutStatus] = heat > 0 || cool > 0 || fan > 0;

            var demand = active && _direction != 0 && _tempOut > 0 ? Math.Max(1, _tempOut) : 0;
            Set(outputs, _ids.OutDemand, _direction < 0 ? -demand : demand, 1);
        }
    }
}
