using System;
using Automatica.Core.Base.Common;
using Automatica.Core.Driver;

namespace P3.Driver.Times.DriverFactory.DateTime
{
    public class DateTimeDriver : DriverBase
    {
        public DateTimeDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "times-datetime":
                    return new DateTimeNode(ctx, () => System.DateTime.Now);
                case "times-date":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Date);
                case "times-time":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.TimeOfDay);
                case "times-day-of-week":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.DayOfWeek);
                case "times-day-of-month":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Day);
                case "times-month":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Month);
                case "times-year":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Year);
                case "times-milliseconds":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Millisecond);
                case "times-seconds":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Second);
                case "times-minutes":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Minute);
                case "times-hours":
                    return new DateTimeNode(ctx, () => System.DateTime.Now.Hour);
                case "times-running":
                    return new DateTimeNode(ctx, () => Math.Floor((System.DateTime.Now - ServerInfo.StartupTime).TotalSeconds));
                default:
                    return null;
            }
        }
    }
}
