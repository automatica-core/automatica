using System;

namespace P3.Knx.Core.Driver.Frames
{
    internal class WriteStatusFrame : KnxFrame
    {
        private readonly byte[] _data;
        private readonly string _ia;
        private WriteStatusFrame(KnxConnection knx, string ia, byte[] value)
            : base(knx, KnxHelper.ServiceType.TunnellingRequest)
        {
            _ia = ia;
            _data = value;
        }

        internal static WriteStatusFrame CreateFrame(KnxConnection knx, string ia, byte[] value)
        {
            return new WriteStatusFrame(knx, ia, value);
        }

        internal override byte[] ToFrame()
        {
            var dataLength = KnxHelper.GetDataLength(_data);

            // HEADER
            var datagram = new byte[10];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x04;
            datagram[03] = 0x20;

            var totalLength = BitConverter.GetBytes(dataLength + 20);
            datagram[04] = totalLength[1];
            datagram[05] = totalLength[0];

            datagram[06] = 0x04;
            datagram[07] = KnxConnection.ChannelId;
            datagram[08] = KnxConnection.GenerateSequenceNumber();
            datagram[09] = 0x00;

            return CreateActionDatagramCommon(_ia, _data, datagram);
        }

        private byte[] CreateActionDatagramCommon(string destinationAddress, byte[] data, byte[] header)
        {
            int i;
            var dataLength = KnxHelper.GetDataLength(data);

            // HEADER   
            var datagram = new byte[dataLength + 10 + header.Length];
            for (i = 0; i < header.Length; i++)
                datagram[i] = header[i];

            datagram[i++] =
                KnxConnection.ActionMessageCode != 0x00
                    ? KnxConnection.ActionMessageCode
                    : (byte)0x11;

            datagram[i++] = 0x00;
            datagram[i++] = 0xA4;

            datagram[i++] =
                KnxHelper.IsAddressIndividual(destinationAddress)
                    ? (byte)0x50
                    : (byte)0xE0;

            datagram[i++] = 0x00;
            datagram[i++] = 0x00;
            var dstAddress = KnxHelper.GetAddress(destinationAddress);
            datagram[i++] = dstAddress[0];
            datagram[i++] = dstAddress[1];
            datagram[i++] = (byte)dataLength;
            datagram[i++] = 0x00;
            datagram[i] = 0x80;

            KnxHelper.WriteData(datagram, data, i);

            return datagram;
        }


    }
}
