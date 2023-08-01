using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.ExclusiveOr
{
    public class ExclusiveOrLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("c362dd5d-6a33-4fc5-9d97-12ab77d03ce8");
        public static readonly Guid RuleInput2 = new Guid("f91adad2-11a0-415f-9a9f-6712f5a0b361");


        public static readonly Guid RuleOutput = new Guid("85248065-c7c0-458e-af4b-488a287bed9a");

        public override string LogicName => "Logic.ExclusiveOr";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("0f6ddef4-0522-4181-ab0b-32b4e68ea390");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.EXCLUSIVE_OR.NAME", "LOGIC.EXCLUSIVE_OR.DESCRIPTION", "logic.exclusive_or", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.EXCLUSIVE_OR.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "LOGIC.EXCLUSIVE_OR.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.EXCLUSIVE_OR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ExclusiveOrRule(context);
        }
    }
}
