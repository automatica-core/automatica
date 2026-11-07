using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.FlipFlop
{
    /// <summary>
    /// RS flip-flop: reset wins when S and R are 1
    /// </summary>
    public class RsFlipFlopLogicFactory : LogicFactory
    {
        public static readonly Guid RuleSet = new Guid("e28b0a76-1a21-483a-bd49-51a80bfdf8bf");
        public static readonly Guid RuleReset = new Guid("dd6ae6cf-e068-4ce7-9921-ecf02d9782fe");
        public static readonly Guid RuleOutput = new Guid("a401b220-47e4-4ed8-bbc6-5f6d23c55dfd");

        public override string LogicName => "Logic.RsFlipFlop";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("c54b1f1d-5157-436b-99f5-9ef541c17986");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.RS_FLIPFLOP.NAME", "LOGIC.RS_FLIPFLOP.DESCRIPTION", "logic.rs_flipflop", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleSet, "S", "LOGIC.FLIPFLOP.SET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "LOGIC.FLIPFLOP.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Q", "LOGIC.FLIPFLOP.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FlipFlopRule(context, RuleSet, RuleReset, RuleOutput, setDominant: false);
        }
    }

    /// <summary>
    /// SR flip-flop: set wins when S and R are 1
    /// </summary>
    public class SrFlipFlopLogicFactory : LogicFactory
    {
        public static readonly Guid RuleSet = new Guid("2fd7cc6c-90e3-4004-83b0-342697d44c7c");
        public static readonly Guid RuleReset = new Guid("2146983a-8f4d-4e52-bc9e-783c05b5136e");
        public static readonly Guid RuleOutput = new Guid("7ab0a6a7-958b-4042-90da-41f402ae33e8");

        public override string LogicName => "Logic.SrFlipFlop";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("61f0128e-41b3-43e4-b766-3720aaedda9c");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.SR_FLIPFLOP.NAME", "LOGIC.SR_FLIPFLOP.DESCRIPTION", "logic.sr_flipflop", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleSet, "S", "LOGIC.FLIPFLOP.SET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "LOGIC.FLIPFLOP.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Q", "LOGIC.FLIPFLOP.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FlipFlopRule(context, RuleSet, RuleReset, RuleOutput, setDominant: true);
        }
    }
}
