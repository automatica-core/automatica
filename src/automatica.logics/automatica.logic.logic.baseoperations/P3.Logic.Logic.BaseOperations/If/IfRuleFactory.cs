using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.If
{
    public class IfLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("ad42fdfd-cac1-418d-9ab8-326352485b86");
        public static readonly Guid RuleInput2 = new Guid("098455a7-0337-4e6b-bbb9-e4e379aa5885");


        public static readonly Guid RuleParamTrue = new Guid("b644de1d-35dd-4f1e-aef4-06b2a566bae7");
        public static readonly Guid RuleParamFalse = new Guid("eead24da-f1c8-479f-b643-755efc240125");


        public static readonly Guid RuleOutput = new Guid("76b8d507-8237-4af8-a320-ccebfdd7007d");

        public override string LogicName => "Logic.If";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("8e3666b9-3455-441e-afe2-b96c012929c5");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.IF.NAME", "LOGIC.IF.DESCRIPTION", "logic.if", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.IF.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "LOGIC.IF.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.IF.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamTrue, "LOGIC.IF.PARAM.TRUE.NAME", "LOGIC.IF.PARAM.TRUE.DESCRIPTION", LogicGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 1);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamFalse, "LOGIC.IF.PARAM.FALSE.NAME", "LOGIC.IF.PARAM.FALSE.DESCRIPTION", LogicGuid, 2, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 0);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new IfRule(context);
        }
    }
}
