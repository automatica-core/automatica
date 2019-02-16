namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    enum MessageType : byte
    {
        Request = 0x00,
        Response = 0x01,
        GetVersion = 0x15,
        MemoryGetId = 0x20,
        ClockSet = 0x30
    }
}
