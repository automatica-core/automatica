using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// S0 pulse meter: pulses (bool edges or a pulse counter, see <see cref="PulseCounter"/>) and pulses per kWh in,
    /// power (from the time between pulses) and energy today / month / total out.
    /// </summary>
    public class PulseMeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputPulse = new Guid("b1ed4c97-9712-470f-a167-818e06d56f5b");

        public static readonly Guid OutputPower = new Guid("f61bf9c6-87e8-4be7-9814-dbc0245b8148");
        public static readonly Guid[] OutputCounters = { new Guid("5f5630f7-0cbd-443f-a485-339c185c2028"), new Guid("49fbb065-b691-48be-b752-70844b88d13b"), new Guid("6eec6144-6ef6-4fce-bf51-1033aa122e24") };

        public static readonly Guid ParamPulsesPerKwh = new Guid("4b2ef930-37a3-473e-bb62-c5788bcb54ab");

        internal const double DefaultPulsesPerKwh = 1000;

        public override string LogicName => "Metering.MeterPulse";
        public override Guid LogicGuid => new Guid("8c57ad14-89b6-43ed-a760-92cac588a610");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER_PULSE";
        protected override string Key => "metering.meter_pulse";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            Input(factory, InputPulse, "PULSE", "pulse", 1);

            var sort = 0;
            Output(factory, OutputPower, "POWER", "power", ++sort, Common, RuleInterfaceType.Status);
            Counters(factory, OutputCounters, "", "", ref sort, Common);

            Param(factory, ParamPulsesPerKwh, "PULSES_PER_KWH", "pulsesPerKwh", 1, RuleInterfaceParameterDataType.Double, DefaultPulsesPerKwh);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new PulseMeterRule(context);
    }

    public class PulseMeterRule : MeterRuleBase
    {
        internal const string Energy = "energy";

        private readonly RateEstimator _rate = new();
        private readonly PulseCounter _pulses = new();
        private double _pulsesPerKwh = PulseMeterLogicFactory.DefaultPulsesPerKwh;

        public PulseMeterRule(ILogicContext context) : base(context, Energy)
        {
        }

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            if (template != PulseMeterLogicFactory.InputPulse)
            {
                return false;
            }
            var pulses = _pulses.Pulses(value);
            if (pulses <= 0)
            {
                return false;
            }
            var energy = pulses / _pulsesPerKwh;
            Registers[Energy].AddEnergy(energy, now);
            _rate.Add(energy, now);
            return true;
        }

        protected override void OnParameter(Guid template, object value)
        {
            if (template == PulseMeterLogicFactory.ParamPulsesPerKwh)
            {
                _pulsesPerKwh = ToDouble(value) is { } d && d > 0 ? d : PulseMeterLogicFactory.DefaultPulsesPerKwh;
            }
        }

        protected override void SaveValues(IDictionary<string, double> values) => _pulses.Save(values);

        protected override void LoadValues(IReadOnlyDictionary<string, double> values) => _pulses.Load(values);

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            var energy = Registers[Energy];
            yield return (PulseMeterLogicFactory.OutputPower, _rate.Current(now));
            yield return (PulseMeterLogicFactory.OutputCounters[0], energy.Today);
            yield return (PulseMeterLogicFactory.OutputCounters[1], energy.Month);
            yield return (PulseMeterLogicFactory.OutputCounters[2], energy.Total);
        }
    }
}
