namespace P3.Driver.EBus.Config
{
    public abstract class IpConfig : IEBusIpConfig
    {
        protected IpConfig(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }

        public string Ip { get; }
        public int Port { get; }
        public abstract InterfaceType InterfaceType { get; }
    }
}
