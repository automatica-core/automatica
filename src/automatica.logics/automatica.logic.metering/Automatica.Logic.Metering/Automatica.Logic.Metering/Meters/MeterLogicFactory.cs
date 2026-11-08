using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Meter of a consumer or producer: power (kW) and/or absolute reading (kWh) in, power and energy today / this
    /// month / total out. Without a power input the power comes from the readings.
    /// Was the empty "Metering" placeholder, it keeps that template id so existing instances get the interfaces.
    /// </summary>
    public class MeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputPower = new Guid("59d50bb0-e86b-430e-8839-cd11f5af6366");
        public static readonly Guid InputReading = new Guid("eb51326e-0bf5-454a-8a58-cde3a3529cc2");

        public static readonly Guid OutputPower = new Guid("331a0d69-5ae1-4c7d-b70b-b4bbe93cb782");
        public static readonly Guid[] OutputCounters = { new Guid("231f3202-3c34-478c-8492-2cf71d7679b3"), new Guid("219a6e9d-2129-48a0-b76e-e61e5fe6cde5"), new Guid("18a7c0a6-78bf-4d40-a061-af091169c8d6") };

        public static readonly Guid ParamPowerFactor = new Guid("4c4e037c-b3eb-4c3c-aba3-95596fa458c8");
        public static readonly Guid ParamReadingFactor = new Guid("d93b46fe-035b-427e-bd8a-22ffa1b5ac6e");

        public override string LogicName => "Metering.Meter";
        public override Guid LogicGuid => new Guid("6b395c46-ee29-4f06-b416-c9367c41fc85");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER";
        protected override string Key => "metering.meter";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            var sort = 0;
            Input(factory, InputPower, "POWER", "power", ++sort, Common);
            Input(factory, InputReading, "READING", "reading", ++sort, Common);

            sort = 0;
            Output(factory, OutputPower, "POWER", "power", ++sort, Common, RuleInterfaceType.Status);
            Counters(factory, OutputCounters, "", "", ref sort, Common);

            sort = 0;
            Param(factory, ParamPowerFactor, "POWER_FACTOR", "powerFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
            Param(factory, ParamReadingFactor, "READING_FACTOR", "readingFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new MeterRule(context);
    }

    public class MeterRule : MeterRuleBase
    {
        internal const string Energy = "energy";

        private readonly RateEstimator _rate = new();
        private double _powerFactor = 1;
        private double _readingFactor = 1;
        private double? _power;

        public MeterRule(ILogicContext context) : base(context, Energy)
        {
        }

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            var number = ToDouble(value);
            if (template == MeterLogicFactory.InputPower)
            {
                if (number is not { } p)
                {
                    return false;
                }
                _power = p * _powerFactor;
                Registers[Energy].SetPower(_power.Value, now);
                return true;
            }
            if (template == MeterLogicFactory.InputReading)
            {
                if (number is not { } r)
                {
                    return false;
                }
                _rate.Add(Registers[Energy].SetReading(r * _readingFactor, now), now);
                return true;
            }
            return false;
        }

        protected override void Resume(DateTimeOffset now)
        {
            if (_power is { } p)
            {
                Registers[Energy].SetPower(p, now);
            }
        }

        protected override void OnParameter(Guid template, object value)
        {
            if (template == MeterLogicFactory.ParamPowerFactor) _powerFactor = Factor(value);
            else if (template == MeterLogicFactory.ParamReadingFactor) _readingFactor = Factor(value);
        }

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            var energy = Registers[Energy];
            yield return (MeterLogicFactory.OutputPower, _power ?? _rate.Current(now));
            yield return (MeterLogicFactory.OutputCounters[0], energy.Today);
            yield return (MeterLogicFactory.OutputCounters[1], energy.Month);
            yield return (MeterLogicFactory.OutputCounters[2], energy.Total);
        }
    }
}
