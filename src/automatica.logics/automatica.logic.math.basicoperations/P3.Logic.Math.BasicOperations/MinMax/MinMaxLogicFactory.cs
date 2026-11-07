using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.MinMax
{
    public class MinMaxLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("58149a97-fe23-4a73-98ca-47c72d78ece6");
        public static readonly Guid RuleInput2 = new Guid("51575831-b767-493f-9546-6508e1aa4e70");
        public static readonly Guid RuleInput3 = new Guid("384cb3f7-461a-47b3-a6d7-b1ffeb795e48");
        public static readonly Guid RuleInput4 = new Guid("7699ff73-847b-46ca-980a-3c85e46e5c42");

        public static readonly Guid RuleMin = new Guid("339a4870-1556-4a57-a1f1-7c1e13f7de96");
        public static readonly Guid RuleMax = new Guid("7541f358-05b9-4a7d-9275-1939288571e4");
        public static readonly Guid RuleAverage = new Guid("fae559a6-953f-4623-b342-adae1cb1cb83");

        public override string LogicName => "Math.MinMax";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("1d5f127e-0ad2-4e1a-8ac3-bab763afd922");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.MINMAX.NAME", "MATH.MINMAX.DESCRIPTION", "math.minmax", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "MATH.MINMAX.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "MATH.MINMAX.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleInput3, "I3", "MATH.MINMAX.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleInput4, "I4", "MATH.MINMAX.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);

            factory.CreateLogicInterfaceTemplate(RuleMin, "Min", "MATH.MINMAX.MIN.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleMax, "Max", "MATH.MINMAX.MAX.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleAverage, "Avg", "MATH.MINMAX.AVERAGE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MinMaxRule(context);
        }
    }
}
