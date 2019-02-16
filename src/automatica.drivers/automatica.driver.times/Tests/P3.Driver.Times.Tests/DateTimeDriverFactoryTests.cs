using System;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
using P3.Driver.Times.DriverFactory;
using P3.Driver.Times.DriverFactory.DateTime;
using Xunit;

namespace P3.Driver.Times.Tests
{
    public class DateTimeDriverFactoryTests : DriverFactoryTestBase<DateTimeDriverFactory>
    {
        public DateTimeDriver CreateDriver(Guid childNodeGuid)
        {
            return CreateDriver<DateTimeDriver>(DateTimeDriverFactory.DriverGuidId, childNodeGuid);
        }

        [Fact]
        public void TestDateTimeNow()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.DateTime);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt =  values.First().Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);
        

            Assert.Equal(dtNow.Date, busDt.Value.Date);
            Assert.Equal(dtNow.Hour, busDt.Value.Hour);
            Assert.Equal(dtNow.Minute, busDt.Value.Minute);
            Assert.Equal(dtNow.Second, busDt.Value.Second);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public void TestDateNow()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Date);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt =  values.First().Value as DateTime?;

            Assert.NotNull(busDt);
            Assert.IsType<DateTime>(busDt);

            Assert.Equal(dtNow.Date, busDt.Value.Date);
            Assert.Equal(0, busDt.Value.Hour);
            Assert.Equal(0, busDt.Value.Minute);
            Assert.Equal(0, busDt.Value.Second);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestTimeNow()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Time);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var busDt = values.First().Value as TimeSpan?;

            Assert.NotNull(busDt);
            Assert.IsType<TimeSpan>(busDt);
           
            Assert.Equal(dtNow.TimeOfDay.Hours, busDt.Value.Hours);
            Assert.Equal(dtNow.TimeOfDay.Minutes, busDt.Value.Minutes);
            Assert.InRange(busDt.Value.Seconds, dtNow.TimeOfDay.Seconds - 5, dtNow.TimeOfDay.Seconds + 5);


            driver.Stop();
            DispatcherMock.Instance.Clear();
        }


        [Fact]
        public void TestDayOfWeek()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.DayOfWeek);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.DayOfWeek);
            driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public void TestDayOfMonth()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.DayOfMonth);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Day);
            
            driver.Stop();
            DispatcherMock.Instance.Clear();
        }

        [Fact]
        public void TestYear()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Year);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Year);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestMonth()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Month);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Month);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestMinutes()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Minutes);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Minute);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestSeconds()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Seconds);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Second);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestHours()
        {
            DispatcherMock.Instance.Clear();
            var dtNow = DateTime.Now;
            var driver = CreateDriver(DateTimeDriverFactory.Hours);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.Equal(value, dtNow.Hour);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
        [Fact]
        public void TestMilliseconds()
        {
            DispatcherMock.Instance.Clear();
            var driver = CreateDriver(DateTimeDriverFactory.Milliseconds);

            Assert.Equal(1, driver.Children.Count);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(1, values.Count);

            var value =  values.First().Value;

            Assert.NotNull(value);

            driver.Stop();
            DispatcherMock.Instance.Clear();
        }
    }
}
