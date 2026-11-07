using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Scaler
{
    public class ScalerLogicFactory : LogicFactory
    {
        internal const double DefaultInMax = 10;
        internal const double DefaultOutMax = 100;

        public static readonly Guid RuleInput = new Guid("bc23c0d4-3b90-47c6-93af-bbd87edf9759");
        public static readonly Guid RuleParamInMin = new Guid("4d869d07-b777-483c-a25d-f554affa3ffe");
        public static readonly Guid RuleParamInMax = new Guid("1a33e328-dfdd-448a-a8fb-4c3f2e6f3ac0");
        public static readonly Guid RuleParamOutMin = new Guid("094057cd-7559-499d-ab34-e8420162d3fd");
        public static readonly Guid RuleParamOutMax = new Guid("7bf81efd-885c-441c-a315-0c595f9cebff");
        public static readonly Guid RuleParamLimit = new Guid("6c19020d-cd81-4db0-8725-27211692663c");
        public static readonly Guid RuleOutput = new Guid("5c904693-4d7b-4f4f-b61c-5ed3b44eb78d");

        public override string LogicName => "Math.Scaler";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("32b8f5de-2bc7-4753-8e16-67272ab710a7");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.SCALER.NAME", "MATH.SCALER.DESCRIPTION", "math.scaler", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "V", "MATH.SCALER.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.SCALER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamInMin, "MATH.SCALER.IN_MIN.NAME", "MATH.SCALER.IN_MIN.DESCRIPTION", "inMin", LogicGuid, 1, RuleInterfaceParameterDataType.Double, 0d);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamInMax, "MATH.SCALER.IN_MAX.NAME", "MATH.SCALER.IN_MAX.DESCRIPTION", "inMax", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultInMax);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOutMin, "MATH.SCALER.OUT_MIN.NAME", "MATH.SCALER.OUT_MIN.DESCRIPTION", "outMin", LogicGuid, 3, RuleInterfaceParameterDataType.Double, 0d);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOutMax, "MATH.SCALER.OUT_MAX.NAME", "MATH.SCALER.OUT_MAX.DESCRIPTION", "outMax", LogicGuid, 4, RuleInterfaceParameterDataType.Double, DefaultOutMax);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLimit, "MATH.SCALER.LIMIT.NAME", "MATH.SCALER.LIMIT.DESCRIPTION", "limit", LogicGuid, 5, RuleInterfaceParameterDataType.Bool, true);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ScalerRule(context);
        }
    }
}
