using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Floor
{
    public class FloorLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("746e1068-232f-4ec1-b019-7e6bae074a44");
        public static readonly Guid RuleOutput = new Guid("a44cb7b6-13ae-4394-b9dd-4b4abd595766");

        public override string LogicName => "Math.Floor";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("aa351d39-443e-425b-8a50-dde82ce4ba58");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.FLOOR.NAME", "MATH.FLOOR.DESCRIPTION",
                "math.floor", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.FLOOR.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.FLOOR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FloorRule(context);
        }
    }
}
