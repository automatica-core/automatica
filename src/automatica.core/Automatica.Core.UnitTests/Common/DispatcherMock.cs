using Automatica.Core.Runtime.IO;
using Automatica.Push.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using MQTTnet.Server;

namespace Automatica.Core.UnitTests.Base.Common
{
    public class DispatcherMock : Dispatcher
    {
        public static DispatcherMock Instance { get; } = new DispatcherMock();

        public DispatcherMock() : base(new Mock<IHubContext<DataHub>>().Object, new Mock<IMqttServer>().Object)
        {
        }

    }
}
