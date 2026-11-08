using System;
using Automatica.Core.Shading.Engine;
using Xunit;

namespace Automatica.Core.Tests.Shading
{
    public class SunGeometryTests
    {
        private const double Latitude = 48.2082;
        private const double Longitude = 16.3738;

        [Fact]
        public void Calculate_SolarNoonInVienna_SunIsSouthAndHigh()
        {
            // midsummer, solar noon in Vienna is about 10:56 UTC
            var sun = SunGeometry.Calculate(Latitude, Longitude, new DateTimeOffset(2026, 6, 21, 10, 56, 0, TimeSpan.Zero));

            Assert.InRange(sun.Azimuth, 176, 184);
            // 90 - latitude + 23.44
            Assert.InRange(sun.Elevation, 64, 66.5);
        }

        [Fact]
        public void Calculate_Morning_SunIsEast_Evening_SunIsWest()
        {
            var morning = SunGeometry.Calculate(Latitude, Longitude, new DateTimeOffset(2026, 6, 21, 5, 0, 0, TimeSpan.Zero));
            var evening = SunGeometry.Calculate(Latitude, Longitude, new DateTimeOffset(2026, 6, 21, 17, 0, 0, TimeSpan.Zero));

            Assert.InRange(morning.Azimuth, 70, 110);
            Assert.True(morning.Elevation > 0);
            Assert.InRange(evening.Azimuth, 250, 300);
            Assert.True(evening.Elevation > 0);
        }

        [Fact]
        public void Calculate_Night_ElevationIsNegative()
        {
            var sun = SunGeometry.Calculate(Latitude, Longitude, new DateTimeOffset(2026, 12, 21, 23, 0, 0, TimeSpan.Zero));
            Assert.True(sun.Elevation < 0);
        }

        [Theory]
        [InlineData(180, 180, 0)]
        [InlineData(10, 350, 20)]
        [InlineData(350, 10, -20)]
        [InlineData(90, 270, 180)]
        public void AzimuthDifference_IsSignedAndWraps(double sun, double facade, double expected)
        {
            Assert.Equal(expected, SunGeometry.AzimuthDifference(sun, facade), 6);
        }

        [Theory]
        // in front of the window
        [InlineData(180, 30, 180, true)]
        // 80° to the side is still in, 85° is out
        [InlineData(260, 30, 180, true)]
        [InlineData(265, 30, 180, false)]
        // behind the facade
        [InlineData(0, 30, 180, false)]
        // below the horizon limit (5°) or above the overhang limit (60°)
        [InlineData(180, 3, 180, false)]
        [InlineData(180, 61, 180, false)]
        // facade north-west, wraps around 360
        [InlineData(10, 20, 315, true)]
        public void IsSunOnWindow(double azimuth, double elevation, double facade, bool expected)
        {
            Assert.Equal(expected, SunGeometry.IsSunOnWindow(new SunPosition(azimuth, elevation), facade, 80, 5, 60));
        }

        [Theory]
        // roof window facing south, 30 degrees pitch: the high noon sun is almost straight on the glass
        [InlineData(180, 60, 180, 30, true)]
        // roof facing north, 20 degrees pitch: the high sun from the south still reaches the glass (45 degrees)
        [InlineData(180, 65, 0, 20, true)]
        // ... a low sun from the south does not
        [InlineData(180, 15, 0, 20, false)]
        // flat skylight: any direction counts, only the elevation (incidence 90 - elevation) matters
        [InlineData(37, 20, 180, 0, true)]
        [InlineData(37, 8, 180, 0, false)]
        // a wall (90) keeps the azimuth rule: the sun behind the facade never counts
        [InlineData(180, 65, 0, 90, false)]
        public void IsSunOnWindow_RoofWindow_UsesTheAngleOnTheGlass(double azimuth, double elevation, double facade, double inclination, bool expected)
        {
            Assert.Equal(expected, SunGeometry.IsSunOnWindow(new SunPosition(azimuth, elevation), facade, 80, 5, 90, inclination));
        }

        [Fact]
        public void CosIncidence_WallFlatAndTilted()
        {
            var sun = new SunPosition(200, 40);

            // wall: cos(elevation) * cos(azimuth difference)
            Assert.Equal(Math.Cos(40 * Math.PI / 180) * Math.Cos(20 * Math.PI / 180), SunGeometry.CosIncidence(sun, 180, 90), 9);
            // flat: sin(elevation), whatever the azimuth
            Assert.Equal(Math.Sin(40 * Math.PI / 180), SunGeometry.CosIncidence(sun, 0, 0), 9);
            // the glass faces the sun exactly: incidence 0
            Assert.Equal(1, SunGeometry.CosIncidence(new SunPosition(180, 55), 180, 35), 9);
            Assert.InRange(SunGeometry.IncidenceAngle(new SunPosition(180, 55), 180, 35), 0, 0.01);
            Assert.Equal(60, SunGeometry.IncidenceAngle(new SunPosition(180, 30), 180, 0), 6);
        }

