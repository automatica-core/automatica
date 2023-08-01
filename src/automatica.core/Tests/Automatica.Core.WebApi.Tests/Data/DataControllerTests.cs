using System;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Data
{
    public class DataControllerTests : BaseControllerTest<DataController>
    {
        [Fact]
        public void TestGetCurrentValues()
        {
            var dispatchAble = new DispatchableMock();
            var dispatcher = ServiceProvider.GetRequiredService<IDispatcher>();
            dispatcher.DispatchValue(dispatchAble, false);

            var currentNodeValues = Controller.GetCurrentNodeValues();

            Assert.Contains(dispatchAble.Id, currentNodeValues);
            Assert.Equal(false, currentNodeValues[dispatchAble.Id].Value);
        }

        [Fact]
        public void TestGetNodeValues()
        {
            var dispatchAble = new DispatchableMock(Guid.NewGuid());
            var dispatcher = ServiceProvider.GetRequiredService<IDispatcher>();
            dispatcher.DispatchValue(dispatchAble, true);

            var nodeValue = Controller.GetNodeValue(dispatchAble.Id);

            Assert.NotNull(nodeValue);
            Assert.Equal(true, nodeValue.Value);
        }
        [Fact]
        public void TestGetNodeValueIsNull()
        {
            var dispatchAble = new DispatchableMock(Guid.NewGuid());
            var nodeValue = Controller.GetNodeValue(dispatchAble.Id);

            Assert.Null(nodeValue);
        }
    }
}
