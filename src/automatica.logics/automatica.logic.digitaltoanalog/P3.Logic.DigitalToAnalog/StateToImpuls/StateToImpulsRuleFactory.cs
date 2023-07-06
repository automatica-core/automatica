using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.DigitalToAnalog.StateToImpuls
{
    public class StateToImpulsLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput = new Guid("6a2dd6e5-3d36-41ff-bb2e-ad04060519dc");
        public static readonly Guid RuleOutput = new Guid("32d1bc7d-14e4-4033-a5bb-40da0db6adad");

        public static readonly Guid DelayParameter = new Guid("fe035850-de9a-4648-ac7e-57a6ef20cd89");

        public override Version LogicVersion => new Version(1, 0, 0, 3);

        public override string LogicName => "StatusToImpuls";
        public override Guid LogicGuid => new Guid("088dfa08-6487-4f5e-8d77-2ce4402ba661");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.NAME", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DESCRIPTION", "digital_to_analog.state_to_imopuls", "DIGITAL_TO_ANALOG.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "I", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(DelayParameter, "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DELAY.NAME", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DELAY.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, 1000);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StateToImpulsRule(context);
        }
    }
}
