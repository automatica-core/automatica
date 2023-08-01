using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Times.DriverFactory;
using P3.Driver.Times.DriverFactory.DateTime;
using Xunit;

namespace P3.Driver.Times.Tests
{
    public class DateTimeDriverFactoryTests : DriverFactoryTestBase<DateTimeDriverFactory>
    {
        public Task<DateTimeDriver> CreateDriver(Guid childNodeGuid)
        {
            return CreateDriver<DateTimeDriver>(DateTimeDriverFactory.DriverGuidId, childNodeGuid);
        }

        [Fact]
        public async Task TestDateTimeNow()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.DateTime);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt =  values.First().Value.Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);
        

            Assert.Equal(dtNow.Date, busDt.Value.Date);
            Assert.Equal(dtNow.Hour, busDt.Value.Hour);
            Assert.Equal(dtNow.Minute, busDt.Value.Minute);
            Assert.Equal(dtNow.Second, busDt.Value.Second);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestDateNow()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Date);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt =  values.First().Value.Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);

            Assert.Equal(dtNow.Date, busDt.Value.Date);
            Assert.Equal(0, busDt.Value.Hour);
            Assert.Equal(0, busDt.Value.Minute);
            Assert.Equal(0, busDt.Value.Second);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestTimeNow()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Time);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt = values.First().Value.Value as TimeSpan?;

            Assert.NotNull(busDt);
            Assert.IsType<TimeSpan>(busDt);
           
            Assert.Equal(dtNow.TimeOfDay.Hours, busDt.Value.Hours);
            Assert.Equal(dtNow.TimeOfDay.Minutes, busDt.Value.Minutes);
            Assert.InRange(busDt.Value.Seconds, dtNow.TimeOfDay.Seconds - 5, dtNow.TimeOfDay.Seconds + 5);


            await driver.Stop();
            await Dispatcher.ClearValues();
        }


        [Fact]
        public async Task TestDayOfWeek()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.DayOfWeek);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.DayOfWeek);
            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestDayOfMonth()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.DayOfMonth);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Day);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }

        [Fact]
        public async Task TestYear()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Year);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Year);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestMonth()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Month);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Month);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestMinutes()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Minutes);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Minute);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestSeconds()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Seconds);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Second);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestHours()
        {
            await Dispatcher.ClearValues();
            var dtNow = DateTime.Now;
            var driver = await CreateDriver(DateTimeDriverFactory.Hours);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.Equal(value, dtNow.Hour);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
        [Fact]
        public async Task TestMilliseconds()
        {
            await Dispatcher.ClearValues();
            var driver = await CreateDriver(DateTimeDriverFactory.Milliseconds);

            Assert.Equal(1, driver.Children.Count);

            var values = Dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value.Value;

            Assert.NotNull(value);

            await driver.Stop();
            await Dispatcher.ClearValues();
        }
    }
}
