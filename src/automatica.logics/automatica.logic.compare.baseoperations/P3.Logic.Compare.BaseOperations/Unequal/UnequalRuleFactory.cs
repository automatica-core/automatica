using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Unequal
{
    public class UnequalLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("0e37bbee-8d82-4322-8b15-fc7139500c3e");
        public static readonly Guid RuleInput2 = new Guid("421cc8f0-4057-4a05-aa28-a5d208ac6ebd");
        public static readonly Guid RuleOutput = new Guid("8c8ee302-4da8-4f43-956b-52d5ca96eb4f");

        public override string LogicName => "Compare.Unequal";
        public override Version LogicVersion => new Version(1, 0, 0, 1);
        public override Guid LogicGuid => new Guid("22d83387-a7db-425a-82fc-28cde7effd25");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.UNEQUAL.NAME", "COMPARE.UNEQUAL.DESCRIPTION",
                "compare.unequal", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.UNEQUAL.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.UNEQUAL.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.UNEQUAL.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new UnequalRule(context);
        }
    }
}
