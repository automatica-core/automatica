using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Meter for any quantity (water, gas, heat, ...): flow (unit per hour), absolute reading and/or pulses in, flow and
    /// quantity today / month / total out. The unit parameter is only a label.
    /// </summary>
    public class UtilityMeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputFlow = new Guid("cdbd24be-ce57-4e7a-a68f-b17f4d9b8f12");
        public static readonly Guid InputReading = new Guid("67c45d83-d376-4644-b233-0506397aec66");
        public static readonly Guid InputPulse = new Guid("e28f68b8-5d93-4832-a67a-08bc6cf643c6");

        public static readonly Guid OutputFlow = new Guid("9a317d6f-c55a-413b-9ac7-56023253eeff");
        public static readonly Guid[] OutputCounters = { new Guid("ae0d6353-0877-4c6c-851b-9c15218b0b7f"), new Guid("efd805eb-1421-4729-8374-1d3bd8bbc99d"), new Guid("dbd41cd6-03ad-42f7-b1fa-06abc897546c") };

        public static readonly Guid ParamUnit = new Guid("8acc937e-61ed-427c-b902-efcf1b8dc2f9");
        public static readonly Guid ParamPulsesPerUnit = new Guid("895c6bcf-ef5a-4cd4-a284-dd314f86d5b4");
        public static readonly Guid ParamReadingFactor = new Guid("d14cf375-e622-4049-b5e3-bcf2c70a6e23");

        internal const double DefaultPulsesPerUnit = 1;

        public override string LogicName => "Metering.MeterUtility";
        public override Guid LogicGuid => new Guid("5df69543-8238-47f9-b7b1-47b4ae308c91");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER_UTILITY";
        protected override string Key => "metering.meter_utility";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            var sort = 0;
            Input(factory, InputFlow, "FLOW", "flow", ++sort);
            Input(factory, InputReading, "READING", "reading", ++sort);
            Input(factory, InputPulse, "PULSE", "pulse", ++sort);

            sort = 0;
            Output(factory, OutputFlow, "FLOW", "flow", ++sort, null, RuleInterfaceType.Status);
            Counters(factory, OutputCounters, "", "", ref sort);

            sort = 0;
            Param(factory, ParamUnit, "UNIT", "unit", ++sort, RuleInterfaceParameterDataType.Text, "m³");
            Param(factory, ParamPulsesPerUnit, "PULSES_PER_UNIT", "pulsesPerUnit", ++sort, RuleInterfaceParameterDataType.Double, DefaultPulsesPerUnit);
            Param(factory, ParamReadingFactor, "READING_FACTOR", "readingFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new UtilityMeterRule(context);
    }

    public class UtilityMeterRule : MeterRuleBase
    {
        internal const string Quantity = "quantity";

        private readonly RateEstimator _rate = new();
        private readonly PulseCounter _pulses = new();
        private double _pulsesPerUnit = UtilityMeterLogicFactory.DefaultPulsesPerUnit;
        private double _readingFactor = 1;
        private double? _flow;

        public UtilityMeterRule(ILogicContext context) : base(context, Quantity)
        {
        }

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            var register = Registers[Quantity];
            if (template == UtilityMeterLogicFactory.InputFlow)
            {
                if (ToDouble(value) is not { } flow)
                {
                    return false;
                }
                _flow = flow;
                register.SetPower(flow, now);
                return true;
            }
            if (template == UtilityMeterLogicFactory.InputReading)
            {
                if (ToDouble(value) is not { } reading)
                {
                    return false;
                }
                _rate.Add(register.SetReading(reading * _readingFactor, now), now);
                return true;
            }
            if (template == UtilityMeterLogicFactory.InputPulse)
            {
                var pulses = _pulses.Pulses(value);
                if (pulses <= 0)
                {
                    return false;
                }
                var quantity = pulses / _pulsesPerUnit;
                register.AddEnergy(quantity, now);
                _rate.Add(quantity, now);
                return true;
            }
            return false;
        }

        protected override void Resume(DateTimeOffset now)
        {
            if (_flow is { } flow)
            {
                Registers[Quantity].SetPower(flow, now);
            }
        }

        protected override void OnParameter(Guid template, object value)
        {
            if (template == UtilityMeterLogicFactory.ParamPulsesPerUnit)
            {
                _pulsesPerUnit = ToDouble(value) is { } d && d > 0 ? d : UtilityMeterLogicFactory.DefaultPulsesPerUnit;
            }
            else if (template == UtilityMeterLogicFactory.ParamReadingFactor)
            {
                _readingFactor = Factor(value);
            }
        }

        protected override void SaveValues(IDictionary<string, double> values) => _pulses.Save(values);

        protected override void LoadValues(IReadOnlyDictionary<string, double> values) => _pulses.Load(values);

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            var quantity = Registers[Quantity];
            yield return (UtilityMeterLogicFactory.OutputFlow, _flow ?? _rate.Current(now));
            yield return (UtilityMeterLogicFactory.OutputCounters[0], quantity.Today);
            yield return (UtilityMeterLogicFactory.OutputCounters[1], quantity.Month);
            yield return (UtilityMeterLogicFactory.OutputCounters[2], quantity.Total);
        }
    }
}
