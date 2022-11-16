using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.PingFactory;
using Xunit;

namespace P3.Driver.PingTests
{
    public class PingTests : DriverFactoryTestBase<PingFactory.PingFactory>
    {
        [Fact]
        public async Task Test_PingOk()
        {
            await Test_Ping("127.0.0.1", 0, true, 0, 1500);
        }

        [Fact]
        public async Task Test_PingFail()
        {
            await Test_Ping("remote-host", 0, false, 0,  1500);
        }

        [Fact]
        public async Task Test_PingSuccess2()
        {
            await Test_Ping("127.0.0.1", 1, true, 1, 3500);
        }
        [Fact]
        public async Task Test_PingFail2()
        {
            await Test_Ping("127.0.0.1", 1, false, 5, 1000);
        }

        private async Task Test_Ping(string device, int intervalValue, bool result, int minSuccessCount, int delay)
        {

            var root = CreateNodeInstance(PingFactory.PingFactory.DriverId);
            var pingDevice = CreateNodeInstance(PingFactory.PingFactory.PingDevice);

            var ip = pingDevice.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplateNavigation.Key == "ip");
            Assert.NotNull(ip);
            ip.Value = device;

            var interval = pingDevice.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplateNavigation.Key == "interval");
            Assert.NotNull(interval);
            interval.Value = intervalValue;

            var minSuccess = pingDevice.PropertyInstance.SingleOrDefault(a => a.This2PropertyTemplateNavigation.Key == "min_success");
            Assert.NotNull(minSuccess);
            minSuccess.Value = minSuccessCount;

            root.InverseThis2ParentNodeInstanceNavigation.Add(pingDevice);

            var driver = CreateDriver(root);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<PingDevice>(driver.Children[0]);


            var con = driver.Children[0] as PingDevice;
            Assert.NotNull(con);
            await con.Start();


            await Task.Delay(delay);

            Assert.Equal(result, con.Value);

        }
    }
}
