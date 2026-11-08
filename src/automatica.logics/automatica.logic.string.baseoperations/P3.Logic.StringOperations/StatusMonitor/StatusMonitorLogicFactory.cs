using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.StatusMonitor
{
    /// <summary>
    /// Status monitor (Loxone "Status Monitor"): any number of objects on Stat, up to 10 user-defined states (value, text, color);
    /// counts the objects per state (Cs1..Cs10, the rest on Csr), keeps an activity log and shows both in its visu tile.
    /// The state order is the priority (state 1 first).
    /// </summary>
    public class StatusMonitorLogicFactory : LogicFactory
    {
        internal const int StateCount = 10;
        internal const int DefaultLogEntries = 20;
        internal const int MaxLogEntries = 100;

        public static readonly Guid InputStatus = new Guid("81c5aee9-f392-4186-92b3-d94a5a6ef501");

        public static readonly Guid OutputLastChange = new Guid("92d6bffa-04a3-4297-a3c4-ea5b6b7f0601");
        public static readonly Guid OutputRest = new Guid("92d6bffa-04a3-4297-a3c4-ea5b6b7f0602");
        public static readonly Guid[] OutputCount = Ids("a3e7c00b-15b4-43a8-b4d5-fb6c7c80");

        public static readonly Guid ParamLogEntries = new Guid("b4f8d11c-26c5-44b9-85e6-0c7d8d910801");
        public static readonly Guid ParamRestText = new Guid("b4f8d11c-26c5-44b9-85e6-0c7d8d910802");
        public static readonly Guid ParamRestColor = new Guid("b4f8d11c-26c5-44b9-85e6-0c7d8d910803");
        public static readonly Guid[] ParamValue = Ids("c509e22d-37d6-45ca-96f7-1d8e9ea2");
        public static readonly Guid[] ParamText = Ids("d61af33e-48e7-46db-a708-2e9fafb3");
        public static readonly Guid[] ParamColor = Ids("e72b044f-59f8-47ec-b819-3fa0b0c4");

        private static Guid[] Ids(string prefix)
        {
            var ids = new Guid[StateCount];
            for (var i = 0; i < StateCount; i++)
            {
                ids[i] = new Guid($"{prefix}{i + 1:0000}");
            }
            return ids;
        }

        public override string LogicName => "StringOperations.StatusMonitor";
        public override Guid LogicGuid => new Guid("9b2d6f18-4a3e-4c0b-b7d5-2e8f0a1c3d42");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StatusMonitorRule(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.STATUS_MONITOR.NAME", "STRING_OPERATIONS.STATUS_MONITOR.DESCRIPTION",
                "string_operations.status_monitor", "STRING_OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputStatus, "Stat", "STRING_OPERATIONS.STATUS_MONITOR.INPUT.STATUS.DESCRIPTION", "status",
                LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 1);

            // status: the label tile of cores without the status monitor tile shows the last change
            factory.CreateLogicInterfaceTemplate(OutputLastChange, "Txlc", "STRING_OPERATIONS.STATUS_MONITOR.OUTPUT.LAST_CHANGE.DESCRIPTION", "lastChange",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputRest, "Csr", "STRING_OPERATIONS.STATUS_MONITOR.OUTPUT.REST.DESCRIPTION", "countRest",
                LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            for (var i = 0; i < StateCount; i++)
            {
                factory.CreateLogicInterfaceTemplate(OutputCount[i], $"Cs{i + 1}", "STRING_OPERATIONS.STATUS_MONITOR.OUTPUT.COUNT.DESCRIPTION", $"count{i + 1}",
                    LogicGuid, LogicInterfaceDirection.Output, 0, i + 3);
            }

            var sort = 0;
            factory.CreateParameterLogicInterfaceTemplate(ParamLogEntries, "STRING_OPERATIONS.STATUS_MONITOR.PARAM.LOG_ENTRIES.NAME",
                "STRING_OPERATIONS.STATUS_MONITOR.PARAM.LOG_ENTRIES.DESCRIPTION", "logEntries", LogicGuid, ++sort, RuleInterfaceParameterDataType.Integer, DefaultLogEntries, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamRestText, "STRING_OPERATIONS.STATUS_MONITOR.PARAM.REST_TEXT.NAME",
                "STRING_OPERATIONS.STATUS_MONITOR.PARAM.REST_TEXT.DESCRIPTION", "restText", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamRestColor, "STRING_OPERATIONS.STATUS_MONITOR.PARAM.REST_COLOR.NAME",
                "STRING_OPERATIONS.STATUS_MONITOR.PARAM.REST_COLOR.DESCRIPTION", "restColor", LogicGuid, ++sort, RuleInterfaceParameterDataType.Color, "", false);

            for (var i = 0; i < StateCount; i++)
            {
                var n = i + 1;
                factory.CreateParameterLogicInterfaceTemplate(ParamValue[i], $"STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE{n}_VALUE.NAME",
                    "STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE_VALUE.DESCRIPTION", $"state{n}Value", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
                factory.CreateParameterLogicInterfaceTemplate(ParamText[i], $"STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE{n}_TEXT.NAME",
                    "STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE_TEXT.DESCRIPTION", $"state{n}Text", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
                factory.CreateParameterLogicInterfaceTemplate(ParamColor[i], $"STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE{n}_COLOR.NAME",
                    "STRING_OPERATIONS.STATUS_MONITOR.PARAM.STATE_COLOR.DESCRIPTION", $"state{n}Color", LogicGuid, ++sort, RuleInterfaceParameterDataType.Color, "", false);
            }

            VisuTemplates.TryUse(factory, LogicGuid, VisuTemplates.StatusMonitor);
        }
    }
}
