using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Time.AdvancedTimer
{
    public class AdvancedTimerRuleFactory : LogicFactory
    {
        public static Guid RuleReset = new Guid("0b13af2f-303a-40da-998f-e65758df6f22");
        public static Guid RuleOutput = new Guid("2023dd5f-2a44-4fdb-9236-2fc138cf9230");
        public static Guid RuleTimerParameter = new Guid("1d2bc9af-b6c7-48ac-a957-33308cd07e28");

        public override string LogicName => "P3.Logic.Time.AdvancedTimer";
        public override Guid LogicGuid => new Guid("8094b053-e226-4f0c-8e2e-bbb71fda9eb9");
        public override Version LogicVersion => new Version(0, 1, 0, 1);
        public override bool InDevelopmentMode => true;
        
        public override void InitTemplates(ILogicTemplateFactory factory)
        {

            factory.CreateLogicTemplate(LogicGuid, "TIME.ADVANCED_TIMER.NAME", "TIME.ADVANCED_TIMER.DESCRIPTION",
                "time.advanced_timer", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "TIME.ADVANCED_TIMER.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.ADVANCED_TIMER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleTimerParameter, "TIME.ADVANCED_TIMER.NAME", "TIME.ADVANCED_TIMER.DESCRIPTION", "delay", LogicGuid, 1, RuleInterfaceParameterDataType.Calendar, JsonConvert.SerializeObject(new CalendarPropertyData()));
            
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AdvancedTimerRule(context);
        }

    }
}
