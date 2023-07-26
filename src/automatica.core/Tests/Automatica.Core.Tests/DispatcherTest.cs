using System.Threading;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.UnitTests.Base.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Automatica.Core.Tests
{
    public class DispatcherTest
    {
        private readonly IDispatcher _dispatcher;
        public DispatcherTest()
        {
            _dispatcher = new Base.IO.Dispatcher(NullLogger<Base.IO.Dispatcher>.Instance, new Mock<IDataBroadcast>().Object, new Mock<IRemoteSender>().Object, new Mock<IRemanentHandler>().Object);
        }

        [Fact]
        public void TestDispatch1()
        {
            var autoResetEvent = new AutoResetEvent(false);

            _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, DispatchableMock.Instance.Id, (dispatchable, o) =>
            {
                Assert.NotNull(o);
                Assert.Equal(100, o.Value);
                autoResetEvent.Set();
            });

            _dispatcher.DispatchValue(DispatchableMock.Instance, 100);

            Assert.True(autoResetEvent.WaitOne(5000));
        }
    }
}
