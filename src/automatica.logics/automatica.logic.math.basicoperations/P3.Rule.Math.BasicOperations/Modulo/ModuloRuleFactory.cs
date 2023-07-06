using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Modulo
{
    public class ModuloLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("dfd56f7a-c75a-4a09-9702-fe4e75255bb1");
        public static readonly Guid RuleInput2 = new Guid("07847d6a-a00a-464f-be4f-679d9b667531");


        public static readonly Guid RuleOutput1 = new Guid("e4ef5af8-792e-45c9-ab9d-77eaf225ee68");
        public static readonly Guid RuleOutput2 = new Guid("f9aaa35e-d494-470c-83ad-c0173682eb36");

        public override string LogicName => "Math.Modulo";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("1aae846f-79df-45da-a671-f71b6aafa285");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.MODULO.NAME", "MATH.MODULO.DESCRIPTION", "math.modulo", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.MODULO.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.MODULO.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput1, "O1", "MATH.MODULO.OUTPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutput2, "O2", "MATH.MODULO.OUTPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ModuloRule(context);
        }
    }
}
