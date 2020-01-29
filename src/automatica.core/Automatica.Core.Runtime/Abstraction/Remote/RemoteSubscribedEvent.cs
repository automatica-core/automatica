namespace Automatica.Core.Runtime.Abstraction.Remote
{
    internal class RemoteSubscribedEvent
    {
        public string ClientId { get; }
        public string Topic { get; }

        public RemoteSubscribedEvent(string clientId, string topic)
        {
            ClientId = clientId;
            Topic = topic;
        }
    }
}
