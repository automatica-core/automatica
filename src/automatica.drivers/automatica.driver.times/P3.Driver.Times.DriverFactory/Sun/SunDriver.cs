using Automatica.Core.Driver;
using Innovative.SolarCalculator;

namespace P3.Driver.Times.DriverFactory.Sun
{
    public class SunDriver : DriverBase

    {
        public SunDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunSetGuid)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunset);
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunRiseGuid)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunrise);
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunDusk)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunset.AddMinutes(30) > b && b < a.Sunrise.AddMinutes(30));
            }

            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunDawn)
            {
                return new SunDriverNode(ctx, (a, b) => a.Sunrise.AddMinutes(30) > b && b < a.Sunset.AddMinutes(30));
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
