using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.Selection
{
    public class SequencerLogicFactory : LogicFactory
    {
        internal const int DefaultMax = 8;
        internal const int DefaultValue = 1;

        public static readonly Guid RuleTrigger = new Guid("4a562d65-090b-4178-a021-e5c4bd4bec26");
        public static readonly Guid RulePosition = new Guid("23fd19db-df26-45c1-b667-5f734171bc48");
        public static readonly Guid RuleReset = new Guid("6d531a48-2f3d-40f5-811b-9aca0770d285");
        public static readonly Guid RuleDisable = new Guid("cb071a71-5929-40c2-96ff-046a8331bec9");

        public static readonly Guid RuleSelected = new Guid("8697c9c0-1683-4868-8e11-211ccb0b8f81");
        public static readonly Guid[] RuleOutputs =
        {
            new Guid("6f83eaa3-fe47-4911-afaf-33aa1463af51"),
            new Guid("2b93799a-1d3f-4170-95d1-409cf62a170e"),
            new Guid("5f268873-1493-4bd7-98b0-7e059c66b223"),
            new Guid("51f42efa-3268-4019-96e8-b57089714170"),
            new Guid("236674b6-e2b2-49e0-a987-8d2bf68c8f75"),
            new Guid("ccae7cfe-e6fe-4a6f-bf08-8f3e6e5cab88"),
            new Guid("fab5ca4b-f931-4dc3-b587-8766ca3bf597"),
            new Guid("7431bdad-640b-4dd6-80e7-07a47ac5276c")
        };

        public static readonly Guid RuleParamMax = new Guid("9342db98-8b41-4d25-9dcf-5a5adcf6c7ae");
        public static readonly Guid RuleParamDefault = new Guid("eb3bc03f-a9de-42ea-ba2d-7fcaa7e9d1c3");

        public override string LogicName => "Logic.Sequencer";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("5453a298-a3fb-4d0a-b4b8-0c06d4d8c813");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.SEQUENCER.NAME", "LOGIC.SEQUENCER.DESCRIPTION", "logic.sequencer", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "Tr", "LOGIC.SEQUENCER.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RulePosition, "P", "LOGIC.SEQUENCER.POSITION.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "LOGIC.SEQUENCER.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleDisable, "DisPc", "LOGIC.SEQUENCER.DISABLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);

            for (var i = 0; i < RuleOutputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleOutputs[i], $"O{i + 1}", "LOGIC.SEQUENCER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, i + 1);
            }
            factory.CreateLogicInterfaceTemplate(RuleSelected, "Sel", "LOGIC.SEQUENCER.SELECTED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, RuleOutputs.Length + 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamMax, "LOGIC.SEQUENCER.MAX.NAME", "LOGIC.SEQUENCER.MAX.DESCRIPTION", "max", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, DefaultMax);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamDefault, "LOGIC.SEQUENCER.DEFAULT.NAME", "LOGIC.SEQUENCER.DEFAULT.DESCRIPTION", "dv", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, DefaultValue);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SequencerRule(context);
        }
    }
}
