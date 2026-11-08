using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.ShiftRegister
{
    public class ShiftRegisterLogicFactory : LogicFactory
    {
        internal const int DefaultBit = 8;

        public static readonly Guid RuleTrigger = new Guid("67a91062-e99c-4f06-a94b-bbf6e86ccf46");
        public static readonly Guid RuleData = new Guid("da0d5b68-7232-4ccc-b381-973e9a6cf18f");
        public static readonly Guid RuleDirection = new Guid("a9339a32-3222-4fae-8ba0-fbc88803dc6d");
        public static readonly Guid RuleOutput = new Guid("c8a3e092-ea67-49e0-b81d-bd678d214fa0");
        public static readonly Guid RuleParamBit = new Guid("4ac98cb6-e779-4c8d-be86-1156aad563a5");

        public override string LogicName => "Logic.ShiftRegister";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("6ad6fa65-d166-4c6f-9dee-a7c767678a73");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.SHIFT_REGISTER.NAME", "LOGIC.SHIFT_REGISTER.DESCRIPTION", "logic.shift_register", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "Tr", "LOGIC.SHIFT_REGISTER.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleData, "D", "LOGIC.SHIFT_REGISTER.DATA.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleDirection, "Dir", "LOGIC.SHIFT_REGISTER.DIRECTION.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.SHIFT_REGISTER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamBit, "LOGIC.SHIFT_REGISTER.BIT.NAME", "LOGIC.SHIFT_REGISTER.BIT.DESCRIPTION", "rb", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, DefaultBit);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ShiftRegisterRule(context);
        }
    }
}
