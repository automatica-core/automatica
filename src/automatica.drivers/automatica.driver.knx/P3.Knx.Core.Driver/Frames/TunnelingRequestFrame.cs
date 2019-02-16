using System;

namespace P3.Knx.Core.Driver.Frames
{
    internal class TunnelingRequestFrame : KnxFrame
    {
        private TunnelingRequestFrame(KnxConnection knx)
            : base(knx, KnxHelper.ServiceType.TunnellingRequest)
        {
            IsValid = false;
        }

        public bool IsValid { get; set; }
        public byte FrameSequenceNumber { get; set; }
        public byte[] CemiPacket { get; set; }

        internal KnxDatagram Datagram { get; set; }

        internal static TunnelingRequestFrame CreateFrame(byte[] datagram, KnxConnection knx, int rxSequenceNumber, bool isFirstRequest)
        {
            TunnelingRequestFrame frame = new TunnelingRequestFrame(knx);


            var knxDatagram = new KnxDatagram(datagram)
            {
                HeaderLength = datagram[0],
                ProtocolVersion = datagram[1],
                ServiceType = new[] { datagram[2], datagram[3] },
                TotalLength = datagram[4] + datagram[5]
            };

            var channelId = datagram[7];
            if (channelId != knx.ChannelId)
                return frame;

            var sequenceNumber = datagram[8];
            var isValid = isFirstRequest || (sequenceNumber >= rxSequenceNumber);

            frame.FrameSequenceNumber = sequenceNumber;
            frame.IsValid = isValid;
            if (isValid)
            {
                var cemi = new byte[datagram.Length - 10];
                Array.Copy(datagram, 10, cemi, 0, datagram.Length - 10);

                frame.CemiPacket = cemi;
                frame.IsValid = true;
                frame.Datagram = knxDatagram;
            }

            return frame;
        }

        internal override byte[] ToFrame()
        {
            throw new NotImplementedException();
        }
    }
}
