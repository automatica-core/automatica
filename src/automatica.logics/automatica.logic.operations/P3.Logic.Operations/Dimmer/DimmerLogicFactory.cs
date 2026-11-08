using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Common;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogicFactory : LogicFactory
    {
        public static Guid RuleInputState = new Guid("1753a8a7-18ed-4636-952e-66cdabd73698");
        public static Guid RuleInputValue = new Guid("1d1f873b-9d56-4c28-8373-7024320b1b89");
        public static Guid RuleInputReset = new Guid("b0368661-51fb-46e5-9151-3c7901ffbffe");

        public static Guid DimmerValue = new Guid("3082aac1-544c-46b0-b9fd-05a54ab2a67f");
        public static Guid DimmerState = new Guid("59223284-c80f-441f-a647-fc2786e998a4");

        /// <summary>PS input + the parameters of the presence simulation (added in 1.1)</summary>
        public static readonly PresenceSimulationIds PresenceSimulation = new(
            new Guid("8be1ca9f-5dc0-45e8-9fda-60ea0c3ec9fe"),
            new Guid("ab44920a-6bd2-4850-b354-9e7936aea8ef"),
            new Guid("d886d17f-3c8b-4ad6-845d-01510a12bf5f"),
            new Guid("b2bf175c-a065-49a1-8054-66bb28752474"),
            new Guid("a14fdad1-e11a-4231-ba6a-3d909a63b891"),
            new Guid("2c5b4c5d-3599-4bf2-a4cc-0f1253e94d92"),
            new Guid("9fc54439-b1e3-4de3-b2fa-9ec461a55035"));

        public override string LogicName => "P3.Logic.Operations.Dimmer";
        public override Guid LogicGuid => new Guid("b1010e11-0e8d-4d8c-97df-a7829cd7be96");
        public override Version LogicVersion => new Version(1, 1, 0, 0);
        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.DIMMER.NAME", "OPERATIONS.DIMMER.DESCRIPTION",
                "operations-DIMMER", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInputState, "OPERATIONS.DIMMER.INPUT.STATE.NAME", "OPERATIONS.DIMMER.INPUT.STATE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
            factory.CreateLogicInterfaceTemplate(RuleInputValue, "OPERATIONS.DIMMER.INPUT.VALUE.NAME", "OPERATIONS.DIMMER.INPUT.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(RuleInputReset, "OPERATIONS.DIMMER.INPUT.RESET.NAME", "OPERATIONS.DIMMER.INPUT.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Unknown);

            factory.CreateLogicInterfaceTemplate(DimmerValue, "OPERATIONS.DIMMER.OUTPUT.VALUE.NAME", "OPERATIONS.DIMMER.OUTPUT.VALUE.DESCRIPTION", "outputValue", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateLogicInterfaceTemplate(DimmerState, "OPERATIONS.DIMMER.OUTPUT.STATE.NAME", "OPERATIONS.DIMMER.OUTPUT.STATE.DESCRIPTION", "outputState", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output); 

            PresenceSimulationBinding.CreateTemplates(factory, LogicGuid, PresenceSimulation, 2, 1);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Dimmer);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new DimmerLogic(context);
        }

    }
}
