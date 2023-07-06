using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Time.Timer
{
    public class TimerLogicFactory : LogicFactory
    {
        public static Guid RuleReset = new Guid("7c23d7a2-4c82-4526-bc1c-3ba6eb665dd4");
        public static Guid RuleOutput = new Guid("7f752368-0e78-4d14-ac81-d79375f330ce");
        public static Guid RuleTimerParameter = new Guid("05d01746-c69c-46ac-b961-abc23d333a90");

        public override string LogicName => "P3.Logic.Time.Timer";
        public override Guid LogicGuid => new Guid("297f00bd-5a99-45cc-8e6b-a6e8b6ae3c19");
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override bool InDevelopmentMode => true;
        
        public override void InitTemplates(ILogicTemplateFactory factory)
        {

            factory.CreateLogicTemplate(LogicGuid, "TIME.TIMER.NAME", "TIME.TIMER.DESCRIPTION",
                "time.timer", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "TIME.TIMER.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.TIMER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleTimerParameter, "TIME.TIMER.NAME", "TIME.TIMER.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Timer, JsonConvert.SerializeObject(new TimerPropertyData()));
            
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TimerRule(context);
        }

    }
}
