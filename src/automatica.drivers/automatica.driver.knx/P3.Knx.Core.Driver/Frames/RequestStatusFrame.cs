namespace P3.Knx.Core.Driver.Frames
{
    internal class RequestStatusFrame : KnxFrame
    {
        public string DestinationAddress { get;  }
        private RequestStatusFrame(KnxConnection knx, string dest)
            : base(knx, KnxHelper.ServiceType.TunnellingRequest)
        {
            DestinationAddress = dest;
        }

        internal static RequestStatusFrame CreateFrame(KnxConnection knx, string ia)
        {
            return new RequestStatusFrame(knx, ia);
        }

        internal override byte[] ToFrame()
        {
            int cemi_start_pos = 10;
            var datagram = new byte[21];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x04;
            datagram[03] = 0x20;
            datagram[04] = 0x00;
            datagram[05] = 0x15;

            datagram[06] = 0x04;
            datagram[07] = KnxConnection.ChannelId;
            datagram[08] = KnxConnection.GenerateSequenceNumber();
            datagram[09] = 0x00;

            var i = 0;

            datagram[cemi_start_pos + i++] =
                KnxConnection.ActionMessageCode != 0x00
                    ? KnxConnection.ActionMessageCode
                    : (byte)0x11;

            datagram[cemi_start_pos + i++] = 0x00;
            datagram[cemi_start_pos + i++] = 0xAC;

            datagram[cemi_start_pos + i++] =
                KnxHelper.IsAddressIndividual(DestinationAddress)
                    ? (byte)0x50
                    : (byte)0xF0;

            datagram[cemi_start_pos + i++] = 0x00;
            datagram[cemi_start_pos + i++] = 0x00;
            byte[] dstAddress = KnxHelper.GetAddress(DestinationAddress);
            datagram[cemi_start_pos + i++] = dstAddress[0];
            datagram[cemi_start_pos + i++] = dstAddress[1];

            datagram[cemi_start_pos + i++] = 0x01;
            datagram[cemi_start_pos + i++] = 0x00;
            datagram[cemi_start_pos + i] = 0x00;

            return datagram;

        }
    }
}
