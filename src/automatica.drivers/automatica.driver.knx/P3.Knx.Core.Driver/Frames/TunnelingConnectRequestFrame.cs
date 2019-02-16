

namespace P3.Knx.Core.Driver.Frames
{
    internal class TunnelingConnectRequestFrame : KnxFrame
    {
        internal TunnelingConnectRequestFrame(KnxConnection knx)
            : base(knx, KnxHelper.ServiceType.ConnectRequest)
        {
            
        }

        internal override byte[] ToFrame()
        {
            var datagram = new byte[26];
            datagram[00] = 0x06;
            datagram[01] = 0x10;

            datagram[02] = 0x02; //Service Type
            datagram[03] = 0x05;

            datagram[04] = 0x00;
            datagram[05] = 0x1A;

            //if (KnxConnection.GetType() == typeof(KnxConnectionTunnelingSecure))
            //{
            //    datagram[06] = 0x08;
            //    datagram[07] = 0x02;
            //    datagram[08] = 0x00;
            //    datagram[09] = 0x00;
            //    datagram[10] = 0x00;
            //    datagram[11] = 0x00; 
            //    datagram[12] = 0x00; 
            //    datagram[13] = 0x00; 

            //    datagram[14] = 0x08;
            //    datagram[15] = 0x02;
            //    datagram[16] = 0x00; 
            //    datagram[17] = 0x00; 
            //    datagram[18] = 0x00;
            //    datagram[19] = 0x00;
            //    datagram[20] = 0x00;
            //    datagram[21] = 0x00;
            //}
            //else
            //{
                if (KnxConnection.UseNat)
                {
                    datagram[06] = 0x08;
                    datagram[07] = 0x01;
                    datagram[08] = 0x00;
                    datagram[09] = 0x00;
                    datagram[10] = 0x00;
                    datagram[11] = 0x00;
                    datagram[12] = 0x00;
                    datagram[13] = 0x00;

                    datagram[14] = 0x08;
                    datagram[15] = 0x01;
                    datagram[16] = 0x00;
                    datagram[17] = 0x00;
                    datagram[18] = 0x00;
                    datagram[19] = 0x00;
                    datagram[20] = 0x00;
                    datagram[21] = 0x00;
                }
                else
                {
                    datagram[06] = 0x08;
                    datagram[07] = 0x01;
                    datagram[08] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[0];
                    datagram[09] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[1];
                    datagram[10] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[2];
                    datagram[11] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[3];
                    datagram[12] = (byte)(KnxConnection.LocalEndpoint.Port >> 8);
                    datagram[13] = (byte)KnxConnection.LocalEndpoint.Port;

                    datagram[14] = 0x08;
                    datagram[15] = 0x01;
                    datagram[16] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[0];
                    datagram[17] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[1];
                    datagram[18] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[2];
                    datagram[19] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[3];
                    datagram[20] = (byte)(KnxConnection.LocalEndpoint.Port >> 8);
                    datagram[21] = (byte)KnxConnection.LocalEndpoint.Port;
                }
           // }
            
            datagram[22] = 0x04;
            datagram[23] = 0x04;
            datagram[24] = 0x02;
            datagram[25] = 0x00;

            return datagram;
        }
    }
}
