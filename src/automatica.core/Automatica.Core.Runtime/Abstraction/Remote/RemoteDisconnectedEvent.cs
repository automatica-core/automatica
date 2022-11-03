namespace Automatica.Core.Runtime.Abstraction.Remote
{
    internal class RemoteDisconnectedEvent
    {
        public string ClientId { get; }

        public RemoteDisconnectedEvent(string clientId)
        {
            ClientId = clientId;
        }
    }
}
