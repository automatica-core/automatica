using System;
using System.Collections.Specialized;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Gauge.RangeCircular
{
    public class RangeCircularControlFactory : LogicFactory
    {
        public override string LogicName => "P3.Logic.Operations.Gauge.RangeCircular";
        public override Guid LogicGuid => new Guid("d509db23-fa64-4496-b3ee-2c0b6dff0abd");
        public override Version LogicVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.GAUGE.RANGE_CIRCULAR.NAME", "OPERATIONS.GAUGE.RANGE_CIRCULAR.DESCRIPTION",
                "operations-gauge-easy-circular", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("519dc496-ded8-46f5-8c73-ccb6216b1bba"), "OPERATIONS.GAUGE.VALUE.NAME", "OPERATIONS.GAUGE.VALUE.DESCRIPTION", "value",LogicGuid, LogicInterfaceDirection.Input, 1, 1, RuleInterfaceType.Input);

            factory.CreateParameterLogicInterfaceTemplate(
                new Guid("180188c4-e72b-4248-8000-b9d3c4bd6bbc"),
                "OPERATIONS.GAUGE.TICKS.NAME",
                "OPERATIONS.GAUGE.TICKS.DESCRIPTION",
                "ticks",
                LogicGuid,
                99,
                RuleInterfaceParameterDataType.Integer,
                "5",
                true);

            var count = 0;
            count = GenerateRangeScaleProperties(factory, "first", 0, 0, 33, "red", count);
            count = GenerateRangeScaleProperties(factory, "second", 1, 34, 66, "orange", count);
            count = GenerateRangeScaleProperties(factory, "third", 2, 67, 100, "green", count);


            factory.CreateParameterLogicInterfaceTemplate(new Guid("67dfb610-5ef1-4d75-a6ec-e2d91bc6e9f5"), "OPERATIONS.GAUGE.TYPE", "OPERATIONS.GAUGE.TYPE", "gauge_type", LogicGuid, 0,
                RuleInterfaceParameterDataType.ConstantString, $"{(int)GaugeType.CircularThreeRange}");


            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.ThreeRangeGauge);
        }

        private int GenerateRangeScaleProperties(ILogicTemplateFactory factory, string name, int index, int defaultStart, int defaultEnd,
            string defaultColor, int count)
        {
            factory.CreateParameterLogicInterfaceTemplate(
                GenerateNewGuid(new Guid("01bc8d9c-167d-4877-8942-3a7f7bbc399d"), index),
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_SCALE_START.NAME",
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_SCALE_START.DESCRIPTION",
                $"{name}_scale_start",
                LogicGuid,
                count++,
                RuleInterfaceParameterDataType.Integer,
                $"{defaultStart}",
            true);


            factory.CreateParameterLogicInterfaceTemplate(
                GenerateNewGuid(new Guid("f76b72b1-b018-47d7-b775-29fec9a01563"), index),
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_SCALE_END.NAME",
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_SCALE_END.DESCRIPTION",
                $"{name}_scale_end",
                LogicGuid,
                count++,
                RuleInterfaceParameterDataType.Integer,
                $"{defaultEnd}",
                true);

            factory.CreateParameterLogicInterfaceTemplate(
                GenerateNewGuid(new Guid("3e980316-b05a-4453-b62a-f250ffec4c76"), index),
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_COLOR.NAME",
                $"OPERATIONS.GAUGE.{name.ToUpperInvariant()}_COLOR.DESCRIPTION",
                $"{name}_color",
                LogicGuid,
                count++,
                RuleInterfaceParameterDataType.Color,
                defaultColor,
                true);
            return count;
        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[^1] = (byte)(Convert.ToInt32(gu[^1]) + c);

            return new Guid(gu);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RangeCircularControl(context);
        }
    }
}
