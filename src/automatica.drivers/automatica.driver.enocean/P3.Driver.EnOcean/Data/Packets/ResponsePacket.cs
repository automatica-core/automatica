using System;

namespace P3.Driver.EnOcean.Data.Packets
{
    public enum ReturnCode
    {
        RetOk = 0,
        RetError = 1,
        RetNotSupported = 2,
        RetWrongParam = 3,
        RetOperationDenied = 4,
        RetLockSet = 5,
        RetBufferToSmall = 6,
        RetNoFreeBuffer = 7
    }
    public class ResponsePacket : EnOceanTelegram
    {
        public ReturnCode ReturnCode { get; private set; }
        public override void FromPacket(EnOceanPacket packet)
        {
            ReturnCode = (ReturnCode)packet.Data.Span[0];
        }

        public override EnOceanPacket ToPacket()
        {
            throw new NotImplementedException();
        }
    }
}
