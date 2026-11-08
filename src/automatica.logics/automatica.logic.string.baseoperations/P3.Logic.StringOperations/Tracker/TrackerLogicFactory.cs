using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.Tracker
{
    /// <summary>
    /// Tracker (Loxone "Tracker"): every value on I becomes a time-stamped entry (text template with &lt;v&gt; and &lt;n&gt;,
    /// own texts for on / off), the last N entries are kept (also across restarts) and shown in its visu tile. R clears it.
    /// </summary>
    public class TrackerLogicFactory : LogicFactory
    {
        internal const int DefaultEntries = 20;
        internal const int MinEntries = 2;
        internal const int MaxEntries = 100;
        internal const string DefaultTemplate = "<v>";

        public static readonly Guid InputValue = new Guid("f83c1550-6a09-48fd-8c2a-40b1c1d5e101");
        public static readonly Guid InputReset = new Guid("f83c1550-6a09-48fd-8c2a-40b1c1d5e102");

        public static readonly Guid OutputLast = new Guid("0a4d2661-7b1a-490e-9d3b-51c2d2e6f201");
        public static readonly Guid OutputCount = new Guid("0a4d2661-7b1a-490e-9d3b-51c2d2e6f202");

        public static readonly Guid ParamTemplate = new Guid("1b5e3772-8c2b-4a1f-8e4c-62d3e3f70301");
        public static readonly Guid ParamOnText = new Guid("1b5e3772-8c2b-4a1f-8e4c-62d3e3f70302");
        public static readonly Guid ParamOffText = new Guid("1b5e3772-8c2b-4a1f-8e4c-62d3e3f70303");
        public static readonly Guid ParamEntries = new Guid("1b5e3772-8c2b-4a1f-8e4c-62d3e3f70304");
        public static readonly Guid ParamRemanent = new Guid("1b5e3772-8c2b-4a1f-8e4c-62d3e3f70305");

        public override string LogicName => "StringOperations.Tracker";
        public override Guid LogicGuid => new Guid("ac3e7029-5b4f-4d1c-88e6-3f901b2d4e53");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TrackerRule(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.TRACKER.NAME", "STRING_OPERATIONS.TRACKER.DESCRIPTION",
                "string_operations.tracker", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputValue, "I", "STRING_OPERATIONS.TRACKER.INPUT.VALUE.DESCRIPTION", "value",
                LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 1);
            factory.CreateLogicInterfaceTemplate(InputReset, "R", "STRING_OPERATIONS.TRACKER.INPUT.RESET.DESCRIPTION", "reset",
                LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            // status: the label tile of cores without the tracker tile shows the last entry
            factory.CreateLogicInterfaceTemplate(OutputLast, "Txl", "STRING_OPERATIONS.TRACKER.OUTPUT.LAST.DESCRIPTION", "last",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputCount, "Cnt", "STRING_OPERATIONS.TRACKER.OUTPUT.COUNT.DESCRIPTION", "count",
                LogicGuid, LogicInterfaceDirection.Output, 0, 2);

            var sort = 0;
            Param(factory, ParamTemplate, "TEMPLATE", "template", ++sort, RuleInterfaceParameterDataType.Text, DefaultTemplate);
            Param(factory, ParamOnText, "ON_TEXT", "onText", ++sort, RuleInterfaceParameterDataType.Text, "");
            Param(factory, ParamOffText, "OFF_TEXT", "offText", ++sort, RuleInterfaceParameterDataType.Text, "");
            Param(factory, ParamEntries, "ENTRIES", "entries", ++sort, RuleInterfaceParameterDataType.Integer, DefaultEntries);
            Param(factory, ParamRemanent, "REMANENT", "remanent", ++sort, RuleInterfaceParameterDataType.Bool, true);

            VisuTemplates.TryUse(factory, LogicGuid, VisuTemplates.Tracker);
        }

        private void Param(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, RuleInterfaceParameterDataType type, object defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(id, $"STRING_OPERATIONS.TRACKER.PARAM.{name}.NAME", $"STRING_OPERATIONS.TRACKER.PARAM.{name}.DESCRIPTION",
                key, LogicGuid, sort, type, defaultValue, false);
        }
    }
}