        [Fact]
        public void Illuminance_RoofWindow_GetsMoreHighSunThanAWall_AndAWallIsUnchanged()
        {
            var sun = new SunPosition(180, 65);
            var sky = SunGeometry.ClearSky(sun.Elevation);

            var wall = SunGeometry.Illuminance(sky, sun, 180);
            Assert.Equal(wall, SunGeometry.Illuminance(sky, sun, 180, 90), 9);

            // the old formula of the wall: direct * cos h * cos(az) + half the diffuse + half the ground reflection
            var expected = 105 * (sky.DirectNormal * Math.Cos(65 * Math.PI / 180) + 0.5 * sky.DiffuseHorizontal + 0.5 * 0.2 * sky.GlobalHorizontal);
            Assert.Equal(expected, wall, 6);

            var roof = SunGeometry.Illuminance(sky, sun, 180, 35);
            Assert.True(roof > 1.8 * wall, $"roof {roof} wall {wall}");

            // a flat skylight sees the whole sky and no ground: like the horizontal brightness
            Assert.Equal(105 * sky.GlobalHorizontal, SunGeometry.Illuminance(sky, sun, 0, 0), 6);

            // the estimate of a roof facing north still sees the high sun
            Assert.True(SunGeometry.EstimateIlluminance(sun, 0, null, null, 20) > 50000);
        }

        [Fact]
        public void ProfileAngle_SunInFront_IsElevation_SunAtTheSide_IsSteeper_SunBehind_IsNull()
        {
            Assert.Equal(30, SunGeometry.ProfileAngle(new SunPosition(180, 30), 180)!.Value, 6);
            // atan(tan 30° / cos 60°) = 49.1°
            Assert.Equal(49.1066, SunGeometry.ProfileAngle(new SunPosition(240, 30), 180)!.Value, 3);
            Assert.Null(SunGeometry.ProfileAngle(new SunPosition(0, 30), 180));
            Assert.Null(SunGeometry.ProfileAngle(new SunPosition(180, -2), 180));
        }

        [Fact]
        public void CutOffSlatAngle()
        {
            // slats as wide as their spacing: horizontal sun needs closed slats, sun at 30° needs 30°
            Assert.Equal(90, SunGeometry.CutOffSlatAngle(0, 80, 80), 6);
            Assert.Equal(30, SunGeometry.CutOffSlatAngle(30, 80, 80), 6);

            // 80 mm slats, 72 mm spacing: asin(0.9) = 64.16° at a horizontal sun
            Assert.Equal(64.158, SunGeometry.CutOffSlatAngle(0, 80, 72), 3);

            // sun from above: slats can be horizontal
            Assert.Equal(0, SunGeometry.CutOffSlatAngle(90, 80, 72), 6);

            // the higher the sun, the more open
            Assert.True(SunGeometry.CutOffSlatAngle(20, 80, 72) > SunGeometry.CutOffSlatAngle(40, 80, 72));

            // spacing wider than the slats cannot block a low sun
            Assert.Equal(90, SunGeometry.CutOffSlatAngle(0, 50, 72), 6);
        }

        [Fact]
        public void EstimateIlluminance_ClearSkyOnFacade_IsBright_Clouds_AndSunBehind_AreDark()
        {
            var sun = new SunPosition(180, 30);

            var clear = SunGeometry.EstimateIlluminance(sun, 180, null);
            var overcast = SunGeometry.EstimateIlluminance(sun, 180, 100);
            var behind = SunGeometry.EstimateIlluminance(sun, 0, null);
            var night = SunGeometry.EstimateIlluminance(new SunPosition(180, -5), 180, null);

            Assert.True(clear > 50000, $"clear {clear}");
            Assert.True(overcast < 12000, $"overcast {overcast}");
            Assert.True(behind < 12000, $"behind {behind}");
            Assert.Equal(0, night);

            // low morning sun straight on an east facade still blinds
            Assert.True(SunGeometry.EstimateIlluminance(new SunPosition(90, 10), 90, null) > 20000);

            // horizontal (global) brightness at a high sun: about 100 klx
            Assert.InRange(SunGeometry.EstimateIlluminance(new SunPosition(180, 65), null, null), 80000, 125000);
        }
    }
}
