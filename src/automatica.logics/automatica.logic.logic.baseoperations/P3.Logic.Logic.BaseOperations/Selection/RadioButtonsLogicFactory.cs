using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.Selection
{
    public class RadioButtonsLogicFactory : LogicFactory
    {
        internal const int Size = 16;

        public static readonly Guid[] RuleInputs =
        {
            new Guid("fa6251e9-6a7c-477c-8b1e-0aadd7e61c14"), new Guid("9a16fa67-7016-42f5-bf07-b4b965b4cb02"), new Guid("7531521b-eef5-43c6-b4e0-f0a315651209"), new Guid("d2522478-40c6-4199-8763-2b1e306475c9"),
            new Guid("f9321c35-1956-479c-b26f-723dad0a6ddc"), new Guid("81559120-c22b-40f2-916e-81c09d90f4f5"), new Guid("1d615532-94da-40f7-a6b7-ca8bbbc634cb"), new Guid("5cf8122f-3468-4ddc-8c25-858407659b9c"),
            new Guid("1d464647-569e-4611-be84-463a557cc95b"), new Guid("412cb79c-41f5-471c-96bd-1dadfab358f2"), new Guid("9e26354b-e302-4695-ab0d-c61c02a0ef8e"), new Guid("bc48d4e3-dd86-4d0d-b68b-8c625e79809e"),
            new Guid("4d01b9d4-875f-4c72-849b-a9a43110f511"), new Guid("5080e1e8-991a-435d-aaeb-43f23cf097d4"), new Guid("b584ed22-201e-4a4d-8c80-794e30de1b5d"), new Guid("66daf992-2699-4287-ad13-56f37462bbdd")
        };
        public static readonly Guid RuleNext = new Guid("1dcfde48-7158-427d-83d6-1e9c222ce6f8");
        public static readonly Guid RulePrevious = new Guid("ddec5cff-ff7d-41cc-bd77-2daa31d4bb17");
        public static readonly Guid RuleSelect = new Guid("6fe2b80d-a7b7-4868-9f43-5ee4888cd017");
        public static readonly Guid RuleVisu = new Guid("3bca909c-cabb-44e7-b736-a0a57b64d038");
        public static readonly Guid RuleOff = new Guid("c3b6984b-2510-440d-ac4b-ba82ca524bb3");
        public static readonly Guid RuleDisable = new Guid("1271ea77-a796-4c2a-a42c-376871b49de8");

        public static readonly Guid RuleNumber = new Guid("f9b68a26-7caa-4536-a9e6-aea0c4766c46");
        public static readonly Guid[] RuleOutputs =
        {
            new Guid("f5ab2e29-b03c-4ef8-8369-22515cbc9c63"), new Guid("932ff9e4-2d34-4ae1-8a9a-74c5714d37fc"), new Guid("72cfbac4-4200-45be-87fe-f6068dae4310"), new Guid("d7612421-aa5d-4f2d-a44b-f8c1fa0f4faa"),
            new Guid("1f906028-8883-42bb-b134-50b24ace0577"), new Guid("dd3f7c0c-cf34-474f-9b34-d37c75ab030b"), new Guid("1e03d5cb-5d34-4e90-98e7-f3550529e729"), new Guid("4f9f6ab1-e7d2-424d-98ea-9833ce7a95a0"),
            new Guid("68493744-a18a-4154-b9ef-bf0523c76359"), new Guid("5f223912-ba37-4d34-96cf-4f41442504b6"), new Guid("734eafe9-f179-4086-a0d4-0e639243c0d9"), new Guid("d6698c7b-82be-4902-bc5e-4881751c5f67"),
            new Guid("649ce902-449c-4a26-b049-3003937833bb"), new Guid("c4490ce1-969f-478e-95ea-04d478af563a"), new Guid("a18c543c-eabb-4ea6-a060-4d3b770eedaa"), new Guid("1f73db3a-a1b5-4e1d-8d0a-51dabfece6c5")
        };

        public static readonly Guid RuleParamMax = new Guid("cf5bf702-7475-42e9-94a9-31c80b9476be");
        public static readonly Guid RuleParamSkipZero = new Guid("fb9b9047-4cf5-4ea5-b780-65fef4395e0d");
        public static readonly Guid RuleParamLabels = new Guid("4e7d0a68-3e51-45bb-979f-3636b8d79a0e");

        public override string LogicName => "Logic.RadioButtons";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("79af30c7-0d67-4b2a-afef-bf267466d4c8");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.RADIO_BUTTONS.NAME", "LOGIC.RADIO_BUTTONS.DESCRIPTION", "logic.radio_buttons", "LOGIC.NAME", 100, 100);

            for (var i = 0; i < RuleInputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleInputs[i], $"I{i + 1}", "LOGIC.RADIO_BUTTONS.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, i + 1);
            }
            factory.CreateLogicInterfaceTemplate(RuleNext, "+", "LOGIC.RADIO_BUTTONS.NEXT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, Size + 1);
            factory.CreateLogicInterfaceTemplate(RulePrevious, "-", "LOGIC.RADIO_BUTTONS.PREVIOUS.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, Size + 2);
            factory.CreateLogicInterfaceTemplate(RuleSelect, "Sel", "LOGIC.RADIO_BUTTONS.SELECT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, Size + 3);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "LOGIC.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, Size + 4);
            factory.CreateLogicInterfaceTemplate(RuleDisable, "DisPc", "LOGIC.RADIO_BUTTONS.DISABLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, Size + 5);
            // written by the visualisation (slider), not meant to be linked
            factory.CreateLogicInterfaceTemplate(RuleVisu, "UI", "LOGIC.RADIO_BUTTONS.VISU.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, Size + 6, RuleInterfaceType.Status);

            factory.CreateLogicInterfaceTemplate(RuleNumber, "N", "LOGIC.RADIO_BUTTONS.NUMBER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            for (var i = 0; i < RuleOutputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(RuleOutputs[i], $"O{i + 1}", "LOGIC.RADIO_BUTTONS.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, i + 2);
            }

            factory.CreateParameterLogicInterfaceTemplate(RuleParamMax, "LOGIC.RADIO_BUTTONS.MAX.NAME", "LOGIC.RADIO_BUTTONS.MAX.DESCRIPTION", "max", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, Size);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamSkipZero, "LOGIC.RADIO_BUTTONS.SKIP_ZERO.NAME", "LOGIC.RADIO_BUTTONS.SKIP_ZERO.DESCRIPTION", "sk0", LogicGuid, 2, RuleInterfaceParameterDataType.Bool, false);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLabels, "LOGIC.RADIO_BUTTONS.LABELS.NAME", "LOGIC.RADIO_BUTTONS.LABELS.DESCRIPTION", "labels", LogicGuid, 3, RuleInterfaceParameterDataType.Text, "");

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Slider);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RadioButtonsRule(context);
        }
    }
}
