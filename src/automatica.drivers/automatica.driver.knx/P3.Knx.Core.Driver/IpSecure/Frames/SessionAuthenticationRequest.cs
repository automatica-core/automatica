using System;

namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    internal class SessionAuthenticationRequest : SecureFrame
    {
        private SessionAuthenticationRequest(KnxConnectionTunnelingSecure knx)
            : base(knx, KnxHelper.ServiceType.SessionAuthenticate)
        {
            
        }

        internal static SessionAuthenticationRequest Create(KnxConnectionTunnelingSecure knx)
        {
            return new SessionAuthenticationRequest(knx);
        }

        internal override byte[] ToFrame()
        {
            byte[] datagram = new byte[8];

            datagram[0] = 0x06;
            datagram[1] = 0x10;
            datagram[2] = 0x09;
            datagram[3] = 0x53;
            datagram[4] = 0x00;
            datagram[5] = 0x18;

            datagram[6] = 0x00;
            datagram[7] = 0x01; //user


            byte[] xorClientServer = CryptoHelper.Xor(KnxConnectionSecure.ClientPublic, KnxConnectionSecure.ServerPublic);

            byte[] bA = new byte[datagram.Length + xorClientServer.Length];
            datagram.CopyTo(bA, 0);
            xorClientServer.CopyTo(bA, 8);
            
            byte[] b = new byte[16];
            byte[] yn = CryptoHelper.CalculateY(KnxConnectionSecure.HashedPassword, new byte[0], bA, b);
            byte[] ctr = new byte[16];
            ctr[14] = 0xff;

            byte[] calculatedMac = CryptoHelper.CalculateMac(KnxConnectionSecure.HashedPassword, yn, ctr);

            byte[] newDatagram = new byte[datagram.Length + calculatedMac.Length];
            Array.Copy(datagram, 0, newDatagram, 0, datagram.Length);
            Array.Copy(calculatedMac, 0, newDatagram, 8, 16);

            return newDatagram;
        }
    }
}
