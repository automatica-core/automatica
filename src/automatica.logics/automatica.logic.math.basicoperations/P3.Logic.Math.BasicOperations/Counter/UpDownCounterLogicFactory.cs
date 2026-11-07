using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Counter
{
    public class UpDownCounterLogicFactory : LogicFactory
    {
        internal const double DefaultStep = 1;

        public static readonly Guid RuleUp = new Guid("65ab7995-0997-4b1f-a9b7-6b7899eb0f31");
        public static readonly Guid RuleDown = new Guid("26d4bd60-8fbd-45a8-8cf4-9c659fbb969b");
        public static readonly Guid RuleReset = new Guid("d8e93e32-e521-4953-bf19-933547cb05db");
        public static readonly Guid RuleParamStep = new Guid("38269adf-4393-43cb-8e6d-9b95e6f5c507");
        public static readonly Guid RuleParamLimit = new Guid("a48bdf5a-2d94-4264-ad54-fd5e68b41df1");
        public static readonly Guid RuleOutput = new Guid("77f019fa-a0da-4e8b-b1e2-5ae7d01af96e");
        public static readonly Guid RuleReached = new Guid("200cc4c4-1a41-4a03-969b-720f0b985989");

        public override string LogicName => "Math.UpDownCounter";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("315a5dd4-04a3-4e4a-91ca-f72186f66136");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.COUNTER.NAME", "MATH.COUNTER.DESCRIPTION", "math.counter", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleUp, "+", "MATH.COUNTER.UP.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleDown, "-", "MATH.COUNTER.DOWN.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "MATH.COUNTER.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.COUNTER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleReached, "Q", "MATH.COUNTER.REACHED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamStep, "MATH.COUNTER.STEP.NAME", "MATH.COUNTER.STEP.DESCRIPTION", "step", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultStep);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLimit, "MATH.COUNTER.LIMIT.NAME", "MATH.COUNTER.LIMIT.DESCRIPTION", "limit", LogicGuid, 2, RuleInterfaceParameterDataType.Double, 0d);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new UpDownCounterRule(context);
        }
    }
}
