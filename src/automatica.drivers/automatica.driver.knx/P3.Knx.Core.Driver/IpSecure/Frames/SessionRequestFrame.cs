using System;
using System.Net;
using System.Security.Cryptography;
using P3.Elliptic;

namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    internal class SessionRequestFrame : SecureFrame
    {
        private SessionRequestFrame(KnxConnectionTunnelingSecure knx)
            : base(knx, KnxHelper.ServiceType.SessionRequest)
        {
            
        }

        internal static SessionRequestFrame Create(KnxConnectionTunnelingSecure knx)
        {
            SessionRequestFrame frame = new SessionRequestFrame(knx);
          
            return frame;
        }

        internal override byte[] ToFrame()
        {
            var datagram = new byte[0x2e];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x09;
            datagram[03] = 0x51;
            datagram[04] = 0x00;
            datagram[05] = 0x2e;

            if (KnxConnection.GetType() == typeof(KnxConnectionTunnelingSecure))
            {
                datagram[06] = 0x08;
                datagram[07] = 0x02;
                datagram[08] = 0x00;
                datagram[09] = 0x00;
                datagram[10] = 0x00;
                datagram[11] = 0x00; 
                datagram[12] = 0x00;
                datagram[13] = 0x00;
            }
            else
            {
                datagram[06] = 0x08;
                datagram[07] = 0x02;
                datagram[08] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[0];
                datagram[09] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[1];
                datagram[10] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[2];
                datagram[11] = KnxConnection.LocalEndpoint.Address.GetAddressBytes()[3];
                datagram[12] = (byte)(KnxConnection.LocalEndpoint.Port >> 8);
                datagram[13] = (byte)KnxConnection.LocalEndpoint.Port;

            }
            byte[] aliceRandomBytes = new byte[32];
            RandomNumberGenerator.Create().GetBytes(aliceRandomBytes);

            byte[] alicePrivate = Curve25519.ClampPrivateKey(aliceRandomBytes);
            byte[] alicePublic = Curve25519.GetPublicKey(alicePrivate);

            KnxConnectionSecure.SetPublicAndPrivateKey(alicePublic, alicePrivate);

            alicePublic.CopyTo(datagram, 14);

            return datagram;
        }

        internal static SessionRequestFrame Parse(KnxConnectionTunnelingSecure knx, byte[] data)
        {
            byte[] ipArray = new byte[4];
            Array.Copy(data, 8, ipArray, 0, 4);

            IPAddress a = new IPAddress(ipArray);

            byte[] port = new byte[2];
            Array.Copy(data, 20, port, 0, 2);
            int portInt = (port[0] << 8) + port[1];

           ((KnxSenderTunnelingSecure) knx.Sender).SetRemoteEndpoint(new IPEndPoint(a, portInt));

            byte[] aliceRandomBytes = new byte[32];
            RandomNumberGenerator.Create().GetBytes(aliceRandomBytes);

            byte[] alicePrivate = Curve25519.ClampPrivateKey(aliceRandomBytes);
            byte[] alicePublic = Curve25519.GetPublicKey(alicePrivate);

            knx.SetPublicAndPrivateKey(alicePublic, alicePrivate);

            byte[] serverPublic = new byte[32];
            Array.Copy(data, 6, serverPublic, 0, 32);
            knx.SetServerPublicKey(serverPublic);

            return new SessionRequestFrame(knx);
        }

    }
}
