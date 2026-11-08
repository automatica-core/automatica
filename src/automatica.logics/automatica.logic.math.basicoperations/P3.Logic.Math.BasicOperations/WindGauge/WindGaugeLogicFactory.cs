using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.WindGauge
{
    /// <summary>
    /// Wind gauge: Turns the frequency of a wind sensor into the wind speed: average, 3 second gust, maximum and wind alarm (modelled on the Loxone function block of the same name)
    /// </summary>
    public class WindGaugeLogicFactory : LogicFactory
    {
        public static readonly Guid InputFrequency = new Guid("c32500b4-46cd-595d-b3ab-d6a2629fab8c");
        public static readonly Guid OutputAverage = new Guid("338889ad-663f-5f88-85d1-3f78a1bb6ac0");
        public static readonly Guid OutputGust = new Guid("35f135ba-4da8-529d-99d8-26d221ca461c");
        public static readonly Guid OutputMaximum = new Guid("de1f5eac-0175-5105-856c-0abbed841e5e");
        public static readonly Guid OutputAlarm = new Guid("23983fd5-f474-52c3-a4a1-0edca746c974");
        public static readonly Guid ParamAveragingTime = new Guid("fae63d9b-b1c8-506b-aeb5-e45b760de3ba");
        public static readonly Guid ParamFactor = new Guid("6247cb76-6bf7-5606-8afc-957020716d76");
        public static readonly Guid ParamAlarmSpeed = new Guid("ed21606b-19f6-5440-b8c9-c7f0194abbe8");


        public override string LogicName => "Math.WindGauge";
        public override Guid LogicGuid => new Guid("f60d41ab-540a-5ed5-a730-f524a7c930c9");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.WIND_GAUGE.NAME", "MATH.WIND_GAUGE.DESCRIPTION", "math.wind-gauge", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputFrequency, "MATH.WIND_GAUGE.INPUT.FREQUENCY.NAME", "MATH.WIND_GAUGE.INPUT.FREQUENCY.DESCRIPTION", "frequency", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputAverage, "MATH.WIND_GAUGE.OUTPUT.AVERAGE.NAME", "MATH.WIND_GAUGE.OUTPUT.AVERAGE.DESCRIPTION", "average", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputGust, "MATH.WIND_GAUGE.OUTPUT.GUST.NAME", "MATH.WIND_GAUGE.OUTPUT.GUST.DESCRIPTION", "gust", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputMaximum, "MATH.WIND_GAUGE.OUTPUT.MAXIMUM.NAME", "MATH.WIND_GAUGE.OUTPUT.MAXIMUM.DESCRIPTION", "maximum", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputAlarm, "MATH.WIND_GAUGE.OUTPUT.ALARM.NAME", "MATH.WIND_GAUGE.OUTPUT.ALARM.DESCRIPTION", "alarm", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateParameterLogicInterfaceTemplate(ParamAveragingTime, "MATH.WIND_GAUGE.PARAM.AVERAGING_TIME.NAME", "MATH.WIND_GAUGE.PARAM.AVERAGING_TIME.DESCRIPTION", "averagingTime", LogicGuid, 1, RuleInterfaceParameterDataType.Double, 10.0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamFactor, "MATH.WIND_GAUGE.PARAM.FACTOR.NAME", "MATH.WIND_GAUGE.PARAM.FACTOR.DESCRIPTION", "factor", LogicGuid, 2, RuleInterfaceParameterDataType.Double, 1.0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamAlarmSpeed, "MATH.WIND_GAUGE.PARAM.ALARM_SPEED.NAME", "MATH.WIND_GAUGE.PARAM.ALARM_SPEED.DESCRIPTION", "alarmSpeed", LogicGuid, 3, RuleInterfaceParameterDataType.Double, 50.0, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new WindGaugeRule(context);
        }
    }
}
