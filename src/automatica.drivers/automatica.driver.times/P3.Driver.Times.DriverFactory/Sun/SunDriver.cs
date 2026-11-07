using Automatica.Core.Driver;
using System;
using System.Runtime.CompilerServices;
using Innovative.SolarCalculator;

[assembly: InternalsVisibleTo("P3.Driver.Times.Tests")]

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
                return new SunDriverNode(ctx, (a, b) => IsDusk(a, b));
            }

            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunDawn)
            {
                return new SunDriverNode(ctx, (a, b) => IsDawn(a, b));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunIsRiseGuid)
            {
                return new SunDriverNode(ctx, (a, b) => IsSunRise(a, b));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunIsSetGuid)
            {
                return new SunDriverNode(ctx, (a, b) => IsSunSet(a, b));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunAzimuthGuid)
            {
                return new SunDriverNode(ctx, (a, b) => Azimuth(a));
            }
            if (ctx.NodeInstance.This2NodeTemplateNavigation.ObjId == SunDriverFactory.SunElevationGuid)
            {
                return new SunDriverNode(ctx, (a, b) => Elevation(a));
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

        // the timezone offset is only applied to the dispatched sunrise/sunset values,
        // the comparisons use the unshifted solar times because "now" is unshifted as well
        internal static readonly TimeSpan TwilightDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Dusk is the time between sunset and sunset + 30 minutes
        /// </summary>
        internal static bool IsDusk(SolarTimes solarTimes, System.DateTime now)
        {
            return now >= solarTimes.Sunset && now < solarTimes.Sunset.Add(TwilightDuration);
        }

        /// <summary>
        /// Dawn is the time between sunrise - 30 minutes and sunrise
        /// </summary>
        internal static bool IsDawn(SolarTimes solarTimes, System.DateTime now)
        {
            return now >= solarTimes.Sunrise.Subtract(TwilightDuration) && now < solarTimes.Sunrise;
        }

        /// <summary>
        /// Sun azimuth in degrees clockwise from north (90 east, 180 south, 270 west), rounded to 0.1° so the value is
        /// dispatched about every 30 seconds and not every second
        /// </summary>
        internal static double Azimuth(SolarTimes solarTimes)
        {
            var azimuth = (double)(decimal)solarTimes.SolarAzimuth % 360;
            return Math.Round(azimuth < 0 ? azimuth + 360 : azimuth, 1);
        }

        /// <summary>
        /// Sun elevation above the horizon in degrees (negative at night), rounded to 0.1°
        /// </summary>
        internal static double Elevation(SolarTimes solarTimes)
        {
            return Math.Round((double)(decimal)solarTimes.SolarElevation, 1);
        }

        internal static bool IsSunRise(SolarTimes solarTimes, System.DateTime now)
        {
            return now > solarTimes.Sunrise && now < solarTimes.Sunset;
        }

        internal static bool IsSunSet(SolarTimes solarTimes, System.DateTime now)
        {
            return now >= solarTimes.Sunset || now < solarTimes.Sunrise;
        }
    }
}
