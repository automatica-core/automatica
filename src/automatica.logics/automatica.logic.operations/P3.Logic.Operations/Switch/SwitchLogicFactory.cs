using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Common;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Switch
{
    public class SwitchLogicFactory : LogicFactory
    {
        public static Guid RuleInput = new Guid("a9fc6cad-d76f-464f-8b8a-a9873c695cb1");
        public static Guid RuleState = new Guid("b198329b-453a-4a2d-9f32-08a3287fb0f6");
        public static Guid RuleOutput = new Guid("ca8747ac-9c1d-4ca5-a91f-dd69d940bcb6");

        /// <summary>PS input + the parameters of the presence simulation (added in 1.1)</summary>
        public static readonly PresenceSimulationIds PresenceSimulation = new(
            new Guid("bb4c372c-fc43-4960-9705-c238a962e964"),
            new Guid("620071c9-3412-43ac-a899-ffbdc4d6a485"),
            new Guid("0062c263-0d0c-4769-839c-807ea07b3614"),
            new Guid("f0b315ce-eb76-4e9d-ac69-54de1c9c46c9"),
            new Guid("6035fc62-bd62-4dac-895f-0d0f572f90f9"),
            new Guid("1dffdfd8-952a-4aac-aa93-235a394ac9bf"),
            new Guid("bb8ccfab-183c-49a9-89fb-df197f49f111"));

        public override string LogicName => "P3.Logic.Operations.Switch";
        public override Guid LogicGuid => new Guid("3721acc3-5639-4cd2-aa30-a1952690d177");
        public override Version LogicVersion => new Version(1, 1, 0, 0);
        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.SWITCH.NAME", "OPERATIONS.SWITCH.DESCRIPTION",
                "operations-switch", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "OPERATIONS.SWITCH.INPUT.NAME", "OPERATIONS.SWITCH.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
            factory.CreateLogicInterfaceTemplate(RuleState, "OPERATIONS.SWITCH.STATE.NAME", "OPERATIONS.SWITCH.STATE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 2, RuleInterfaceType.Status);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "OPERATIONS.SWITCH.OUTPUT.NAME", "OPERATIONS.SWITCH.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);

            PresenceSimulationBinding.CreateTemplates(factory, LogicGuid, PresenceSimulation, 3, 1);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.ToggleButton);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SwitchLogic(context);
        }

    }
}
