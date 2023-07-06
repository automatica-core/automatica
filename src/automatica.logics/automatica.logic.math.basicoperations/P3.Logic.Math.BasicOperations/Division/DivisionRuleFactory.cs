using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Division
{
    public class DivisionLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("f1facd80-cdab-4487-97c4-e2477444191c");
        public static readonly Guid RuleInput2 = new Guid("75b5ee1e-8f3c-492d-b196-325bbaa88022");


        public static readonly Guid RuleOutput = new Guid("538c6157-2c0a-4f90-ba62-57038abac838");

        public override string LogicName => "Math.Divison";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("151ee36d-680a-4978-b51f-a099e8f94895");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.DIVISION.NAME", "MATH.DIVIDE.DESCRIPTION",
                "math.division", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.DIVISION.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.DIVISION.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.DIVISION.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new DivisionRule(context);
        }
    }
}
