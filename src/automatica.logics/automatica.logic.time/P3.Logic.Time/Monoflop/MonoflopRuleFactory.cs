using System;
using Automatica.Core.Logic;
using Automatica.Core.Base.Templates;

namespace P3.Logic.Time.Monoflop
{
    public class MonoflopLogicFactory : LogicFactory
    {
        public static readonly Guid RuleTrigger = new Guid("8f018851-dac0-4392-9b3a-547eeeef6a1f");
        public static readonly Guid RuleParamDelay = new Guid("52cfb566-5f13-4e58-98db-01568fddce6f");


        public static readonly Guid RuleOutput = new Guid("3DBE05BF-BD15-4460-BB1D-9622797FD4AA");

        public override string LogicName => "Time.Monoflop";
        public override Version LogicVersion => new Version(1, 0, 0, 2);
        public override Guid LogicGuid => new Guid("32C3A4C9-9A84-4581-AA25-72B5D25091EA");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.MONOFLOP.NAME", "TIME.MONOFLOP.DESCRIPTION",
                "time.monoflop", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "T", "TIME.MONOFLOP.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.MONOFLOP.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", "delay", LogicGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MonoflopRule(context);
        }
    }
}
