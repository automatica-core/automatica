using System;
using System.Linq;

namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    internal class SessionResponseFrame : SecureFrame
    {
        private bool mValid__;
        private readonly SessionRequestFrame mRequestFrame__;
        private SessionResponseFrame(KnxConnectionTunnelingSecure knx, SessionRequestFrame frame)
            : base(knx, KnxHelper.ServiceType.SessionResponse)
        {
            mValid__ = false;
            mRequestFrame__ = frame;
        }

        internal static SessionResponseFrame Parse(KnxConnectionTunnelingSecure knx, byte[] data)
        {
            SessionResponseFrame frame = new SessionResponseFrame(knx, knx.SessionRequestFrame);
            frame.Parse(data);
            return frame;
        }

        internal static SessionResponseFrame Create(KnxConnectionTunnelingSecure knx, SessionRequestFrame request)
        {
            return new SessionResponseFrame(knx, request);
        }

        internal bool IsValid()
        {
            return mValid__;
        }

        private void Parse(byte[] datagram)
        {
            TimeSpan diff = CreationDateTime - mRequestFrame__.CreationDateTime; //Check if package is still valid

            if (diff.TotalSeconds > 10)
                return;

            if (datagram.Length != 0x38)
            {
                return;
            }

            byte[] secureSessionId = new byte[2];
            Array.Copy(datagram, 6, secureSessionId, 0, 2);
            KnxConnectionSecure.SetSessionId(secureSessionId);

            byte[] serverPublicKey = new byte[32];
            Array.Copy(datagram, 8, serverPublicKey, 0, 32);
            
            byte[] messageAuthCode = new byte[16];
            Array.Copy(datagram, 40, messageAuthCode, 0, 16);

            KnxConnectionSecure.SetServerPublicKey(serverPublicKey);

            if (KnxConnectionSecure.HashedAuthCode != null)
            {
                byte[] expectedMac = ExpectedMac();

                mValid__ = expectedMac.SequenceEqual(messageAuthCode);
            }
            else
            {
                mValid__ = true;
            }
        }

        internal byte[] ExpectedMac()
        {
            byte[] frame = ToFrame();

            byte[] b = new byte[16];
            byte[] publicYValue = new byte[32];
            Array.Copy(frame, 8, publicYValue, 0, 32);
            Array.Copy(CryptoHelper.Xor(publicYValue, KnxConnectionSecure.ClientPublic), 0, frame, 8, 32);

            byte[] yn = CryptoHelper.CalculateY(KnxConnectionSecure.HashedAuthCode, new byte[0], frame, b);
            byte[] ctr = new byte[16];
            ctr[14] = 0xff;
            byte[] calculatedMac = CryptoHelper.CalculateMac(KnxConnectionSecure.HashedAuthCode, yn, ctr);

            return calculatedMac;
        }


        internal override byte[] ToFrame()
        {
            var datagram = new byte[40];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x09;
            datagram[03] = 0x52;
            datagram[04] = 0x00;
            datagram[05] = 0x38;

            Array.Copy(KnxConnectionSecure.SessionId, 0, datagram, 6, 2);

            KnxConnectionSecure.ServerPublic.CopyTo(datagram, 8);

            return datagram;
        }


    }
}
