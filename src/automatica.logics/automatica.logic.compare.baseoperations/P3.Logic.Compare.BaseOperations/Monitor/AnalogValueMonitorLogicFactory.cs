using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Threshold;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Compare.BaseOperations.Monitor
{
    public class AnalogValueMonitorLogicFactory : LogicFactory
    {
        internal const double DefaultUpper = 7;
        internal const double DefaultLower = 3;

        public static readonly Guid RuleValue = new Guid("940a6781-4a6e-4776-a179-607e6b944100");
        public static readonly Guid RuleOff = new Guid("f345d552-5621-4a88-906e-34a37e6d1c2a");
        public static readonly Guid RuleOutput = new Guid("feeaf949-c907-4d7e-b7da-c730605e6c65");
        public static readonly Guid RuleParamUpper = new Guid("e8a595cf-6e4f-4c87-b590-fdc2ff27884f");
        public static readonly Guid RuleParamLower = new Guid("b0b49e2c-4160-4ed6-9a20-3627819293d8");

        public override string LogicName => "Compare.AnalogValueMonitor";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("fabfe200-3e53-4eca-8a5a-b9f6d184e02e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.VALUE_MONITOR.NAME", "COMPARE.VALUE_MONITOR.DESCRIPTION", "compare.value_monitor", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "COMPARE.VALUE_MONITOR.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "COMPARE.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Te", "COMPARE.VALUE_MONITOR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamUpper, "COMPARE.VALUE_MONITOR.UPPER.NAME", "COMPARE.VALUE_MONITOR.UPPER.DESCRIPTION", "tu", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultUpper);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLower, "COMPARE.VALUE_MONITOR.LOWER.NAME", "COMPARE.VALUE_MONITOR.LOWER.DESCRIPTION", "tl", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultLower);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AnalogValueMonitorRule(context);
        }
    }

    /// <summary>
    /// Analog value monitor (Loxone "Analogue Value Monitor"): Te is 1 while V is above the upper or below the lower threshold,
    /// i.e. outside the tolerance band. Off = 1 sends 0 and locks the block until Off is 0 again.
    /// </summary>
    public class AnalogValueMonitorRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal double Upper = AnalogValueMonitorLogicFactory.DefaultUpper;
        internal double Lower = AnalogValueMonitorLogicFactory.DefaultLower;

        private bool _locked;

        public AnalogValueMonitorRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == AnalogValueMonitorLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == AnalogValueMonitorLogicFactory.RuleParamUpper)
            {
                Upper = ThresholdSwitchRule.ToDouble(value) ?? AnalogValueMonitorLogicFactory.DefaultUpper;
            }
            else if (instance.This2RuleInterfaceTemplate == AnalogValueMonitorLogicFactory.RuleParamLower)
            {
                Lower = ThresholdSwitchRule.ToDouble(value) ?? AnalogValueMonitorLogicFactory.DefaultLower;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var number = ThresholdSwitchRule.ToDouble(value);

            if (template == AnalogValueMonitorLogicFactory.RuleOff)
            {
                _locked = number.HasValue && number.Value != 0;
                return _locked ? SingleOutputChanged(new LogicOutputChanged(_output, false)) : new List<ILogicOutputChanged>();
            }

            if (template != AnalogValueMonitorLogicFactory.RuleValue || !number.HasValue || _locked)
            {
                return new List<ILogicOutputChanged>();
            }

            var low = System.Math.Min(Lower, Upper);
            var high = System.Math.Max(Lower, Upper);
            return SingleOutputChanged(new LogicOutputChanged(_output, number.Value > high || number.Value < low));
        }
    }
}
