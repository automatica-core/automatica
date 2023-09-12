using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.EasyLinear
{
    public class EasyLinearControlFactory : LogicFactory
    {
        public override string LogicName => "P3.Logic.Operations.Gauge.EasyLinear";
        public override Guid LogicGuid => new Guid("9c746a50-11dc-485e-8e58-d3ea9f03c1c7");
        public override Version LogicVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.GAUGE.EASY_LINEAR.NAME", "OPERATIONS.GAUGE.EASY_LINEAR.DESCRIPTION",
                "operations-gauge-easy-linear", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("75042f29-6258-446f-8e28-260ee541b98d"), "OPERATIONS.GAUGE.VALUE.NAME", "OPERATIONS.GAUGE.VALUE.DESCRIPTION", "value",LogicGuid, LogicInterfaceDirection.Input, 1, 1, RuleInterfaceType.Input);

            //factory.CreateLogicInterfaceTemplate(RuleOutput, "OPERATIONS.PUSH.OUTPUT.NAME", "OPERATIONS.PUSH.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("be13f0ca-e80d-4703-a2ed-6ed8332ecf39"),
                "OPERATIONS.GAUGE.SCALE_START.NAME",
                "OPERATIONS.GAUGE.SCALE_START.DESCRIPTION",
                "scale_start",
                LogicGuid,
                1,
                RuleInterfaceParameterDataType.Integer,
                "0",
                true);


            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("4de9b822-3652-4698-8b37-1717b1fc5129"),
                "OPERATIONS.GAUGE.SCALE_END.NAME",
                "OPERATIONS.GAUGE.SCALE_END.DESCRIPTION",
                "scale_end",
                LogicGuid,
                2,
                RuleInterfaceParameterDataType.Integer,
                "100",
                true);

            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("24475a71-c49f-4cec-97b6-861526b169f2"),
                "OPERATIONS.GAUGE.TICKS.NAME",
                "OPERATIONS.GAUGE.TICKS.DESCRIPTION",
                "ticks",
                LogicGuid,
                3,
                RuleInterfaceParameterDataType.Integer,
                "1",
                true);



            factory.CreateParameterLogicInterfaceTemplate(new Guid("1757d393-86a4-4d17-b76e-0c9f46eb0991"), "OPERATIONS.GAUGE.TYPE.NAME", "OPERATIONS.GAUGE.TYPE.DESCRIPTION", "gauge_type",LogicGuid, 0,
                RuleInterfaceParameterDataType.ConstantString, $"{(int)GaugeType.Linear}", false);


            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Gauge);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EasyLinearControl(context);
        }
    }
}
