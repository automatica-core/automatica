using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Formula
{
    public class FormulaLogicFactory : LogicFactory
    {
        internal const string DefaultFormula = "I1";

        public static readonly Guid RuleInput1 = new Guid("d5c69185-ccad-4507-97e8-6bc94e337863");
        public static readonly Guid RuleInput2 = new Guid("98697708-e332-4ab0-9b83-ff6cbd345e33");
        public static readonly Guid RuleInput3 = new Guid("46f3defb-1725-4e20-b30f-bf30dae2c585");
        public static readonly Guid RuleInput4 = new Guid("c925d721-f461-449b-a343-12c37facb4f5");
        public static readonly Guid RuleParamFormula = new Guid("29b9e9c1-17a6-4cb7-a939-958490dd14df");
        public static readonly Guid RuleOutput = new Guid("7c3e0199-5b2e-40b1-9db9-14154e7eb2ff");

        public override string LogicName => "Math.Formula";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("76dca18d-cfa6-47b7-812a-c82d7e370158");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.FORMULA.NAME", "MATH.FORMULA.DESCRIPTION", "math.formula", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.FORMULA.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.FORMULA.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleInput3, "I3", "MATH.FORMULA.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleInput4, "I4", "MATH.FORMULA.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.FORMULA.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamFormula, "MATH.FORMULA.PARAM.NAME", "MATH.FORMULA.PARAM.DESCRIPTION", "formula", LogicGuid, 1, RuleInterfaceParameterDataType.Text, DefaultFormula);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FormulaRule(context);
        }
    }
}
