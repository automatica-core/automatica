using Automatica.Core.Driver;
using System;

namespace P3.Driver.Times.DriverFactory.Sun
{
    public class SunDriver : DriverNoneAttributeBase

    {
        private int _timeZoneOffset  = 0;

        public SunDriver(IDriverContext driverContext) : base(driverContext)
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

            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunSetGuid)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunset.AddHours(_timeZoneOffset));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunRiseGuid)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunrise.AddHours(_timeZoneOffset));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunDusk)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunset.AddHours(_timeZoneOffset).AddMinutes(30) > b && b < a.Sunrise.AddMinutes(30));
            }

            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunDawn)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunrise.AddHours(_timeZoneOffset).AddMinutes(30) > b && b < a.Sunset.AddMinutes(30));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunIsRiseGuid)
            {
                return new SunDriverNode(ctx, (a, b) => {
                    return b > a.Sunrise && b < a.Sunset;
                });
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunIsSetGuid)
            {
                return new SunDriverNode(ctx, (a, b) =>
                {
                    return b < a.Sunset && b < a.Sunrise;
                });
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.IsDaylight)
            {
                return new SunDriverNode(ctx, (a, b) =>
                {
                    var sunrise = a.Sunrise;
                    var sunset = a.Sunset;

                    var sunsetTime = new TimeOnly(sunset.Hour, sunset.Minute, sunset.Second);
                    var sunriseTime = new TimeOnly(sunrise.Hour, sunrise.Minute, sunrise.Second);

                    var nowTime = new TimeOnly(b.Hour, b.Minute, b.Second);

                    return nowTime < sunsetTime && nowTime > sunriseTime;
                });
            }

            return null;
        }
    }
}
