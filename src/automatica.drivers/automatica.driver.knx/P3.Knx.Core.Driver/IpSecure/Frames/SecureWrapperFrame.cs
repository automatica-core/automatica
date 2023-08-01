using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver.Frames;

namespace P3.Knx.Core.Driver.IpSecure.Frames
{
    internal class SecureWrapperFrame : SecureFrame
    {
        private readonly byte[] mFrame__;
        public KnxFrame InnerFrame { get; }


        private SecureWrapperFrame(KnxConnectionTunnelingSecure knx, KnxFrame frame)
            : base(knx, KnxHelper.ServiceType.SecureWrapper)
        {
            mFrame__ = frame.ToFrame();
            InnerFrame = frame;
        }


        internal static KnxFrame Parse(KnxConnectionTunnelingSecure knx, byte[] data, out bool valid)
        {
            try
            {
                byte[] dataHeader = new byte[8];

                Array.Copy(data, 0, dataHeader, 0, 8);

                byte[] sequenceNr = new byte[6];
                Array.Copy(data, 8, sequenceNr, 0, 6);

                byte[] serialNr = new byte[6];
                Array.Copy(data, 14, serialNr, 0, 6);

                byte[] mac = new byte[16];
                Array.Copy(data, data.Length - 16, mac, 0, 16);

                short payloadLength = Convert.ToInt16(data.Length - 38);
                byte[] payload = new byte[payloadLength];
                Array.Copy(data, 22, payload, 0, payloadLength);

                byte[] b = CryptoHelper.CreateDefaultB(sequenceNr, serialNr, new[] {data[20], data[21]},
                    BitConverter.GetBytes(payloadLength).Reverse().ToArray());
                byte[] ctr0 = CryptoHelper.CreateDefaultCtr(b);
                ctr0[14] = 0xff;

                byte[] decrypted = CryptoHelper.DecryptPayload2(knx.SessionKey, payload, ctr0);
                ctr0[15] = 0;
                byte[] yn = CryptoHelper.CalculateY(knx.SessionKey, decrypted, CryptoHelper.GetMsb(8, data), b);
                byte[] calcedMac = CryptoHelper.CalculateMac(knx.SessionKey, yn, ctr0);

                valid = mac.SequenceEqual(calcedMac);

                if (valid)
                {
                    return knx.Receiver.ParseFrame(decrypted);
                }
                return null;
            }
            catch (Exception e)
            {
                KnxHelper.Logger.LogError(e, "Unkown error occured");
                valid = false;
                return null;
            }
        }


        internal static SecureWrapperFrame Create(KnxConnectionTunnelingSecure knx, KnxFrame frame)
        {
            return new SecureWrapperFrame(knx, frame);
        }

        internal byte[] ToFrame(byte[] messageTag)
        {
            byte[] innerFrame = mFrame__;
            var lenBytes = Convert.ToByte(innerFrame.Length);
            byte[] innerFrameLength = BitConverter.GetBytes((int)lenBytes).Reverse().ToArray();

            byte[] datagram = new byte[38 + innerFrame.Length];

            datagram[0] = 0x06;
            datagram[1] = 0x10;
            datagram[2] = 0x09;
            datagram[3] = 0x50;
            datagram[4] = 0x00;
            datagram[5] = Convert.ToByte(38 + innerFrame.Length);

            Array.Copy(KnxConnectionSecure.SessionId, 0, datagram, 6, 2);

            UInt64 seq = KnxConnectionSecure.SequenceNumber;
            KnxConnectionSecure.IncSequenceNumber();

            byte[] seqNr = CryptoHelper.GetLsb(6, BitConverter.GetBytes(seq).Reverse().ToArray()).Reverse().ToArray();

            datagram[8] = seqNr[5]; //sequence number
            datagram[9] = seqNr[4];
            datagram[10] = seqNr[3];
            datagram[11] = seqNr[2];
            datagram[12] = seqNr[1];
            datagram[13] = seqNr[0];
        

            byte[] serial = KnxConnectionTunnelingSecure.SerialNumber;
            Array.Copy(serial, 0, datagram, 14, 6);
            Array.Copy(messageTag, 0, datagram, 20, 2);

            byte[] header = new byte[8];
            Array.Copy(datagram, 0, header, 0, 8);

            byte[] b0 = CryptoHelper.CreateDefaultB(seqNr.Reverse().ToArray(), serial, messageTag, innerFrameLength);
            byte[] ctr0 = CryptoHelper.CreateDefaultCtr(b0);

            byte[] yn = CryptoHelper.CalculateY(KnxConnectionSecure.SessionKey, innerFrame, header, b0);
            byte[] mac = CryptoHelper.CalculateMac(KnxConnectionSecure.SessionKey, yn, ctr0);
            byte[] payload = CryptoHelper.EncryptPayload2(KnxConnectionSecure.SessionKey, innerFrame, ctr0);

            Array.Copy(payload, 0, datagram, 22, payload.Length);
            Array.Copy(mac, 0, datagram, 22 + payload.Length, mac.Length);

            return datagram;
        }

        internal override byte[] ToFrame()
        {
            ushort messageTag = 0; //0 on unicast connection
            byte[] msgTagA = BitConverter.GetBytes(messageTag);
            msgTagA = msgTagA.Reverse().ToArray();

            return ToFrame(msgTagA);
        }
    }
}