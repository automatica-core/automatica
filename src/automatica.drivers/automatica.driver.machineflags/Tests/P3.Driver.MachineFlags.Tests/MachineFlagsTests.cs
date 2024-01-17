using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Xunit;

namespace P3.Driver.MachineFlags.Tests
{
    public class MachineFlagsTests : DriverFactoryTestBase<MachineFlagsDriverFactory>
    {
        private DispatchValue Create(Guid id, object value)
        {
            return new DispatchValue(id, DispatchableType.NodeInstance, value, DateTime.Now,
                DispatchValueSource.Read);
        }

        [Fact]
        public async void Test_MachineFlagsValue()
        {
            var driver = await CreateDriver<MachineFlagsDriver>(MachineFlagsDriverFactory.BusId, MachineFlagsDriverFactory.ValueId);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.MachineFlags>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.MachineFlags;

            
            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, Create(con.Id, 100));
            await Task.Delay(100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(100, value.Value);

        }

        [Fact]
        public async void Test_DoubleFlagsValue()
        {
            var driver = await CreateDriver<MachineFlagsDriver>(MachineFlagsDriverFactory.BusId, MachineFlagsDriverFactory.NumberId);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.DoubleBinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.DoubleBinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, Create(con.Id, 100));
            await Task.Delay(200);


            var value = Dispatcher.GetValue(DispatchableType.NodeInstance, con.Id);

            Assert.Equal(100d, value.Value);

        }


        [Fact]
        public async void Test_StringFlagsValue()
        {
            var driver = await CreateDriver<MachineFlagsDriver>(MachineFlagsDriverFactory.BusId, MachineFlagsDriverFactory.StringId);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.StringBinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.StringBinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, Create(con.Id, 100));
            await Task.Delay(100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal("100", value.Value);

        }

        [Fact]
        public async void Test_BinaryFlagsValue()
        {
            var driver = await CreateDriver<MachineFlagsDriver>(MachineFlagsDriverFactory.BusId, MachineFlagsDriverFactory.BinaryId);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.BinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.BinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, Create(con.Id, 100));

            await Task.Delay(100);
            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(true, value.Value);



            await con.WriteValue(DispatchableMock.Instance, Create(con.Id, 0));

            await Task.Delay(100);

            value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(false, value.Value);
        }
    }
}
