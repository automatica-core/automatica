using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.DigitalToAnalog.Trigger
{
    public class TriggerLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput = new Guid("5e8b998d-6e89-4ae9-bed2-a2045ae2ca9d");
        public static readonly Guid RuleValueInput = new Guid("a3068c7e-ca1e-4de9-b25e-ceaaba33e37b");

        public static readonly Guid RuleOutput = new Guid("48f1a2d7-2a9d-40a5-ae0d-064538014f37");

        public static readonly Guid DelayParameter = new Guid("62e4a6c7-9748-4339-b085-59b7c8969ca2");
        public static readonly Guid ValueParameter = new Guid("95af86b8-cff1-4039-8849-bed814fdf468");

        public override Version LogicVersion => new Version(1, 0, 0, 3);

        public override string LogicName => "Trigger";
        public override Guid LogicGuid => new Guid("94a9071e-f0b1-4e83-86d2-ea0a0b565e22");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "DIGITAL_TO_ANALOG.TRIGGER.NAME", "DIGITAL_TO_ANALOG.TRIGGER.DESCRIPTION", "digital_to_analog.trigger", "DIGITAL_TO_ANALOG.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "I", "DIGITAL_TO_ANALOG.TRIGGER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleValueInput, "V", "DIGITAL_TO_ANALOG.TRIGGER.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "DIGITAL_TO_ANALOG.TRIGGER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(DelayParameter, "DIGITAL_TO_ANALOG.TRIGGER.DELAY.NAME", "DIGITAL_TO_ANALOG.TRIGGER.DELAY.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, 0);
            factory.CreateParameterLogicInterfaceTemplate(ValueParameter, "DIGITAL_TO_ANALOG.TRIGGER.VALUE_PARAM.NAME", "DIGITAL_TO_ANALOG.TRIGGER.VALUE_PARAM.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Text, "");
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TriggerRule(context);
        }
    }
}
