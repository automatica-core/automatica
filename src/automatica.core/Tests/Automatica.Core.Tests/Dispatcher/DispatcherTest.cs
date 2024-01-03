using Automatica.Core.Base.IO;
using System;
using System.Threading.Tasks;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Common;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{


    public class DispatcherTest
    {
     
        [Fact]
        public async Task WriteDirect()
        {
            var driverMock = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName", DispatcherMock.Instance);
            var driverMock2 = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName2", DispatcherMock.Instance);

            await driverMock.WriteValue(driverMock2, new DispatchValue(Guid.NewGuid(), DispatchableType.NodeInstance, true, DateTime.Now, DispatchValueSource.User));

            await Task.Delay(500);

            Assert.True(driverMock.WriteReceived);
        }

        [Fact]
        public async Task WriteDispatcher()
        {
            var driverMock = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName", DispatcherMock.Instance);
            var driverMock2 = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName2", DispatcherMock.Instance);

            await DispatcherMock.Instance.RegisterDispatch(DispatchableType.NodeInstance, driverMock.Id, (a, b) =>
            {
                ((DriverNodeMock)driverMock2.Children[0]).WriteValue(a, b);
            });

            await DispatcherMock.Instance.DispatchValue(driverMock, true, DispatchValueSource.Read);
            await Task.Delay(200);

            Assert.True(((DriverNodeMock)driverMock2.Children[0]).WriteReceived);
        }

        [Fact]
        public async Task TryGetValue()
        {
            var driverMock = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName", DispatcherMock.Instance);
            await DispatcherMock.Instance.DispatchValue(driverMock, true, DispatchValueSource.Read);

            var value = DispatcherMock.Instance.GetValue(DispatchableType.NodeInstance, driverMock.Id);

            Assert.IsType<bool>(value.Value);
            Assert.True((bool)value.Value);
        }


        [Fact]
        public async Task TryGetValues()
        {
            var driverMock = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName", DispatcherMock.Instance);
            await DispatcherMock.Instance.DispatchValue(driverMock, true, DispatchValueSource.Read);

            var values = DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance);

            Assert.True(values.ContainsKey(driverMock.Id));
        }

        [Fact]
        public async Task TestDispatcherClear()
        {
            var driverMock2 = await DispatcherHelperUtils.CreateNodeMock(Guid.NewGuid(), "MockName2", DispatcherMock.Instance);

            await DispatcherMock.Instance.RegisterDispatch(DispatchableType.NodeInstance, DispatchableMock.Instance.Id, (a, b) =>
            {
                ((DriverNodeMock)driverMock2.Children[0]).WriteValue(a, b);
            });
            await DispatcherMock.Instance.ClearRegistrations();
            await DispatcherMock.Instance.DispatchValue(DispatchableMock.Instance, true, DispatchValueSource.Read);



            Assert.False(((DriverNodeMock)driverMock2.Children[0]).WriteReceived);
        }
    }
}
