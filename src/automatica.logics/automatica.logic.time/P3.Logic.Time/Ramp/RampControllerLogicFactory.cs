using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.Ramp
{
    public class RampControllerLogicFactory : LogicFactory
    {
        internal const double DefaultStart = 5;
        internal const double DefaultStep = 1;
        internal const double DefaultLevel1 = 7;
        internal const double DefaultLevel2 = 3;

        public static readonly Guid RuleSelect = new Guid("859ade34-819d-4a20-a995-015820181443");
        public static readonly Guid RuleStop = new Guid("e3fe2cc0-c2df-4483-8877-4d5825742650");
        public static readonly Guid RuleOff = new Guid("a4e2ad57-074f-4353-83cc-94d7ca6cf207");
        public static readonly Guid RuleOutput = new Guid("8c3a9d60-6e1e-4195-b13c-523d5f8794b1");
        public static readonly Guid RuleParamStart = new Guid("4d944f2e-2df1-4dbd-8881-5ff75402b0af");
        public static readonly Guid RuleParamStep = new Guid("470c7336-0e13-4058-b383-435676a6ae49");
        public static readonly Guid RuleParamLevel1 = new Guid("6efc8791-428f-4b68-96ef-6d6f8fad0ab7");
        public static readonly Guid RuleParamLevel2 = new Guid("96ecb6d9-c7fc-4bdd-94b4-9e9a9465c9f9");

        public override string LogicName => "Time.RampController";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("4fb91cc3-45e0-4bc8-a891-f96cd3a01a94");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.RAMP.NAME", "TIME.RAMP.DESCRIPTION", "time.ramp", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleSelect, "S", "TIME.RAMP.SELECT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleStop, "St", "TIME.RAMP.STOP.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "V", "TIME.RAMP.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamStart, "TIME.RAMP.START.NAME", "TIME.RAMP.START.DESCRIPTION", "sv", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultStart);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamStep, "TIME.RAMP.STEP.NAME", "TIME.RAMP.STEP.DESCRIPTION", "sts", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultStep);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLevel1, "TIME.RAMP.LEVEL1.NAME", "TIME.RAMP.LEVEL1.DESCRIPTION", "l1", LogicGuid, 3, RuleInterfaceParameterDataType.Double, DefaultLevel1);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamLevel2, "TIME.RAMP.LEVEL2.NAME", "TIME.RAMP.LEVEL2.DESCRIPTION", "l2", LogicGuid, 4, RuleInterfaceParameterDataType.Double, DefaultLevel2);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RampControllerRule(context);
        }
    }
}
