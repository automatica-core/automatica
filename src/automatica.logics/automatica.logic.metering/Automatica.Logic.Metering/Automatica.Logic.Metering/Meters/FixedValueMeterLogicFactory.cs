using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Meter of a device with a known, constant power: on/off in, power (the parameter while on) and energy
    /// (power × on-time) today / month / total out.
    /// </summary>
    public class FixedValueMeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputOn = new Guid("bc573c8c-c768-4593-b68b-e726de503873");

        public static readonly Guid OutputPower = new Guid("8efc5da8-76c6-4222-a40c-d7bbcf9531ac");
        public static readonly Guid[] OutputCounters = { new Guid("5e62ceca-e615-4c85-b7d0-3f944d297f43"), new Guid("ac915edf-3d34-4239-9b70-adafc98b2e56"), new Guid("95327e6a-f597-4c39-9124-25a08f228962") };

        public static readonly Guid ParamPower = new Guid("6987f8b0-18b2-4d3a-a06a-1ba51168f668");

        internal const double DefaultPower = 1;

        public override string LogicName => "Metering.MeterFixedValue";
        public override Guid LogicGuid => new Guid("1fdccdbe-a952-4527-ac0b-1aec0e572510");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER_FIXED";
        protected override string Key => "metering.meter_fixed";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            Input(factory, InputOn, "ON", "on", 1);

            var sort = 0;
            Output(factory, OutputPower, "POWER", "power", ++sort, Common, RuleInterfaceType.Status);
            Counters(factory, OutputCounters, "", "", ref sort, Common);

            Param(factory, ParamPower, "POWER", "ratedPower", 1, RuleInterfaceParameterDataType.Double, DefaultPower);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new FixedValueMeterRule(context);
    }

    public class FixedValueMeterRule : MeterRuleBase
    {
        internal const string Energy = "energy";

        private double _ratedPower = FixedValueMeterLogicFactory.DefaultPower;
        private bool? _on;

        public FixedValueMeterRule(ILogicContext context) : base(context, Energy)
        {
        }

        private double Power => _on == true ? _ratedPower : 0;

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            if (template != FixedValueMeterLogicFactory.InputOn)
            {
                return false;
            }
            _on = ToBool(value);
            Registers[Energy].SetPower(Power, now);
            return true;
        }

        protected override void Resume(DateTimeOffset now) => Registers[Energy].SetPower(Power, now);

        protected override void OnParameter(Guid template, object value)
        {
            if (template != FixedValueMeterLogicFactory.ParamPower)
            {
                return;
            }
            _ratedPower = Math.Max(ToDouble(value) ?? FixedValueMeterLogicFactory.DefaultPower, 0);
            if (_on == true)
            {
                Registers[Energy].SetPower(Power, Now);
            }
        }

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            var energy = Registers[Energy];
            yield return (FixedValueMeterLogicFactory.OutputPower, _on.HasValue ? Power : null);
            yield return (FixedValueMeterLogicFactory.OutputCounters[0], energy.Today);
            yield return (FixedValueMeterLogicFactory.OutputCounters[1], energy.Month);
            yield return (FixedValueMeterLogicFactory.OutputCounters[2], energy.Total);
        }
    }
}
