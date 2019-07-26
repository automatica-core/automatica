namespace Automatica.Core.Runtime.Abstraction.Remote
{
    internal class RemoteConnectedEvent
    {
        public string ClientId { get; }

        public RemoteConnectedEvent(string clientId)
        {
            ClientId = clientId;
        }
    }
}
