using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Threshold;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Compare.BaseOperations.Comparator
{
    public class ComparatorLogicFactory : LogicFactory
    {
        internal const double DefaultOn = 5;
        internal const double DefaultOff = 1;

        public static readonly Guid RuleValue1 = new Guid("ddd37664-229b-41e1-88aa-7f756f160089");
        public static readonly Guid RuleValue2 = new Guid("f8e2201f-b75c-45e4-a118-542115def55a");
        public static readonly Guid RuleOutput = new Guid("4db751b7-e134-4bb9-8f50-584f7fa2accc");
        public static readonly Guid RuleParamOn = new Guid("1a6ce377-e892-429a-aa7e-815c80a1c74f");
        public static readonly Guid RuleParamOff = new Guid("4d2987f5-ae01-448a-8e58-6e447dcab0de");

        public override string LogicName => "Compare.Comparator";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("0606a49a-cfac-494e-b37f-7eb30c0d607f");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.COMPARATOR.NAME", "COMPARE.COMPARATOR.DESCRIPTION", "compare.comparator", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue1, "V1", "COMPARE.COMPARATOR.VALUE1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleValue2, "V2", "COMPARE.COMPARATOR.VALUE2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "De", "COMPARE.COMPARATOR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamOn, "COMPARE.COMPARATOR.ON.NAME", "COMPARE.COMPARATOR.ON.DESCRIPTION", "von", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOn);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOff, "COMPARE.COMPARATOR.OFF.NAME", "COMPARE.COMPARATOR.OFF.DESCRIPTION", "voff", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultOff);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ComparatorRule(context);
        }
    }

    /// <summary>
    /// Comparator (Loxone "Comparator"): De switches on when V1 - V2 rises above the on value and off when it falls below
    /// the off value; in between it keeps its state (hysteresis), e.g. collector minus storage temperature for a solar pump.
    /// Nothing is sent before both inputs have a value.
    /// </summary>
    public class ComparatorRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal double On = ComparatorLogicFactory.DefaultOn;
        internal double Off = ComparatorLogicFactory.DefaultOff;

        private double? _v1;
        private double? _v2;
        private bool _state;

        public ComparatorRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ComparatorLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == ComparatorLogicFactory.RuleParamOn)
            {
                On = ThresholdSwitchRule.ToDouble(value) ?? ComparatorLogicFactory.DefaultOn;
            }
            else if (instance.This2RuleInterfaceTemplate == ComparatorLogicFactory.RuleParamOff)
            {
                Off = ThresholdSwitchRule.ToDouble(value) ?? ComparatorLogicFactory.DefaultOff;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var number = ThresholdSwitchRule.ToDouble(value);
            if (!number.HasValue)
            {
                return new List<ILogicOutputChanged>();
            }

            var template = instance.This2RuleInterfaceTemplate;
            if (template == ComparatorLogicFactory.RuleValue1)
            {
                _v1 = number;
            }
            else if (template == ComparatorLogicFactory.RuleValue2)
            {
                _v2 = number;
            }
            else
            {
                return new List<ILogicOutputChanged>();
            }

            if (!_v1.HasValue || !_v2.HasValue)
            {
                return new List<ILogicOutputChanged>();
            }

            var difference = _v1.Value - _v2.Value;
            if (difference > On)
            {
                _state = true;
            }
            else if (difference < Off)
            {
                _state = false;
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, _state));
        }
    }
}
