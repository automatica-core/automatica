using System;
using Automatica.Core.Base.Common;
using Automatica.Core.Driver;

namespace P3.Driver.Times.DriverFactory.DateTime
{
    public class DateTimeDriver : DriverBase
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
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset));
                case "times-date":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Date);
                case "times-time":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).TimeOfDay);
                case "times-day-of-week":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).DayOfWeek);
                case "times-day-of-month":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Day);
                case "times-month":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Month);
                case "times-year":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Year);
                case "times-milliseconds":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Millisecond);
                case "times-seconds":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Second);
                case "times-minutes":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Minute);
                case "times-hours":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.AddHours(_timeZoneOffset).Hour);
                case "times-running":
                    return new DateTimeNode(ctx, () => Math.Floor((System.DateTime.Now.AddHours(_timeZoneOffset) - ServerInfo.StartupTime).TotalSeconds));
                case "boot-time":
                    return new DateTimeNode(ctx, () => ServerInfo.StartupTime);
                default:
                    return null;
            }
        }
    }
}
