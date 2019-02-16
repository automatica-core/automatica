using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.UnitTests.Common;
using Automatica.Core.UnitTests.Drivers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{

    public class DriverNodeMock : DriverBase
    {
        public bool WriteReceived { get; set; }
        public DriverNodeMock(IDriverContext driverContext) : base(driverContext)
        {
        }


        public override Task WriteValue(IDispatchable source, object value)
        {
            WriteReceived = true;
            return base.WriteValue(source, value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new DriverNodeMock(ctx);
        }
    }

    public class DispatcherTest
    {
        private async Task<DriverNodeMock> CreateNodeMock(Guid guid, string name)
        {
            var mockNode = new EF.Models.NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = name+"Parent"
            };
            var mockNodeChild = new EF.Models.NodeInstance
            {
                ObjId = guid,
                Name = name
            };

            mockNode.InverseThis2ParentNodeInstanceNavigation.Add(mockNodeChild);
            var mock = new DriverNodeMock(new DriverContextMock(mockNode, new NodeTemplateFactoryMock()));

            mock.Configure();
            await mock.Start();

            return mock;
        }

        [Fact]
        public async Task WriteDirect()
        {
            var driverMock = await CreateNodeMock(Guid.NewGuid(), "MockName");
            var driverMock2 = await CreateNodeMock(Guid.NewGuid(), "MockName2");

            await driverMock.WriteValue(driverMock2, true);

            Assert.True(driverMock.WriteReceived);
        }

        [Fact]
        public async Task WriteDispatcher()
        {
            var driverMock = await CreateNodeMock(Guid.NewGuid(), "MockName");
            var driverMock2 = await CreateNodeMock(Guid.NewGuid(), "MockName2");

            DispatcherMock.Instance.RegisterDispatch(DispatchableType.NodeInstance, DispatchableMock.Instance.Id, (a, b) =>
            {
                ((DriverNodeMock)driverMock2.Children[0]).WriteValue(a, b);
            });

            await DispatcherMock.Instance.DispatchValue(DispatchableMock.Instance, true);

            Assert.True(((DriverNodeMock)driverMock2.Children[0]).WriteReceived);
        }
    }
}
