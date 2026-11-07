using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.Multiplexer
{
    public class MultiplexerLogicFactory : LogicFactory
    {
        public static readonly Guid RuleSelect = new Guid("55063860-68b0-4958-bcc8-7fcd433d37d7");
        public static readonly Guid RuleInput1 = new Guid("10d36ca3-b0ef-431f-b954-f2f442a93e83");
        public static readonly Guid RuleInput2 = new Guid("3a9df52c-d6c5-4d12-b0c2-10a031d17dd2");
        public static readonly Guid RuleInput3 = new Guid("1b00bea8-5cae-4358-93ec-92708d355a71");
        public static readonly Guid RuleInput4 = new Guid("510de4f5-4f7f-43f6-8744-435287751f8a");
        public static readonly Guid RuleOutput = new Guid("e361a2f5-b282-4fdb-8d35-d8b571d4d8f5");

        public override string LogicName => "Logic.Multiplexer";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("1817472d-42d7-4523-9e40-dd2c371b1a72");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.MULTIPLEXER.NAME", "LOGIC.MULTIPLEXER.DESCRIPTION", "logic.multiplexer", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleSelect, "S", "LOGIC.MULTIPLEXER.SELECT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.MULTIPLEXER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "LOGIC.MULTIPLEXER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);
            factory.CreateLogicInterfaceTemplate(RuleInput3, "I3", "LOGIC.MULTIPLEXER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 4);
            factory.CreateLogicInterfaceTemplate(RuleInput4, "I4", "LOGIC.MULTIPLEXER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 5);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.MULTIPLEXER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MultiplexerRule(context);
        }
    }
}
