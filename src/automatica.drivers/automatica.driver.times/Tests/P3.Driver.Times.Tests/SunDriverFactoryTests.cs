﻿using System;
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
    }
}
