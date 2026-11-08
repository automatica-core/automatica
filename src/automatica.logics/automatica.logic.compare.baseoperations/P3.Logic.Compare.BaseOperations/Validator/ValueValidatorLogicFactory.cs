using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Compare.BaseOperations.Validator
{
    public class ValueValidatorLogicFactory : LogicFactory
    {
        internal const double DefaultChangeInterval = 3600;
        internal const double DefaultMinimum = -1000;
        internal const double DefaultMaximum = 1000;
        internal const double DefaultDelay = 0;

        public static readonly Guid RuleValue = new Guid("a9a43392-abc6-486c-a8ad-ca9dd9d5ebce");
        public static readonly Guid RuleEnable = new Guid("c8a21cde-aaaf-46da-a6f9-80d4dda6f24c");
        public static readonly Guid RuleOutput = new Guid("61857cef-bfc7-4ac0-b15f-538fcd561789");
        public static readonly Guid RuleError = new Guid("7a7c9b53-d3b4-40db-8aec-225162913aa5");
        public static readonly Guid RuleParamChangeInterval = new Guid("7baeedb0-7290-4ca9-94ee-ec1f5efa0451");
        public static readonly Guid RuleParamMinimum = new Guid("0eef8c42-e40e-4020-980a-f755202e3fbb");
        public static readonly Guid RuleParamMaximum = new Guid("b6c86204-fe94-4757-a3af-1ff8bc718d5c");
        public static readonly Guid RuleParamDelay = new Guid("5269804f-d069-4526-bb21-ec8ba647361c");

        public override string LogicName => "Compare.ValueValidator";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("33cad4c1-7391-46e2-be5d-00c14cee035a");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.VALUE_VALIDATOR.NAME", "COMPARE.VALUE_VALIDATOR.DESCRIPTION", "compare.value_validator", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "COMPARE.VALUE_VALIDATOR.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleEnable, "En", "COMPARE.VALUE_VALIDATOR.ENABLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "V", "COMPARE.VALUE_VALIDATOR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleError, "E", "COMPARE.VALUE_VALIDATOR.ERROR.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamChangeInterval, "COMPARE.VALUE_VALIDATOR.CHANGE_INTERVAL.NAME", "COMPARE.VALUE_VALIDATOR.CHANGE_INTERVAL.DESCRIPTION", "tmc", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultChangeInterval);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMinimum, "COMPARE.VALUE_VALIDATOR.MINIMUM.NAME", "COMPARE.VALUE_VALIDATOR.MINIMUM.DESCRIPTION", "min", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultMinimum);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMaximum, "COMPARE.VALUE_VALIDATOR.MAXIMUM.NAME", "COMPARE.VALUE_VALIDATOR.MAXIMUM.DESCRIPTION", "max", LogicGuid, 3, RuleInterfaceParameterDataType.Double, DefaultMaximum);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "COMPARE.VALUE_VALIDATOR.DELAY.NAME", "COMPARE.VALUE_VALIDATOR.DELAY.DESCRIPTION", "d", LogicGuid, 4, RuleInterfaceParameterDataType.Double, DefaultDelay);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ValueValidatorRule(context);
        }
    }
}
