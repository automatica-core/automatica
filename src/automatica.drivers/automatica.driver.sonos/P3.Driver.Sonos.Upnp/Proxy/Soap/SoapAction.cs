namespace P3.Driver.Sonos.Upnp.Proxy.Soap
{
    public class SoapAction
    {
        public static readonly string HeaderName = "SOAPAction";

        public string HeaderValue => $"\"{ActionNamespace}#{Action}\"";

        public string Action { get; }

        public string ActionNamespace { get; }

        public SoapAction(string action, string actionNamespace)
        {
            Action = action;
            ActionNamespace = actionNamespace;
        }
    }
}