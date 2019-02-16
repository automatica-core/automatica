namespace P3.Knx.Core.Driver.Frames
{
    internal class DisconnectResponseFrame : KnxFrame
    {
        internal DisconnectResponseFrame(KnxConnection knx)
            : base(knx, KnxHelper.ServiceType.DisconnectRequest)
        {
            
        }

        internal override byte[] ToFrame()
        {
            byte[] datagram = new byte[8];

            datagram[0] = 0x06;
            datagram[1] = 0x10;
            datagram[2] = 0x02;
            datagram[3] = 0x0A;
            datagram[4] = 0x00;
            datagram[5] = 0x08;
            datagram[6] = KnxConnection.ChannelId;
            datagram[7] = 0x00;


            return datagram;
        }
    }
}
