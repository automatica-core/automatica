namespace P3.Driver.EBus.Config
{
    public class TcpConfig : IpConfig
    {
        public TcpConfig(string ip, int port) : base(ip, port)
        {
        }

        public override InterfaceType InterfaceType => InterfaceType.Tcp;
    }
}
