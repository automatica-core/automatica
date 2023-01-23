namespace P3.Driver.MBus
{
    public enum Medium
    {
        Other,
        Oil,
        Electricity,
        Gas,
        Heat,
        Steam,
        HotWater,
        Water,
        Hca,
        Reserved1,
        GasMode2,
        HeatMode2,
        HotWaterMode2,
        WaterMode2,
        HcaMode2,
        Reserverd2
    }

    public enum MBusStatus
    {
        Counter1And2CodedSignedBinary = 1,
        Counter1And2AreStoredAtFixedDate = 2,
        PowerLow = 4,
        PermanentError = 8,
        TemporaryError = 16,
        ManufacturerSpecific1 = 32,
        ManufacturerSpecific2 = 64,
        ManufacturerSpecific3 = 128
    }

    public enum MBusFrameType
    {
        SingleChar,
        ShortFrame,
        ControlFrame,
        LongFrame
    }

    public static class MBus
    {
        public const string MBusDriverName = "P3.Driver.MBus";
        public const byte SingleCharFrame = 0xE5;
        public const byte ShortFrameStart = 0x10;
        public const byte ControlFrameLongFrameStart = 0x68;

        public const byte FrameEndByte = 0x16;

        public const byte ControlMaskSndNke = 0x40;
        public const byte ControlMaskSndUd = 0x53;
        public const byte ControlMaskReqUd2 = 0x5B;
        public const byte ControlMaskReqUd1 = 0x5A;
        public const byte ControlMaskRspUd = 0x08;

    }
}
