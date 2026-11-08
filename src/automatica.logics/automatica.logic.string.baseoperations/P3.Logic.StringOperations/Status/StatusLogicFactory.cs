using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.Status
{
    /// <summary>
    /// Status (Loxone "Status"): up to 8 rules on the inputs I1..I8, the first rule that is true sets the status text, value,
    /// icon and color. Shown in the visu with its own tile, the outputs can be used in other logics.
    /// </summary>
    public class StatusLogicFactory : LogicFactory
    {
        internal const int RuleCount = 8;

        public static readonly Guid[] Inputs =
        {
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f701"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f702"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f703"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f704"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f705"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f706"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f707"),
            new Guid("0f4d2c61-7b1a-4f0e-9a3b-51c8d2e6f708")
        };

        public static readonly Guid OutputText = new Guid("1a5e3d72-8c2b-4a1f-8b4c-62d9e3f70811");
        public static readonly Guid OutputValue = new Guid("1a5e3d72-8c2b-4a1f-8b4c-62d9e3f70812");
        public static readonly Guid OutputRule = new Guid("1a5e3d72-8c2b-4a1f-8b4c-62d9e3f70813");

        public static readonly Guid ParamDefaultText = new Guid("2b6f4e83-9d3c-4b20-9c5d-73eaf4081901");
        public static readonly Guid ParamDefaultValue = new Guid("2b6f4e83-9d3c-4b20-9c5d-73eaf4081902");
        public static readonly Guid ParamDefaultIcon = new Guid("2b6f4e83-9d3c-4b20-9c5d-73eaf4081903");
        public static readonly Guid ParamDefaultColor = new Guid("2b6f4e83-9d3c-4b20-9c5d-73eaf4081904");

        public static readonly Guid[] ParamCondition = Ids("3c705f94-ae4d-4c31-8d6e-84fb0519a0");
        public static readonly Guid[] ParamText = Ids("4d816aa5-bf5e-4d42-9e7f-950c162ab1");
        public static readonly Guid[] ParamValue = Ids("5e927bb6-c06f-4e53-af80-a61d273bc2");
        public static readonly Guid[] ParamIcon = Ids("6fa38cc7-d170-4f64-b091-b72e384cd3");
        public static readonly Guid[] ParamColor = Ids("70b49dd8-e281-4075-81a2-c83f495de4");

        private static Guid[] Ids(string prefix)
        {
            var ids = new Guid[RuleCount];
            for (var i = 0; i < RuleCount; i++)
            {
                ids[i] = new Guid($"{prefix}{i + 1:00}");
            }
            return ids;
        }

        public override string LogicName => "StringOperations.Status";
        public override Guid LogicGuid => new Guid("8a1c5e07-3f2d-4b9a-a6c4-1d7e9f0b2c31");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StatusRule(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "STRING_OPERATIONS.STATUS.NAME", "STRING_OPERATIONS.STATUS.DESCRIPTION",
                "string_operations.status", "STRING_OPERATIONS.NAME", 100, 100);

            for (var i = 0; i < Inputs.Length; i++)
            {
                factory.CreateLogicInterfaceTemplate(Inputs[i], $"I{i + 1}", "STRING_OPERATIONS.STATUS.INPUT.DESCRIPTION", $"i{i + 1}",
                    LogicGuid, LogicInterfaceDirection.Input, 1, i + 1);
            }

            // status: the label tile of cores without the status tile shows the text
            factory.CreateLogicInterfaceTemplate(OutputText, "Txt", "STRING_OPERATIONS.STATUS.OUTPUT.TEXT.DESCRIPTION", "text",
                LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputValue, "Val", "STRING_OPERATIONS.STATUS.OUTPUT.VALUE.DESCRIPTION", "value",
                LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputRule, "Nr", "STRING_OPERATIONS.STATUS.OUTPUT.RULE.DESCRIPTION", "rule",
                LogicGuid, LogicInterfaceDirection.Output, 0, 3);

            var sort = 0;
            Param(factory, ParamDefaultText, "DEFAULT_TEXT", "defaultText", ++sort, RuleInterfaceParameterDataType.Text, "");
            Param(factory, ParamDefaultValue, "DEFAULT_VALUE", "defaultValue", ++sort, RuleInterfaceParameterDataType.Double, 0);
            Param(factory, ParamDefaultIcon, "DEFAULT_ICON", "defaultIcon", ++sort, RuleInterfaceParameterDataType.Text, "");
            Param(factory, ParamDefaultColor, "DEFAULT_COLOR", "defaultColor", ++sort, RuleInterfaceParameterDataType.Color, "");

            for (var i = 0; i < RuleCount; i++)
            {
                var n = i + 1;
                RuleParam(factory, ParamCondition[i], n, "CONDITION", "condition", ++sort, RuleInterfaceParameterDataType.Text, "");
                RuleParam(factory, ParamText[i], n, "TEXT", "text", ++sort, RuleInterfaceParameterDataType.Text, "");
                RuleParam(factory, ParamValue[i], n, "VALUE", "value", ++sort, RuleInterfaceParameterDataType.Double, n);
                RuleParam(factory, ParamIcon[i], n, "ICON", "icon", ++sort, RuleInterfaceParameterDataType.Text, "");
                RuleParam(factory, ParamColor[i], n, "COLOR", "color", ++sort, RuleInterfaceParameterDataType.Color, "");
            }

            VisuTemplates.TryUse(factory, LogicGuid, VisuTemplates.Status);
        }

        private void Param(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, RuleInterfaceParameterDataType type, object defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(id, $"STRING_OPERATIONS.STATUS.PARAM.{name}.NAME", $"STRING_OPERATIONS.STATUS.PARAM.{name}.DESCRIPTION",
                key, LogicGuid, sort, type, defaultValue, false);
        }

        private void RuleParam(ILogicTemplateFactory factory, Guid id, int rule, string name, string key, int sort, RuleInterfaceParameterDataType type, object defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(id, $"STRING_OPERATIONS.STATUS.PARAM.RULE{rule}_{name}.NAME", $"STRING_OPERATIONS.STATUS.PARAM.RULE_{name}.DESCRIPTION",
                $"rule{rule}{char.ToUpperInvariant(key[0])}{key.Substring(1)}", LogicGuid, sort, type, defaultValue, false);
        }
    }
}
