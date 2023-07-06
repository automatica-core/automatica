using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.EnOceanFactory.WindowHandle
{
    public class EnOceanWindowHandleLogicFactory : LogicFactory
    {
        public override string LogicName => "EnOcean.WindowHandle";

        public override Guid LogicGuid => new Guid("5ef3001b-0fbf-40e2-86b9-c6e672daef1c");
        public static readonly Guid RuleInput = new Guid("0395f5da-e4fe-49da-b3b2-570db24b50b5");
        public static readonly Guid RuleOutput = new Guid("98418c84-881a-4e22-ad51-374769a2fb4b");

        public override Version LogicVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EnOceanWindowHandleLogic(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "ENOCEAN_LOGIC.WINDOW_HANDLE.NAME", "ENOCEAN_LOGIC.WINDOW_HANDLE.DESCRIPTION",
               "enocean-window-handle", "ENOCEAN_LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "I", "ENOCEAN_LOGIC.WINDOW_HANDLE.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "ENOCEAN_LOGIC.WINDOW_HANDLE.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }
    }
}
