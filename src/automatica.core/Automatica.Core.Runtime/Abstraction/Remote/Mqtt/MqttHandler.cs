using System;
using System.Threading.Tasks;
using Automatica.Core.Base.Remote;

namespace Automatica.Core.Runtime.Abstraction.Remote.Mqtt
{
    internal class MqttHandler : IRemoteHandler
    {
        public Task ClientConnected(RemoteConnectedEvent connectedEvent)
        {
            throw new NotImplementedException();
        }

        public Task ClientSubscribedTopic(RemoteSubscribedEvent subscribedEvent)
        {
            throw new NotImplementedException();
        }

        public Task MessageReceived(RemoteMessageEvent messageEvent)
        {
            throw new NotImplementedException();
        }

        public Task SendAction(Guid client, DriverNodeRemoteAction action, object data)
        {
            throw new NotImplementedException();
        }
    }
}
