using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Random
{
    public class RandomLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInputTrigger = new Guid("40e41d2c-b7d7-4d85-906d-8abf2e5ed4c7");
        public static readonly Guid RuleInputDisabled = new Guid("37bca3d8-e865-46f6-9803-7ca23431f890");

        public static readonly Guid RuleParamMin= new Guid("d091fcf2-4561-4cba-b880-b6d8aeecd2b0");
        public static readonly Guid RuleParamMax = new Guid("1a36e087-3b98-4e44-a40b-da698087da6f");

        public static readonly Guid RuleOutput = new Guid("1c7d9aff-0b29-40b1-9502-17b534763c08");

        public override string LogicName => "Math.Random";
        public override Version LogicVersion => new Version(1, 0, 0, 2);
        public override Guid LogicGuid => new Guid("3e08d671-610f-42f4-a676-a626ba42e83d");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.RANDOM.NAME", "MATH.RANDOM.DESCRIPTION",
                "math.random", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInputTrigger, "Tr", "MATH.RANDOM.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInputDisabled, "Dis", "MATH.RANDOM.DISABLED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamMin, "Min", "MATH.RANDOM.MIN.DESCRIPTION", LogicGuid,  1, RuleInterfaceParameterDataType.Integer, 0);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMax, "Max", "MATH.RANDOM.MAX.DESCRIPTION", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, 100);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.RANDOM.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RandomRule(context);
        }
    }
}
