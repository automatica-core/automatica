using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Addition
{
    public class AdditionLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("e697bef4-ddca-45fb-ae83-6b3ad1c840e4");
        public static readonly Guid RuleInput2 = new Guid("fda9e783-252e-469d-8065-bb2e99aa5fe5");
        public static readonly Guid RuleInput3 = new Guid("cd594053-8c6b-444e-ac2f-593ba5d50ca5");
        public static readonly Guid RuleInput4 = new Guid("536e2e6b-59f9-483b-93ca-be1b365ecbc6");


        public static readonly Guid RuleOutput = new Guid("7a9d63b2-050f-4532-8ba9-188a308b4a9a");

        public override string LogicName => "Math.Add";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("010669c3-11db-4e99-86a9-ce678779d3a0");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.ADD.NAME", "MATH.ADD.DESCRIPTION",
                "math.add", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.ADD.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.ADD.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleInput3, "I3", "MATH.ADD.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleInput4, "I4", "MATH.ADD.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.ADD.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AdditionRule(context);
        }
    }
}
