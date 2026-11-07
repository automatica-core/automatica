using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.ClickDetection
{
    public class ClickDetectionLogicFactory : LogicFactory
    {
        internal const int DefaultClickWindow = 400;
        internal const int DefaultLongPress = 800;

        public static readonly Guid RuleInput = new Guid("576379f8-5b88-44b8-ba97-681661e45ce9");
        public static readonly Guid RuleSingle = new Guid("6ffeff35-e384-4e4e-b96f-2bd0e709d61b");
        public static readonly Guid RuleDouble = new Guid("ad84bfb2-98db-4029-b5a8-422e1c76d737");
        public static readonly Guid RuleTriple = new Guid("c9805458-5b87-4447-8e53-ba356aeab00a");
        public static readonly Guid RuleLong = new Guid("dd683e1b-5a45-4ec2-9b35-9985e09a289f");
        public static readonly Guid RuleParamClickWindow = new Guid("e2323f5f-1022-488a-aac9-9a1bb465289b");
        public static readonly Guid RuleParamLongPress = new Guid("e0d0ebf5-4427-458c-86ff-5b122680e0c7");

        public override string LogicName => "Time.ClickDetection";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("dfd253fc-06fc-403b-a01f-61fdbd39b80d");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.CLICK_DETECTION.NAME", "TIME.CLICK_DETECTION.DESCRIPTION", "time.click_detection", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "I", "TIME.CLICK_DETECTION.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(RuleSingle, "1x", "TIME.CLICK_DETECTION.SINGLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleDouble, "2x", "TIME.CLICK_DETECTION.DOUBLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(RuleTriple, "3x", "TIME.CLICK_DETECTION.TRIPLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(RuleLong, "L", "TIME.CLICK_DETECTION.LONG.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 4);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamClickWindow, "TIME.CLICK_DETECTION.CLICK_WINDOW.NAME", "TIME.CLICK_DETECTION.CLICK_WINDOW.DESCRIPTION", "clickWindow", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, DefaultClickWindow);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLongPress, "TIME.CLICK_DETECTION.LONG_PRESS.NAME", "TIME.CLICK_DETECTION.LONG_PRESS.DESCRIPTION", "longPress", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, DefaultLongPress);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ClickDetectionRule(context);
        }
    }
}
