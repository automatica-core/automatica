using System;
using Automatica.Core.Base.Common;
using Automatica.Core.Driver;

namespace P3.Driver.Times.DriverFactory.DateTime
{
    public class DateTimeDriver : DriverNoneAttributeBase
    {
        private readonly int _timeZoneOffset = 0;

        public DateTimeDriver(IDriverContext driverContext) : base(driverContext)
        {
            var timeZoneOffset = driverContext.NodeTemplateFactory.GetSetting("timezoneOffset");

            try
            {
                if (timeZoneOffset != null)
                {
                    _timeZoneOffset = timeZoneOffset.ValueInt.Value;
                }
                else
                {
                    _timeZoneOffset = 0;
                }
            }
            catch
            {
                //ignore exception
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "times-datetime":
                    return new DateTimeNode<System.DateTime>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset));
                case "times-date":
                    return new DateTimeNode<DateOnly>(ctx, () => DateOnly.FromDateTime(System.DateTime.Now.AddHours(_timeZoneOffset).Date));
                case "times-time":
                    return new DateTimeNode<TimeOnly>(ctx, () => TimeOnly.FromDateTime(System.DateTime.Now.AddHours(_timeZoneOffset)));
                case "times-day-of-week":
                    return new DateTimeNode<DayOfWeek>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).DayOfWeek);
                case "times-day-of-month":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Day);
                case "times-month":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Month);
                case "times-year":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Year);
                case "times-milliseconds":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Millisecond);
                case "times-seconds":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Second);
                case "times-minutes":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Minute);
                case "times-hours":
                    return new DateTimeNode<int>(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Hour);
                case "times-running":
                    return new DateTimeNode<double>(ctx, () => Math.Floor((System.DateTime.Now.AddHours(_timeZoneOffset) - ServerInfo.StartupTime).TotalSeconds));
                case "boot-time":
                    return new DateTimeNode<System.DateTime>(ctx, () => ServerInfo.StartupTime.AddHours(_timeZoneOffset));
                default:
                    return null;
            }
        }
    }
}
