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
        private const double Longitude = 48.1341337;
        private const double Latitude = 14.1515088;

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

            Assert.Equal(1, driver.Children.Count);

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

            Assert.Equal(1, driver.Children.Count);

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

        [Fact]
        public async Task TestIsSunSet()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.First();
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, new DateTime(2018, 08, 28, 14, 00, 00));

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

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, setTime);


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

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, setTime);

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

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, setTime);

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
        public async Task TestIsSunRise2()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsRiseGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.First();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await ((DriverBase)node).WriteValue(DispatchableMock.Instance, setTime);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value.Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            Assert.False((bool) busDt);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
    }
}
