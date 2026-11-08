using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Meter counting both ways, typically the grid: signed power (+ import, − export) or import and export power, and/or
    /// import and export readings. Out: signed power, import / export power and their energy today / month / total.
    /// </summary>
    public class BidirectionalMeterLogicFactory : MeterFactoryBase
    {
        public static readonly Guid InputPower = new Guid("29d1dda6-1386-4976-b80e-cfc0aea12eb8");
        public static readonly Guid InputImportPower = new Guid("5a6e9a4b-6275-4f6e-9424-b8067ad19d5a");
        public static readonly Guid InputExportPower = new Guid("be2e3889-0db3-48f2-82d9-690910ae80cc");
        public static readonly Guid InputImportReading = new Guid("06ce31e0-6335-4e37-8010-a0e862a57237");
        public static readonly Guid InputExportReading = new Guid("53661930-50a4-4365-8b86-4ed24aa076b7");

        public static readonly Guid OutputPower = new Guid("07fda9bb-1e55-4127-a96c-9878d868fb60");
        public static readonly Guid OutputImportPower = new Guid("76e31b46-534b-487a-b52d-06a7e5ceb258");
        public static readonly Guid OutputExportPower = new Guid("32098864-e04b-4cda-99c2-2936034cb37b");
        public static readonly Guid[] OutputImportCounters = { new Guid("3f10d2f4-d327-4824-aa16-1f9ba7dd4119"), new Guid("796bcbfc-c416-43ee-a093-516b5a43751c"), new Guid("9f4e6c50-2406-4749-b896-28123d29d493") };
        public static readonly Guid[] OutputExportCounters = { new Guid("9d3c0174-f37f-4392-80ab-08ce2fb5f97e"), new Guid("37ee2c1c-3473-476e-9747-4264785791bc"), new Guid("c6d8aa5d-ccfc-4630-bc86-2f3d514dc3b3") };

        public static readonly Guid ParamInvert = new Guid("a338555b-edcc-4c3f-a381-2c8554267cdc");
        public static readonly Guid ParamPowerFactor = new Guid("bafec047-f04e-45cf-856b-8049ec12f88f");
        public static readonly Guid ParamReadingFactor = new Guid("326210aa-bffb-46a4-980b-b83079a6ed3d");

        public override string LogicName => "Metering.MeterBidirectional";
        public override Guid LogicGuid => new Guid("d49f30a0-1ba2-4dfc-92ef-8720ef9fa1bd");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        protected override string Section => "METER_BIDIRECTIONAL";
        protected override string Key => "metering.meter_bidirectional";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            CreateTemplate(factory);

            var sort = 0;
            Input(factory, InputPower, "POWER", "power", ++sort);
            Input(factory, InputImportPower, "IMPORT_POWER", "importPower", ++sort);
            Input(factory, InputExportPower, "EXPORT_POWER", "exportPower", ++sort);
            Input(factory, InputImportReading, "IMPORT_READING", "importReading", ++sort);
            Input(factory, InputExportReading, "EXPORT_READING", "exportReading", ++sort);

            sort = 0;
            Output(factory, OutputPower, "POWER", "power", ++sort, null, RuleInterfaceType.Status);
            Output(factory, OutputImportPower, "IMPORT_POWER", "importPower", ++sort);
            Output(factory, OutputExportPower, "EXPORT_POWER", "exportPower", ++sort);
            Counters(factory, OutputImportCounters, "IMPORT_", "import", ref sort);
            Counters(factory, OutputExportCounters, "EXPORT_", "export", ref sort);

            sort = 0;
            Param(factory, ParamInvert, "INVERT", "invert", ++sort, RuleInterfaceParameterDataType.Bool, false, Common);
            Param(factory, ParamPowerFactor, "POWER_FACTOR", "powerFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
            Param(factory, ParamReadingFactor, "READING_FACTOR", "readingFactor", ++sort, RuleInterfaceParameterDataType.Double, 1d, Common);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new BidirectionalMeterRule(context);
    }

    public class BidirectionalMeterRule : MeterRuleBase
    {
        internal const string Import = "import";
        internal const string Export = "export";

        private readonly RateEstimator _importRate = new();
        private readonly RateEstimator _exportRate = new();
        private bool _invert;
        private double _powerFactor = 1;
        private double _readingFactor = 1;
        private double? _import;
        private double? _export;

        public BidirectionalMeterRule(ILogicContext context) : base(context, Import, Export)
        {
        }

        protected override bool OnInput(Guid template, object value, DateTimeOffset now)
        {
            if (ToDouble(value) is not { } number)
            {
                return false;
            }

            if (template == BidirectionalMeterLogicFactory.InputPower)
            {
                var signed = number * _powerFactor * (_invert ? -1 : 1);
                _import = Math.Max(signed, 0);
                _export = Math.Max(-signed, 0);
            }
            else if (template == BidirectionalMeterLogicFactory.InputImportPower)
            {
                _import = Math.Max(number * _powerFactor, 0);
            }
            else if (template == BidirectionalMeterLogicFactory.InputExportPower)
            {
                _export = Math.Max(number * _powerFactor, 0);
            }
            else if (template == BidirectionalMeterLogicFactory.InputImportReading)
            {
                _importRate.Add(Registers[Import].SetReading(number * _readingFactor, now), now);
                return true;
            }
            else if (template == BidirectionalMeterLogicFactory.InputExportReading)
            {
                _exportRate.Add(Registers[Export].SetReading(number * _readingFactor, now), now);
                return true;
            }
            else
            {
                return false;
            }

            Registers[Import].SetPower(_import ?? 0, now);
            Registers[Export].SetPower(_export ?? 0, now);
            return true;
        }

        protected override void Resume(DateTimeOffset now)
        {
            Registers[Import].SetPower(_import ?? 0, now);
            Registers[Export].SetPower(_export ?? 0, now);
        }

        protected override void OnParameter(Guid template, object value)
        {
            if (template == BidirectionalMeterLogicFactory.ParamInvert) _invert = ToBool(value);
            else if (template == BidirectionalMeterLogicFactory.ParamPowerFactor) _powerFactor = Factor(value);
            else if (template == BidirectionalMeterLogicFactory.ParamReadingFactor) _readingFactor = Factor(value);
        }

        protected override IEnumerable<(Guid Template, double? Value)> CurrentValues(DateTimeOffset now)
        {
            var hasPower = _import.HasValue || _export.HasValue;
            var import = hasPower ? _import : _importRate.Current(now);
            var export = hasPower ? _export : _exportRate.Current(now);

            yield return (BidirectionalMeterLogicFactory.OutputPower, import.HasValue || export.HasValue ? (import ?? 0) - (export ?? 0) : null);
            yield return (BidirectionalMeterLogicFactory.OutputImportPower, import);
            yield return (BidirectionalMeterLogicFactory.OutputExportPower, export);

            var i = Registers[Import];
            yield return (BidirectionalMeterLogicFactory.OutputImportCounters[0], i.Today);
            yield return (BidirectionalMeterLogicFactory.OutputImportCounters[1], i.Month);
            yield return (BidirectionalMeterLogicFactory.OutputImportCounters[2], i.Total);
            var e = Registers[Export];
            yield return (BidirectionalMeterLogicFactory.OutputExportCounters[0], e.Today);
            yield return (BidirectionalMeterLogicFactory.OutputExportCounters[1], e.Month);
            yield return (BidirectionalMeterLogicFactory.OutputExportCounters[2], e.Total);
        }
    }
}
