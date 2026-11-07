using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Innovative.Geometry;
using Innovative.SolarCalculator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using P3.Driver.Times.DriverFactory;
using P3.Driver.Times.DriverFactory.DateTime;
using P3.Driver.Times.DriverFactory.Sun;
using Xunit;

namespace P3.Driver.Times.Tests
{
    /// <summary>The sun windows over a whole year of fixed dates (no dependency on today).</summary>
    public class SunWindowTests
    {
        private static readonly Angle Latitude = new(48.1341337);
        private static readonly Angle Longitude = new(14.1515088);

        public static TheoryData<DateTime> Days()
        {
            var data = new TheoryData<DateTime>();
            for (var month = 1; month <= 12; month++)
            {
                data.Add(new DateTime(2024, month, 15, 12, 0, 0));
            }

            // around the DST switches and the solstices
            data.Add(new DateTime(2024, 3, 31, 12, 0, 0));
            data.Add(new DateTime(2024, 10, 27, 12, 0, 0));
            data.Add(new DateTime(2024, 6, 21, 12, 0, 0));
            data.Add(new DateTime(2024, 12, 21, 12, 0, 0));
            return data;
        }

        [Theory]
        [MemberData(nameof(Days))]
        public void IsSunRiseAndIsSunSet_AreExclusiveAndCoverTheDay(DateTime day)
        {
            var solar = new SolarTimes(day, Latitude, Longitude);
            for (var t = day.Date; t < day.Date.AddDays(1); t = t.AddMinutes(7))
            {
                if (t == solar.Sunrise)
                {
                    continue;
                }

                var rise = SunDriver.IsSunRise(solar, t);
                var set = SunDriver.IsSunSet(solar, t);
                Assert.True(rise ^ set, $"{t:O}: rise {rise}, set {set} (sunrise {solar.Sunrise:t}, sunset {solar.Sunset:t})");
            }
        }

        [Theory]
        [MemberData(nameof(Days))]
        public void IsDusk_IsTheThirtyMinutesAfterSunset(DateTime day)
        {
            var solar = new SolarTimes(day, Latitude, Longitude);

            Assert.False(SunDriver.IsDusk(solar, solar.Sunset.AddSeconds(-1)));
            Assert.True(SunDriver.IsDusk(solar, solar.Sunset));
            Assert.True(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(30).AddSeconds(-1)));
            Assert.False(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(30)));
            Assert.False(SunDriver.IsDusk(solar, solar.Sunrise));
            Assert.False(SunDriver.IsDusk(solar, day.Date.AddHours(12)));
        }

        [Theory]
        [MemberData(nameof(Days))]
        public void IsDawn_IsTheThirtyMinutesBeforeSunrise(DateTime day)
        {
            var solar = new SolarTimes(day, Latitude, Longitude);

            Assert.False(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(-30).AddSeconds(-1)));
            Assert.True(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(-30)));
            Assert.True(SunDriver.IsDawn(solar, solar.Sunrise.AddSeconds(-1)));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunrise));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunset));
            Assert.False(SunDriver.IsDawn(solar, day.Date.AddHours(12)));
        }

        [Theory]
        [MemberData(nameof(Days))]
        public void DuskAndDawn_HappenWhileTheSunIsDown(DateTime day)
        {
            var solar = new SolarTimes(day, Latitude, Longitude);
            for (var t = day.Date; t < day.Date.AddDays(1); t = t.AddMinutes(3))
            {
                if (SunDriver.IsDusk(solar, t) || SunDriver.IsDawn(solar, t))
                {
                    Assert.True(SunDriver.IsSunSet(solar, t), $"{t:O}");
                    Assert.False(SunDriver.IsDusk(solar, t) && SunDriver.IsDawn(solar, t), $"{t:O} is dusk and dawn");
                }
            }
        }

        [Fact]
        public void TwilightDuration_IsThirtyMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(30), SunDriver.TwilightDuration);
        }
    }

    /// <summary>The timezone offset setting: "now" and the startup time must be shifted consistently.</summary>
    [Collection("times-dispatcher")]
    public class TimezoneOffsetTests : DriverFactoryTestBase<DateTimeDriverFactory>
    {
        private async Task<IDriver> CreateWithOffset(int offset, Guid child)
        {
            var settings = new Mock<INodeTemplateFactory>();
            settings.Setup(s => s.GetSetting("timezoneOffset")).Returns(new Setting { ValueInt = offset });

            var root = CreateNodeInstance(DateTimeDriverFactory.DriverGuidId);
            root.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(child));

            var context = new DriverContextMock(root, DriverFactory, settings.Object, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = DriverFactory.CreateDriver(context);
            await driver.Configure();
            await driver.Start();
            await driver.Started();
            return driver;
        }

        private object DispatchedValue(IDriver driver)
        {
            var id = driver.Children.Single().Id;
            return Dispatcher.GetValue(DispatchableType.NodeInstance, id)?.Value;
        }

        [Theory]
        [InlineData(5)]
        [InlineData(-5)]
        [InlineData(0)]
        public async Task SecondsSinceBoot_DoesNotIncludeTheTimezoneOffset(int offset)
        {
            _ = ServerInfo.StartupTime;
            var driver = await CreateWithOffset(offset, DateTimeDriverFactory.SecondsSinceBoot);

            var value = Assert.IsType<double>(DispatchedValue(driver));

            Assert.InRange(value, 0, 3599);
            await driver.Stop();
        }

        [Theory]
        [InlineData(3)]
        [InlineData(-2)]
        public async Task BootTime_IsShiftedByTheTimezoneOffset(int offset)
        {
            var driver = await CreateWithOffset(offset, DateTimeDriverFactory.BootDateTime);

            Assert.Equal(ServerInfo.StartupTime.AddHours(offset), DispatchedValue(driver));
            await driver.Stop();
        }

        [Fact]
        public async Task Year_IsShiftedByTheTimezoneOffset()
        {
            // 100 years of offset makes the shift visible without depending on the time of day
            var driver = await CreateWithOffset(24 * 366 * 100, DateTimeDriverFactory.Year);

            var year = Assert.IsType<int>(DispatchedValue(driver));
            Assert.InRange(year, DateTime.Now.Year + 99, DateTime.Now.Year + 101);
            await driver.Stop();
        }
    }
}
