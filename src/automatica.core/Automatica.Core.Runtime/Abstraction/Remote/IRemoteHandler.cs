using System;
using System.Threading.Tasks;
using Automatica.Core.Base.Remote;

namespace Automatica.Core.Runtime.Abstraction.Remote
{
    internal interface IRemoteHandler
    {
        Task ClientConnected(RemoteConnectedEvent connectedEvent);
        Task ClientDisconnected(RemoteDisconnectedEvent connectedEvent);
        Task ClientSubscribedTopic(RemoteSubscribedEvent subscribedEvent);

        Task MessageReceived(RemoteMessageEvent messageEvent);

        Task SendAction(Guid client, DriverNodeRemoteAction action, object data);
    }
}
