namespace Automatica.Core.Runtime.Abstraction.Remote
{
    internal class RemoteMessageEvent
    {
        public string ClientId { get; }
        public string Topic { get; }
        public string Message { get; }

        public RemoteMessageEvent(string clientId, string topic, string message)
        {
            ClientId = clientId;
            Topic = topic;
            Message = message;
        }
    }
}
