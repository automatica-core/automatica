using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Slider
{
    public class SliderLogicFactory : LogicFactory
    {
        public static Guid RuleInputValue = new Guid("47bac8cb-05f7-487a-b922-7f3bc35e7b76");
        public static Guid RuleInputReset = new Guid("0df0fc69-cb3c-4eb6-967d-54a69e78bd14");


        public static Guid RuleInputValueMax = new Guid("55e38c73-c595-447d-93cd-41972805e5a4");
        public static Guid RuleInputValueMin = new Guid("aa35e081-3235-4151-b33b-ba7313cc8aeb");

        public static Guid RuleInputValueMaxParam = new Guid("41eb3142-8a32-45ea-bc3a-bff7817036f7");
        public static Guid RuleInputValueMinParam = new Guid("c2492133-a7d9-4e56-a36d-8af42053ce9a");

        public static Guid RuleOutput = new Guid("1f17f3a6-948b-4ca1-9ceb-ce239c5c4e94");
        public static Guid RuleStateOutput = new Guid("d02337a8-7cb1-42bf-84df-1b4b9370095b");

        public override string LogicName => "P3.Logic.Operations.Slider";
        public override Guid LogicGuid => new Guid("3d77ef6f-fa1e-4f20-a3ab-81b3ef969221");
        public override Version LogicVersion => new Version(1, 0, 0, 4);
        public override bool InDevelopmentMode => false;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.SLIDER.NAME", "OPERATIONS.SLIDER.DESCRIPTION",
                "operations-SLIDER", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInputValue, "OPERATIONS.SLIDER.INPUT.VALUE.NAME", "OPERATIONS.SLIDER.INPUT.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 0, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleInputReset, "OPERATIONS.SLIDER.INPUT.RESET.NAME", "OPERATIONS.SLIDER.INPUT.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 2, RuleInterfaceType.Unknown);
            factory.CreateLogicInterfaceTemplate(RuleInputValueMax, "OPERATIONS.SLIDER.INPUT.VALUE_MAX.NAME", "OPERATIONS.SLIDER.INPUT.VALUE_MAX.DESCRIPTION", "value_max",LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(RuleInputValueMin, "OPERATIONS.SLIDER.INPUT.VALUE_MIN.NAME", "OPERATIONS.SLIDER.INPUT.VALUE_MIN.DESCRIPTION", "value_min", LogicGuid, LogicInterfaceDirection.Input, 0, 4);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "OPERATIONS.SLIDER.OUTPUT.VALUE.NAME", "OPERATIONS.SLIDER.OUTPUT.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateLogicInterfaceTemplate(RuleStateOutput, "OPERATIONS.SLIDER.OUTPUT.STATE.NAME", "OPERATIONS.SLIDER.OUTPUT.STATE.DESCRIPTION",  "output-state",LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);

            factory.CreateParameterLogicInterfaceTemplate(RuleInputValueMinParam,
                "OPERATIONS.SLIDER.INPUT.VALUE_MIN.NAME", "OPERATIONS.SLIDER.INPUT.VALUE_MIN.DESCRIPTION", LogicGuid, 1,
                RuleInterfaceParameterDataType.Double, null);
            factory.CreateParameterLogicInterfaceTemplate(RuleInputValueMaxParam,
                "OPERATIONS.SLIDER.INPUT.VALUE_MAX.NAME", "OPERATIONS.SLIDER.INPUT.VALUE_MAX.DESCRIPTION", LogicGuid, 2,
                RuleInterfaceParameterDataType.Double, null);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Slider);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SliderLogic(context);
        }

    }
}
