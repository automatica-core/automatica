using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Pid
{
    public class PidControllerLogicFactory : LogicFactory
    {
        internal const double DefaultKp = 10;
        internal const double DefaultKi = 0.01;
        internal const double DefaultCycle = 10;
        internal const double DefaultMax = 100;

        public static readonly Guid RuleActual = new Guid("68081a1c-2203-40b9-8158-a6572c8cfa9c");
        public static readonly Guid RuleSetpoint = new Guid("6dba31ca-4334-47ae-983f-05cc1d60a49c");
        public static readonly Guid RuleEnable = new Guid("d9c70c68-5d6a-469b-a1f3-bdc1de184d35");
        public static readonly Guid RuleParamKp = new Guid("9eb2caa9-65ef-4a29-a618-7c584491ed43");
        public static readonly Guid RuleParamKi = new Guid("81cbdab4-5330-4298-89ce-3c6f0ae0136d");
        public static readonly Guid RuleParamKd = new Guid("ac8f6957-f605-4ae7-bc9c-c3df94590e93");
        public static readonly Guid RuleParamCycle = new Guid("55afe151-5e28-404f-9705-b23df1cf8daa");
        public static readonly Guid RuleParamMin = new Guid("3dd62b2d-7a03-400d-bb32-7b628fab513c");
        public static readonly Guid RuleParamMax = new Guid("c225db78-e59f-466b-9811-63932417d0b8");
        public static readonly Guid RuleOutput = new Guid("bb0419d0-07dd-4a48-8649-3a52fdb04920");

        public override string LogicName => "Math.PidController";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("6a4b36c5-384d-418f-8a17-02bb10621ed8");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.PID.NAME", "MATH.PID.DESCRIPTION", "math.pid", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleActual, "AI", "MATH.PID.ACTUAL.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleSetpoint, "SP", "MATH.PID.SETPOINT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);
            factory.CreateLogicInterfaceTemplate(RuleEnable, "EN", "MATH.PID.ENABLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "MATH.PID.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamKp, "MATH.PID.KP.NAME", "MATH.PID.KP.DESCRIPTION", "kp", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultKp);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamKi, "MATH.PID.KI.NAME", "MATH.PID.KI.DESCRIPTION", "ki", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultKi);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamKd, "MATH.PID.KD.NAME", "MATH.PID.KD.DESCRIPTION", "kd", LogicGuid, 3, RuleInterfaceParameterDataType.Double, 0d);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamCycle, "MATH.PID.CYCLE.NAME", "MATH.PID.CYCLE.DESCRIPTION", "cycle", LogicGuid, 4, RuleInterfaceParameterDataType.Double, DefaultCycle);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMin, "MATH.PID.MIN.NAME", "MATH.PID.MIN.DESCRIPTION", "min", LogicGuid, 5, RuleInterfaceParameterDataType.Double, 0d);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMax, "MATH.PID.MAX.NAME", "MATH.PID.MAX.DESCRIPTION", "max", LogicGuid, 6, RuleInterfaceParameterDataType.Double, DefaultMax);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PidControllerRule(context);
        }
    }
}
