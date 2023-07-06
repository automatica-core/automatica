using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Multiply
{
    public class MultiplyLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("d1b17504-897d-4707-87e5-3d29d1bf75f5");
        public static readonly Guid RuleInput2 = new Guid("383facb9-fe8c-4ffc-a3d7-593feddb4de9");


        public static readonly Guid RuleOutput = new Guid("1b8dac1d-e812-4938-9640-db0a6e29d225");

        public override string LogicName => "Math.Multiply";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("4856b924-459f-4190-8ff3-8db9b7cd992e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.MULTIPLY.NAME", "MATH.MULTIPLY.DESCRIPTION",
                "math.multiply", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.MULTIPLY.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.MULTIPLY.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.MULTIPLY.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MultiplyRule(context);
        }
    }
}
