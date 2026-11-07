using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Innovative.Geometry;
using Innovative.SolarCalculator;
using P3.Driver.Times.DriverFactory;
using P3.Driver.Times.DriverFactory.Sun;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Driver.Times.Tests
{
    public class SunDriverFactoryTests : DriverFactoryTestBase<SunDriverFactory>
    {
        private const double Latitude = 48.1341337;
        private const double Longitude = 14.1515088;

        private static readonly Angle LatitudeAngle = new Angle(Latitude);
        private static readonly Angle LongitudeAngle = new Angle(Longitude);

        public SunDriverFactoryTests()
        {
            Factory.SetSettings("Longitude", Longitude);
            Factory.SetSettings("Latitude", Latitude);

            //reninit because we set the settings to late
            DriverFactory.InitNodeTemplates(Factory);
        }

        [Fact]
        public async Task TestSunSet()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunSetGuid);

            Assert.Single(driver.Children);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);

            var solarTimes = new SolarTimes(DateTime.Now, LatitudeAngle, LongitudeAngle);

            Assert.Equal(solarTimes.Sunset, busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestSunRise()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunRiseGuid);

            Assert.Single(driver.Children);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);


            var solarTimes = new SolarTimes(DateTime.Now, LatitudeAngle, LongitudeAngle);

            Assert.Equal(solarTimes.Sunrise, busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        private DispatchValue Create(object value)
        {
            return new DispatchValue(DispatchableMock.Instance.Id, DispatchableType.NodeInstance, value, DateTime.Now,
                DispatchValueSource.Read);
        }

        [Fact]
        public async Task TestIsSunSet()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(new DateTime(2018, 08, 28, 14, 00, 00)));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);


            Assert.False((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsSunRise()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsRiseGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);
            Assert.True((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsSunSet2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.True((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsSunSet3()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);
            Assert.False((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsSunSet_Evening()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 23, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.IsType<bool>(busDt);
            Assert.True((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        private static SolarTimes SolarTimesFor(DateTime date)
        {
            return new SolarTimes(date, LatitudeAngle, LongitudeAngle);
        }

        [Fact]
        public void TestIsSunSet_AroundSunset()
        {
            var solar = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00));

            Assert.False(SunDriver.IsSunSet(solar, solar.Sunset.AddMinutes(-1)));
            Assert.True(SunDriver.IsSunSet(solar, solar.Sunset));
            Assert.True(SunDriver.IsSunSet(solar, solar.Sunset.AddMinutes(1)));
            Assert.True(SunDriver.IsSunSet(solar, solar.Sunrise.AddMinutes(-1)));
            Assert.False(SunDriver.IsSunSet(solar, solar.Sunrise.AddMinutes(1)));
        }

        [Fact]
        public void TestIsSunRise_AroundSunset()
        {
            var solar = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00));

            Assert.True(SunDriver.IsSunRise(solar, solar.Sunset.AddMinutes(-1)));
            Assert.False(SunDriver.IsSunRise(solar, solar.Sunset.AddMinutes(1)));
            Assert.False(SunDriver.IsSunRise(solar, solar.Sunrise.AddMinutes(-1)));
            Assert.True(SunDriver.IsSunRise(solar, solar.Sunrise.AddMinutes(1)));
        }

        [Fact]
        public void TestIsDusk()
        {
            var solar = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00));

            Assert.False(SunDriver.IsDusk(solar, new DateTime(2018, 08, 28, 12, 00, 00)));
            Assert.False(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(-1)));
            Assert.True(SunDriver.IsDusk(solar, solar.Sunset));
            Assert.True(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(10)));
            Assert.True(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(29)));
            Assert.False(SunDriver.IsDusk(solar, solar.Sunset.AddMinutes(31)));
            Assert.False(SunDriver.IsDusk(solar, solar.Sunrise.AddMinutes(-10)));
            Assert.False(SunDriver.IsDusk(solar, new DateTime(2018, 08, 28, 23, 30, 00)));
        }

        [Fact]
        public void TestIsDawn()
        {
            var solar = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00));

            Assert.False(SunDriver.IsDawn(solar, new DateTime(2018, 08, 28, 12, 00, 00)));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(-31)));
            Assert.True(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(-29)));
            Assert.True(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(-10)));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunrise));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunrise.AddMinutes(10)));
            Assert.False(SunDriver.IsDawn(solar, solar.Sunset.AddMinutes(10)));
        }

        [Fact]
        public async Task TestDusk_AfterSunset()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunDusk);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00)).Sunset.AddMinutes(10);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.IsType<bool>(busDt);
            Assert.True((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestDawn_Evening()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunDawn);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = SolarTimesFor(new DateTime(2018, 08, 28, 12, 00, 00)).Sunset.AddMinutes(10);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.IsType<bool>(busDt);
            Assert.False((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsSunRise2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsRiseGuid);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.False((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }


        [Fact]
        public async Task TestIsDaylight_True1()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.IsDaylight);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2023, 12, 04, 15, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.True((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsDaylight_True2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.IsDaylight);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2023, 12, 04, 08, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.True((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }


        [Fact]
        public async Task TestIsDaylight_False1()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.IsDaylight);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2023, 12, 04, 18, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.False((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestIsDaylight_False2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.IsDaylight);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2023, 12, 04, 05, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.False((bool)busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAzimuthAndElevation(bool azimuth)
        {
            await Dispatcher.ClearValues();
            var template = azimuth ? SunDriverFactory.SunAzimuthGuid : SunDriverFactory.SunElevationGuid;
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, template);

            Assert.Single(driver.Children);

            var node = driver.Children.First();
            var setTime = new DateTime(2024, 06, 21, 13, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, Create(setTime));

            await Task.Delay(200);
            var value = Dispatcher.GetValues(DispatchableType.NodeInstance).First().Value.Value;
            Assert.IsType<double>(value);

            var solarTimes = new SolarTimes(setTime, LatitudeAngle, LongitudeAngle);
            var expected = azimuth ? SunDriver.Azimuth(solarTimes) : SunDriver.Elevation(solarTimes);
            Assert.Equal(expected, (double)value, 1);

            if (azimuth)
            {
                Assert.InRange((double)value, 0, 360);
            }
            else
            {
                // midsummer around noon in Austria: the sun is high
                Assert.InRange((double)value, -90, 90);
            }

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
    }
}
