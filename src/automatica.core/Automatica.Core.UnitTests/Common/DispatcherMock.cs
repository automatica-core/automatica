using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Automatica.Core.UnitTests.Base.Common
{
    public class DispatcherMock : Dispatcher
    {
        public static DispatcherMock Instance { get; } = new DispatcherMock();

        public DispatcherMock() : base(NullLogger<Dispatcher>.Instance, new Mock<IDataBroadcast>().Object, new Mock<IRemoteSender>().Object)
        {
        }

    }
}
