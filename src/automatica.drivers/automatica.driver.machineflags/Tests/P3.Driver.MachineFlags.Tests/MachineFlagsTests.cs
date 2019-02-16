using System;
using System.Linq;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
using Xunit;

namespace P3.Driver.MachineFlags.Tests
{
    public class MachineFlagsTests : DriverFactoryTestBase<MachineFlagsDriverFactory>
    {
        [Fact]
        public void Test_MachineFlagsValue()
        {
            var constantsRoot = CreateNodeInstance(MachineFlagsDriverFactory.BusId);
            var valueId = CreateNodeInstance(MachineFlagsDriverFactory.ValueId);

            
            constantsRoot.InverseThis2ParentNodeInstanceNavigation.Add(valueId);

            var driver = CreateDriver(constantsRoot);

            Assert.True(driver.Children.Count == 1);
            Assert.IsType<MachineFlags>(driver.Children[0]);

            var con = driver.Children[0] as MachineFlags;

            
            Assert.NotNull(con);
            con.WriteValue(DispatchableMock.Instance, 100);


            var value =DispatcherMock.Instance.GetValue(Automatica.Core.Base.IO.DispatchableType.NodeInstance, con.Id);

            Assert.Equal(100, value);

        }
    }
}
