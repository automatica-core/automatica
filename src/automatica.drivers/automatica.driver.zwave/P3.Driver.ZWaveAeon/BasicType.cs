namespace P3.Driver.ZWaveAeon
{
    public enum BasicType : byte
    {
        Unknown = 0x00,
        Controller = 0x01,
        StaticController = 0x02,
        Slave = 0x03,
        RoutingSlave = 0x04
    }
}
