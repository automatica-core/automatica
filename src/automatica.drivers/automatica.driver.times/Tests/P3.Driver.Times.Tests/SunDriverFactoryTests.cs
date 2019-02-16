using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
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
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunSetGuid);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);

            var solarTimes = new SolarTimes(DateTime.Now, LatitudeAngle, LongitudeAngle);

            Assert.Equal(solarTimes.Sunset, busDt);
            
            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public async Task TestSunRise()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunRiseGuid);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);


            var solarTimes = new SolarTimes(DateTime.Now, LatitudeAngle, LongitudeAngle);

            Assert.Equal(solarTimes.Sunrise, busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public async Task TestIsSunSet()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.FirstOrDefault();
            await node.WriteValue(DispatchableMock.Instance, new DateTime(2018, 08, 28, 14, 00, 00));

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            
            Assert.False((bool)busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public async Task TestIsSunRise()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsRiseGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.FirstOrDefault();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await node.WriteValue(DispatchableMock.Instance, setTime);


            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);


            var solarTimes = new SolarTimes(new DateTime(2018, 08, 28, 14, 00, 00), LatitudeAngle, LongitudeAngle);

            Assert.True((bool)busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public async Task TestIsSunSet2()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.FirstOrDefault();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await node.WriteValue(DispatchableMock.Instance, setTime);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            var solarTimes = new SolarTimes(setTime, LatitudeAngle, LongitudeAngle);

            Assert.True((bool)busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public async Task TestIsSunSet3()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsSetGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.FirstOrDefault();
            var setTime = new DateTime(2018, 08, 28, 15, 00, 00);
            await node.WriteValue(DispatchableMock.Instance, setTime);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);

            var solarTimes = new SolarTimes(setTime, LatitudeAngle, LongitudeAngle);

            Assert.False((bool)busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public async Task TestIsSunRise2()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver<SunDriver>(SunDriverFactory.DriverGuidId, SunDriverFactory.SunIsRiseGuid);

            Assert.Equal(1, driver.Children.Count);

            var node = driver.Children.FirstOrDefault();
            var setTime = new DateTime(2018, 08, 28, 00, 00, 00);
            await node.WriteValue(DispatchableMock.Instance, setTime);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.Count > 0);
            var busDt = values.First().Value;

            Assert.NotNull(busDt);
            Assert.IsType<bool>(busDt);


            var solarTimes = new SolarTimes(setTime, LatitudeAngle, LongitudeAngle);

            Assert.False((bool)busDt);

            await driver.Stop();
            DispatcherMock.Instance.Clear();
        }
    }
}
