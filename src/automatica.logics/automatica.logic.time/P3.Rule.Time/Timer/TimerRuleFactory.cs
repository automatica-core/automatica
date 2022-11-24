using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Newtonsoft.Json;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Time.Timer
{
    public class TimerRuleFactory : RuleFactory
    {
        public static Guid RuleReset = new Guid("7c23d7a2-4c82-4526-bc1c-3ba6eb665dd4");
        public static Guid RuleOutput = new Guid("7f752368-0e78-4d14-ac81-d79375f330ce");
        public static Guid RuleTimerParameter = new Guid("05d01746-c69c-46ac-b961-abc23d333a90");

        public override string RuleName => "P3.Rule.Time.Timer";
        public override Guid RuleGuid => new Guid("297f00bd-5a99-45cc-8e6b-a6e8b6ae3c19");
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override bool InDevelopmentMode => true;
        
        public override void InitTemplates(IRuleTemplateFactory factory)
        {

            factory.CreateRuleTemplate(RuleGuid, "TIME.TIMER.NAME", "TIME.TIMER.DESCRIPTION",
                "time.timer", "TIME.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleReset, "R", "TIME.TIMER.RESET.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "TIME.TIMER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(RuleTimerParameter, "TIME.TIMER.NAME", "TIME.TIMER.DESCRIPTION", RuleGuid, 1, RuleInterfaceParameterDataType.Timer, JsonConvert.SerializeObject(new TimerPropertyData()));
            
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new TimerRule(context);
        }

    }
}
