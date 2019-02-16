using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace P3.Rule.Surveillance.WindowMonitor
{
    public class WindowMonitorFactory : RuleFactory
    {
        public override string RuleName => "WindowMonitor";

        public override Guid RuleGuid => new Guid("b664d32f-3b21-40b4-80a8-608491e07a5f");

        public override Version RuleVersion => new Version(0, 0, 0, 4);

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new WindowMonitorRule(context);
        }

        public static readonly Guid Ct = new Guid("56e789c7-4a25-4a71-ac75-7bf2fcc647e3");
        public static readonly Guid Co = new Guid("bef5e164-47db-47d5-9aa1-3ecd578640e9");
        public static readonly Guid Cl = new Guid("1545c638-08e8-4c53-9847-5f2e597dbd1f");
        public static readonly Guid Cti = new Guid("eef56230-7980-4dde-9ac1-246b11469021");
        public static readonly Guid Coi = new Guid("db541c1d-4b57-4002-9408-c687aaba60f9");
        public static readonly Guid Cli = new Guid("b12368f9-8a24-419f-9454-6cb26697085f");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "SURVEILLANCE.WINDOW_MONITOR.NAME", "MATH.WINDOW_MONITOR.DESCRIPTION",
               "surveillance.window_monitor", "SUVREILLANCE.NAME", 100, 100);


            factory.CreateRuleInterfaceTemplate(Ct, "Ct", "SURVEILLANCE.WINDOW_MONITOR.TILT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 2);
            factory.CreateRuleInterfaceTemplate(Co, "Co", "SURVEILLANCE.WINDOW_MONITOR.OPEN.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 3);
            factory.CreateRuleInterfaceTemplate(Cl, "Cl", "SURVEILLANCE.WINDOW_MONITOR.LOCKED.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 4);


            factory.CreateRuleInterfaceTemplate(Cti, "Cti", "SURVEILLANCE.WINDOW_MONITOR.TILT_INVERTED.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 5);
            factory.CreateRuleInterfaceTemplate(Coi, "Coi", "SURVEILLANCE.WINDOW_MONITOR.OPEN_INVERTED.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 6);
            factory.CreateRuleInterfaceTemplate(Cli, "Cli", "SURVEILLANCE.WINDOW_MONITOR.LOCKED_INVERTED.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, int.MaxValue, 7);

            factory.ChangeDefaultVisuTemplate(RuleGuid, VisuMobileObjectTemplateTypes.WindowMonitor);
        }
    }
}
