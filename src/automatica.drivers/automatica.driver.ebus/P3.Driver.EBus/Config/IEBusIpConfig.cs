namespace P3.Driver.EBus.Config
{
    public interface IEBusIpConfig : IEBusConfig
    {
        string Ip { get; }
        int Port { get; }
    }
}
