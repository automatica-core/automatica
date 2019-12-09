namespace P3.Driver.EBus.Config
{
    public enum InterfaceType
    {
        Tcp = 0,
        Udp = 1,
        Serial = 2
    }

    public interface IEBusConfig
    {
        InterfaceType InterfaceType { get; }
    }
}
