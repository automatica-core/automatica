using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Meter of a storage (battery): signed power (+ charge, − discharge), state of charge and/or charge and discharge
    /// readings. Out: signed power, charge / discharge power, state of charge, stored energy (with the capacity) and
    /// charged / discharged energy today / month / total.
    /// </summary>
    public class StorageMeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputPower = new Guid("4bcbc7ad-ae67-4020-a388-c9752ff87438");
        public static readonly Guid InputSoc = new Guid("be034abb-5cc8-46c1-9be7-179963828176");
        public static readonly Guid InputChargeReading = new Guid("8a0c3c20-2f21-4898-9029-23952948b65f");
        public static readonly Guid InputDischargeReading = new Guid("ba7f50d9-b04f-4ad0-93f8-7422f37a32d2");

        public static readonly Guid OutputPower = new Guid("00ad3c57-952f-4e4f-8260-62d621a47a4d");
        public static readonly Guid OutputChargePower = new Guid("e4f224e4-8055-4cc6-bbd4-35cad02ab049");
        public static readonly Guid OutputDischargePower = new Guid("013bfda6-9a47-4315-92ca-773382be2a2e");
        public static readonly Guid OutputSoc = new Guid("c73ea5c0-2af7-4d48-913a-954a19a429bd");
        public static readonly Guid OutputStored = new Guid("afff25d3-839c-4ff5-9cea-14daa59d2adc");
        public static readonly Guid[] OutputChargeCounters = { new Guid("54751285-2734-466b-a60a-7e2587ef829b"), new Guid("10e6279d-dcef-479d-9927-5ed38a62df6c"), new Guid("e0f22c45-b027-413b-901a-062c9d8bc41b") };
        public static readonly Guid[] OutputDischargeCounters = { new Guid("275f4e94-a218-42b6-9757-2567594c2d37"), new Guid("bf67fccb-e052-4eb5-b157-3546c6d412fc"), new Guid("abd0aaf2-b8f5-4368-bc68-eaf761ef43f9") };

        public static readonly Guid ParamCapacity = new Guid("641968b5-7495-41d7-9d93-8f36c8b9dc19");
        public static readonly Guid ParamInvert = new Guid("9d657dcc-5ca5-4795-b05d-7bb4877eae4b");
        public static readonly Guid ParamPowerFactor = new Guid("0773a5a6-be2a-4f45-9d68-a58b9320bf55");
        public static readonly Guid ParamReadingFactor = new Guid("b20313e2-ec53-4708-ad16-25ec63cb81c1");

        public override string LogicName => "Metering.MeterStorage";
        public override Guid LogicGuid => new Guid("96a9ad18-726e-49e1-ae2e-95f1431a0d0f");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER_STORAGE";
        protected override string Key => "metering.meter_storage";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            var sort = 0;
            Input(factory, InputPower, "POWER", "power", ++sort);
            Input(factory, InputSoc, "SOC", "soc", ++sort);
            Input(factory, InputChargeReading, "CHARGE_READING", "chargeReading", ++sort);
            Input(factory, InputDischargeReading, "DISCHARGE_READING", "dischargeReading", ++sort);

            sort = 0;
            Output(factory, OutputPower, "POWER", "power", ++sort, null, RuleInterfaceType.Status);
            Output(factory, OutputChargePower, "CHARGE_POWER", "chargePower", ++sort);
            Output(factory, OutputDischargePower, "DISCHARGE_POWER", "dischargePower", ++sort);
            Output(factory, OutputSoc, "SOC", "soc", ++sort);
            Output(factory, OutputStored, "STORED", "stored", ++sort);
            Counters(factory, OutputChargeCounters, "CHARGE_", "charge", ref sort);
            Counters(factory, OutputDischargeCounters, "DISCHARGE_", "discharge", ref sort);

            sort = 0;
            Param(factory, ParamCapacity, "CAPACITY", "capacity", ++sort, RuleInterfaceParameterDataType.Double, 0d);
            Param(factory, ParamInvert, "INVERT", "invert", ++sort, RuleInterfaceParameterDataType.Bool, false, Common);
            Param(factory, ParamPowerFactor, "POWER_FACTOR", "powerFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
            Param(factory, ParamReadingFactor, "READING_FACTOR", "readingFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new StorageMeterRule(context);
    }

    public class StorageMeterRule : MeterRuleBase
    {
        internal const string Charge = "charge";
        internal const string Discharge = "discharge";

        private readonly RateEstimator _chargeRate = new();
        private readonly RateEstimator _dischargeRate = new();
        private bool _invert;
        private double _capacity;
        private double _powerFactor = 1;
        private double _readingFactor = 1;
        private double? _power;
        private double? _soc;

        public StorageMeterRule(ILogicContext context) : base(context, Charge, Discharge)
        {
        }

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            if (ToDouble(value) is not { } number)
            {
                return false;
            }

            if (template == StorageMeterLogicFactory.InputPower)
            {
                _power = number * _powerFactor * (_invert ? -1 : 1);
                Registers[Charge].SetPower(Math.Max(_power.Value, 0), now);
                Registers[Discharge].SetPower(Math.Max(-_power.Value, 0), now);
            }
            else if (template == StorageMeterLogicFactory.InputSoc)
            {
                _soc = Math.Clamp(number, 0, 100);
            }
            else if (template == StorageMeterLogicFactory.InputChargeReading)
            {
                _chargeRate.Add(Registers[Charge].SetReading(number * _readingFactor, now), now);
            }
            else if (template == StorageMeterLogicFactory.InputDischargeReading)
            {
                _dischargeRate.Add(Registers[Discharge].SetReading(number * _readingFactor, now), now);
            }
            else
            {
                return false;
            }
            return true;
        }

        protected override void Resume(DateTimeOffset now)
        {
            if (_power is { } p)
            {
                Registers[Charge].SetPower(Math.Max(p, 0), now);
                Registers[Discharge].SetPower(Math.Max(-p, 0), now);
            }
        }

        protected override void OnParameter(Guid template, object value)
        {
            if (template == StorageMeterLogicFactory.ParamCapacity) _capacity = Math.Max(ToDouble(value) ?? 0, 0);
            else if (template == StorageMeterLogicFactory.ParamInvert) _invert = ToBool(value);
            else if (template == StorageMeterLogicFactory.ParamPowerFactor) _powerFactor = Factor(value);
            else if (template == StorageMeterLogicFactory.ParamReadingFactor) _readingFactor = Factor(value);
        }

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            double? charge, discharge;
            if (_power is { } p)
            {
                charge = Math.Max(p, 0);
                discharge = Math.Max(-p, 0);
            }
            else
            {
                charge = _chargeRate.Current(now);
                discharge = _dischargeRate.Current(now);
            }

            yield return (StorageMeterLogicFactory.OutputPower, charge.HasValue || discharge.HasValue ? (charge ?? 0) - (discharge ?? 0) : null);
            yield return (StorageMeterLogicFactory.OutputChargePower, charge);
            yield return (StorageMeterLogicFactory.OutputDischargePower, discharge);
            yield return (StorageMeterLogicFactory.OutputSoc, _soc);
            yield return (StorageMeterLogicFactory.OutputStored, _soc.HasValue && _capacity > 0 ? _soc.Value / 100 * _capacity : null);

            var c = Registers[Charge];
            yield return (StorageMeterLogicFactory.OutputChargeCounters[0], c.Today);
            yield return (StorageMeterLogicFactory.OutputChargeCounters[1], c.Month);
            yield return (StorageMeterLogicFactory.OutputChargeCounters[2], c.Total);
            var d = Registers[Discharge];
            yield return (StorageMeterLogicFactory.OutputDischargeCounters[0], d.Today);
            yield return (StorageMeterLogicFactory.OutputDischargeCounters[1], d.Month);
            yield return (StorageMeterLogicFactory.OutputDischargeCounters[2], d.Total);
        }
    }
}
