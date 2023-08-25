using Automatica.Core.Driver;

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

            return null;
        }
    }
}
