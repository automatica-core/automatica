using Innovative.Geometry;
using Innovative.SolarCalculator;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// Sun position in degrees: azimuth clockwise from north (90 east, 180 south, 270 west), elevation above the horizon
    /// </summary>
    public readonly record struct SunPosition(double Azimuth, double Elevation);

    /// <summary>
    /// Irradiance of the sky (W/m²): direct normal, diffuse horizontal and global horizontal
    /// </summary>
    public readonly record struct SkyIrradiance(double DirectNormal, double DiffuseHorizontal, double GlobalHorizontal);

    /// <summary>
    /// Geometry of sun, facades and slats. Pure functions, all angles in degrees.
    /// </summary>
    public static class SunGeometry
    {
        /// <summary>
        /// Luminous efficacy of sunlight (lm/W), converts irradiance to illuminance
        /// </summary>
        internal const double LuminousEfficacy = 105;

        public static SunPosition Calculate(double latitude, double longitude, DateTimeOffset at)
        {
            var solarTimes = new SolarTimes(at, new Angle(latitude), new Angle(longitude));
            return new SunPosition(Normalize360(ToDouble(solarTimes.SolarAzimuth)), ToDouble(solarTimes.SolarElevation));
        }

        private static double ToDouble(Angle angle) => (double)(decimal)angle;

        public static double Normalize360(double angle)
        {
            angle %= 360;
            return angle < 0 ? angle + 360 : angle;
        }

        /// <summary>
        /// Signed difference between sun azimuth and facade direction (-180..180)
        /// </summary>
        public static double AzimuthDifference(double sunAzimuth, double facadeAzimuth)
        {
            var diff = Normalize360(sunAzimuth - facadeAzimuth);
            return diff > 180 ? diff - 360 : diff;
        }

        /// <summary>
        /// True when the sun is in front of the window: at most <paramref name="acceptanceAngle"/> left or right of the
        /// facade direction and between <paramref name="minElevation"/> (horizon) and <paramref name="maxElevation"/> (overhang)
        /// </summary>
        public static bool IsSunOnWindow(SunPosition sun, double facadeAzimuth, double acceptanceAngle, double minElevation, double maxElevation)
        {
            if (sun.Elevation <= Math.Max(0, minElevation) || sun.Elevation > maxElevation)
            {
                return false;
            }

            return Math.Abs(AzimuthDifference(sun.Azimuth, facadeAzimuth)) <= Math.Min(acceptanceAngle, 90);
        }

        /// <summary>
        /// Profile angle (vertical shadow angle): elevation of the sun projected onto the plane perpendicular to the facade.
        /// Null when the sun is behind the facade.
        /// </summary>
        public static double? ProfileAngle(SunPosition sun, double facadeAzimuth)
        {
            if (sun.Elevation <= 0)
            {
                return null;
            }

            var cos = Math.Cos(ToRadians(AzimuthDifference(sun.Azimuth, facadeAzimuth)));
            if (cos <= 0.0001)
            {
                return null;
            }

            return ToDegrees(Math.Atan(Math.Tan(ToRadians(sun.Elevation)) / cos));
        }

        /// <summary>
        /// Smallest slat angle (0 horizontal, 90 closed) that blocks direct sunlight at the profile angle
        /// <paramref name="profileAngle"/>: light passes between two slats as long as
        /// <c>width * sin(angle + profile) &lt; spacing * cos(profile)</c>.
        /// </summary>
        public static double CutOffSlatAngle(double profileAngle, double width, double spacing)
        {
            if (width <= 0 || spacing <= 0)
            {
                return 90;
            }

            var gamma = ToRadians(Math.Clamp(profileAngle, 0, 90));
            var ratio = spacing * Math.Cos(gamma) / width;
            if (ratio >= 1)
            {
                return 90;
            }

            return Math.Clamp(ToDegrees(Math.Asin(ratio) - gamma), 0, 90);
        }

        /// <summary>
        /// Clear sky direct normal irradiance (W/m²): Meinel model with the Kasten-Young air mass
        /// </summary>
        internal static double ClearSkyDirectNormal(double elevation)
        {
            if (elevation <= 0)
            {
                return 0;
            }

            var airMass = 1 / (Math.Sin(ToRadians(elevation)) + 0.50572 * Math.Pow(elevation + 6.07995, -1.6364));
            return 1353 * Math.Pow(0.7, Math.Pow(airMass, 0.678));
        }

        /// <summary>
        /// Normal irradiance outside the atmosphere (W/m²), varies with the distance to the sun
        /// </summary>
        internal static double ExtraterrestrialNormal(DateTimeOffset at) => 1367 * (1 + 0.033 * Math.Cos(2 * Math.PI * at.DayOfYear / 365.0));

        /// <summary>
        /// Clear sky irradiance: Meinel direct normal, 10 % of it as diffuse light
        /// </summary>
        public static SkyIrradiance ClearSky(double elevation)
        {
            if (elevation <= 0)
            {
                return default;
            }

            var directNormal = ClearSkyDirectNormal(elevation);
            var diffuse = 0.1 * directNormal;
            return new SkyIrradiance(directNormal, diffuse, directNormal * Math.Sin(ToRadians(elevation)) + diffuse);
        }

        /// <summary>
        /// Erbs decomposition of the global horizontal irradiance into direct normal and diffuse horizontal
        /// </summary>
        private static (double DirectNormal, double Diffuse) Erbs(double globalHorizontal, double sinElevation, double extraterrestrial)
        {
            var kt = Math.Clamp(globalHorizontal / (extraterrestrial * sinElevation), 0, 1);
            var diffuseFraction = kt <= 0.22
                ? 1 - 0.09 * kt
                : kt <= 0.8
                    ? 0.9511 - 0.1604 * kt + 4.388 * kt * kt - 16.638 * Math.Pow(kt, 3) + 12.336 * Math.Pow(kt, 4)
                    : 0.165;
            var diffuse = diffuseFraction * globalHorizontal;
            return ((globalHorizontal - diffuse) / sinElevation, diffuse);
        }

        /// <summary>
        /// Sky state from a global horizontal irradiance (measured, or estimated from the cloud cover). The direct part
        /// is the Erbs estimate relative to the Erbs estimate of the clear sky, applied to the clear sky direct normal
        /// irradiance; that keeps a cloudless sky equal to <see cref="ClearSky"/> and avoids huge values at low sun.
        /// </summary>
        public static SkyIrradiance FromGlobalHorizontal(double globalHorizontal, double elevation, DateTimeOffset at)
        {
            globalHorizontal = Math.Max(0, globalHorizontal);
            if (elevation <= 0)
            {
                return new SkyIrradiance(0, globalHorizontal, globalHorizontal);
            }

            var sinElevation = Math.Sin(ToRadians(elevation));
            var extraterrestrial = ExtraterrestrialNormal(at);
            var clear = ClearSky(elevation);
            var (clearDirect, _) = Erbs(clear.GlobalHorizontal, sinElevation, extraterrestrial);
            var (direct, diffuse) = Erbs(globalHorizontal, sinElevation, extraterrestrial);
            var ratio = clearDirect > 0 ? Math.Clamp(direct / clearDirect, 0, 1.2) : 0;
            return new SkyIrradiance(clear.DirectNormal * ratio, diffuse, globalHorizontal);
        }

        /// <summary>
        /// Sky state from the cloud cover: Kasten-Czeplak reduces the clear sky global irradiance, Erbs splits it
        /// </summary>
        public static SkyIrradiance FromCloudCover(double elevation, double cloudCoverPercent, DateTimeOffset at)
        {
            var cloud = Math.Clamp(cloudCoverPercent / 100.0, 0, 1);
            var clear = ClearSky(elevation);
            return FromGlobalHorizontal(clear.GlobalHorizontal * (1 - 0.75 * Math.Pow(cloud, 3.4)), elevation, at);
        }

        /// <summary>
        /// Sky state from the irradiance on a tilted plane (PV panels, W/m²). The clear sky diffuse light is taken as the
        /// base, what is left is the share of the clear sky direct sun. Null when the sun hardly shines onto the panels,
        /// then the direct part cannot be told from the diffuse one.
        /// </summary>
        public static SkyIrradiance? FromPlaneOfArray(double planeIrradiance, SunPosition sun, double panelAzimuth, double tilt)
        {
            if (sun.Elevation < 3)
            {
                return null;
            }

            var h = ToRadians(sun.Elevation);
            var t = ToRadians(tilt);
            var cosIncidence = Math.Sin(h) * Math.Cos(t) + Math.Cos(h) * Math.Sin(t) * Math.Cos(ToRadians(AzimuthDifference(sun.Azimuth, panelAzimuth)));
            if (cosIncidence < 0.2)
            {
                return null;
            }

            var clear = ClearSky(sun.Elevation);
            var skyView = (1 + Math.Cos(t)) / 2;
            var groundView = GroundReflectance * (1 - Math.Cos(t)) / 2;
            var clearDiffuse = clear.DiffuseHorizontal * skyView + clear.GlobalHorizontal * groundView;
            var clearDirect = clear.DirectNormal * cosIncidence;

            planeIrradiance = Math.Max(0, planeIrradiance);
            var share = clearDirect > 0 ? Math.Clamp((planeIrradiance - clearDiffuse) / clearDirect, 0, 1.2) : 0;
            var diffuse = share > 0 ? clear.DiffuseHorizontal : planeIrradiance / (skyView + groundView);
            var directNormal = clear.DirectNormal * share;
            return new SkyIrradiance(directNormal, diffuse, directNormal * Math.Sin(h) + diffuse);
        }

        /// <summary>
        /// Plane irradiance (W/m²) from the power of a PV system
        /// </summary>
        public static double PlaneIrradianceFromPower(double watts, double peakWatts, double performanceRatio) =>
            peakWatts <= 0 || performanceRatio <= 0 ? 0 : Math.Max(0, watts) / (peakWatts * performanceRatio) * 1000;

        internal const double GroundReflectance = 0.2;

        /// <summary>
        /// Illuminance (lux) on a vertical facade: direct sun by the angle of incidence, half of the diffuse sky and 20 %
        /// ground reflection from the lower half. With <paramref name="facadeAzimuth"/> null it is the illuminance on a
        /// horizontal surface (global brightness).
        /// </summary>
        public static double Illuminance(SkyIrradiance sky, SunPosition sun, double? facadeAzimuth)
        {
            if (!facadeAzimuth.HasValue)
            {
                return LuminousEfficacy * sky.GlobalHorizontal;
            }

            var cosIncidence = sun.Elevation <= 0
                ? 0
                : Math.Cos(ToRadians(sun.Elevation)) * Math.Cos(ToRadians(AzimuthDifference(sun.Azimuth, facadeAzimuth.Value)));
            var direct = sky.DirectNormal * Math.Max(0, cosIncidence);
            var diffuse = 0.5 * sky.DiffuseHorizontal;
            var reflected = 0.5 * GroundReflectance * sky.GlobalHorizontal;
            return LuminousEfficacy * (direct + diffuse + reflected);
        }

        /// <summary>
        /// Estimated illuminance (lux) on a facade from the clear sky model, reduced by the cloud cover when it is known
        /// </summary>
        public static double EstimateIlluminance(SunPosition sun, double? facadeAzimuth, double? cloudCoverPercent, DateTimeOffset? at = null)
        {
            if (sun.Elevation <= 0)
            {
                return 0;
            }

            var sky = cloudCoverPercent.HasValue
                ? FromCloudCover(sun.Elevation, cloudCoverPercent.Value, at ?? new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero))
                : ClearSky(sun.Elevation);
            return Illuminance(sky, sun, facadeAzimuth);
        }

        internal static double ToRadians(double degrees) => degrees * Math.PI / 180;
        internal static double ToDegrees(double radians) => radians * 180 / Math.PI;
    }
}
