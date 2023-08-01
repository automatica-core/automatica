using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Xunit;

namespace P3.Driver.MachineFlags.Tests
{
    public class MachineFlagsTests : DriverFactoryTestBase<MachineFlagsDriverFactory>
    {
        [Fact]
        public async void Test_MachineFlagsValue()
        {
            var constantsRoot = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            var valueId = CreateNodeInstance(MachineFlagsDriverFactory.ValueId);

            
            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = await CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.MachineFlags>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.MachineFlags;

            
            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, 100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(100, value.Value);

        }

        [Fact]
        public async void Test_DoubleFlagsValue()
        {
            var constantsRoot = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            var valueId = CreateNodeInstance(MachineFlagsDriverFactory.NumberId);


            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = await CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.DoubleBinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.DoubleBinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, 100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(100d, value.Value);

        }


        [Fact]
        public async void Test_StringFlagsValue()
        {
            var constantsRoot = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            var valueId = CreateNodeInstance(MachineFlagsDriverFactory.StringId);


            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = await CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.StringBinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.StringBinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, 100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal("100", value.Value);

        }

        [Fact]
        public async void Test_BinaryFlagsValue()
        {
            var constantsRoot = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            var valueId = CreateNodeInstance(MachineFlagsDriverFactory.BinaryId);


            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = await CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<Attributes.BinaryFlag>(driver.Children[0]);

            var con = driver.Children[0] as Attributes.BinaryFlag;


            Assert.NotNull(con);
            await con.WriteValue(DispatchableMock.Instance, 100);


            var value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(true, value.Value);



            await con.WriteValue(DispatchableMock.Instance, 0);


            value = Dispatcher.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(false, value.Value);
        }
    }
}
