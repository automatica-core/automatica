using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.EasyCircular
{
    public class EasyCircularControlFactory : LogicFactory
    {
        public override string LogicName => "P3.Logic.Operations.Gauge.EasyCircular";
        public override Guid LogicGuid => new Guid("7d26048b-ff5a-4314-a092-1f55e881d87c");
        public override Version LogicVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.GAUGE.EASY_CIRCULAR.NAME", "OPERATIONS.GAUGE.EASY_CIRCULAR.DESCRIPTION",
                "operations-gauge-easy-circular", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("f7747609-7162-4770-8d86-3560453e97cd"), "OPERATIONS.GAUGE.VALUE.NAME", "OPERATIONS.GAUGE.VALUE.DESCRIPTION", "value",LogicGuid, LogicInterfaceDirection.Input, 1, 1, RuleInterfaceType.Input);

            //factory.CreateLogicInterfaceTemplate(RuleOutput, "OPERATIONS.PUSH.OUTPUT.NAME", "OPERATIONS.PUSH.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("44fa1635-04fc-468e-8727-0911548caf60"),
                "OPERATIONS.GAUGE.SCALE_START.NAME",
                "OPERATIONS.GAUGE.SCALE_START.DESCRIPTION",
                "scale_start",
                LogicGuid,
                1,
                RuleInterfaceParameterDataType.Integer,
                "0",
                true);


            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("21da44be-54a1-432b-86ce-a70dfc4788b2"),
                "OPERATIONS.GAUGE.SCALE_END.NAME",
                "OPERATIONS.GAUGE.SCALE_END.DESCRIPTION",
                "scale_end",
                LogicGuid,
                1,
                RuleInterfaceParameterDataType.Integer,
                "100",
                true);

            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("023c67c9-e0de-4981-b3b3-a589699496b5"),
                "OPERATIONS.GAUGE.TICKS.NAME",
                "OPERATIONS.GAUGE.TICKS.DESCRIPTION",
                "ticks",
                LogicGuid,
                1,
                RuleInterfaceParameterDataType.Integer,
                "1",
                true);

            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("f6951bc2-cbba-4314-9fb0-ffb558d988bf"),
                "OPERATIONS.GAUGE.TICKS.NAME",
                "OPERATIONS.GAUGE.TICKS.DESCRIPTION",
                "type",
                LogicGuid,
                1,
                RuleInterfaceParameterDataType.Integer,
                "1",
                true);

            factory.CreateParameterLogicInterfaceTemplate(new Guid("a074af85-00f7-49b7-9815-d963eaae1fd0"), "OPERATIONS.GAUGE.TYPE", "OPERATIONS.GAUGE.TYPE", LogicGuid, 0,
                RuleInterfaceParameterDataType.NoParameter, (int)GaugeType.Circular);


            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Gauge);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EasyCircularControl(context);
        }
    }
}
