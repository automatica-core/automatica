using P3.Knx.Core.Driver.Tunneling;

namespace P3.Knx.Core.Driver.Frames
{
    internal class DisconnectRequestFrame : KnxFrame
    {
        public bool Disconnect { get; private set; }
        internal DisconnectRequestFrame(KnxConnection knx)
            : base(knx, KnxHelper.ServiceType.DisconnectRequest)
        {
            
        }

        internal static DisconnectRequestFrame Parse(KnxConnection knx, byte[] data)
        {
            DisconnectRequestFrame frame = new DisconnectRequestFrame(knx);

            if (data.Length >= 6 && data[6] == knx.ChannelId)
            {
                frame.Disconnect = true;
            }

            return frame;
        }

        internal override byte[] ToFrame()
        {
            var datagram = new byte[16];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x02;
            datagram[03] = 0x09;
            datagram[04] = 0x00;
            datagram[05] = 0x10;

           
            datagram[06] = KnxConnection.ChannelId;
            datagram[07] = 0x00;

            if (KnxConnection.GetType() == typeof(KnxConnectionTunneling))
            {
                datagram[08] = 0x08;
                datagram[09] = 0x01;
                datagram[10] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[0];
                datagram[11] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[1];
                datagram[12] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[2];
                datagram[13] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[3];
                datagram[14] = (byte) (KnxConnection.LocalEndpoint.Port >> 8);
                datagram[15] = (byte) KnxConnection.LocalEndpoint.Port;
            }
            else
            {
                datagram[08] = 0x08;
                datagram[09] = 0x02;
                datagram[10] = 0x00;
                datagram[11] = 0x00; 
                datagram[12] = 0x00; 
                datagram[13] = 0x00; 
                datagram[14] = 0x00; 
                datagram[15] = 0x00; 
            }
            return datagram;

        }
    }
}
