using System;

namespace P3.Knx.Core.Driver.Frames
{
    internal class ConnectResponseFrame : KnxFrame
    {
        public bool IsValid { get; set; }
        public byte ChannelId { get; set; }

        public string IndividualAddress { get; set; }

        public string ErrorCode { get; set; }

        public bool Disconnect => !String.IsNullOrEmpty(ErrorCode);

        private ConnectResponseFrame(KnxConnection connection)
            : base(connection, KnxHelper.ServiceType.ConnectResponse)
        {
            IsValid = false;
        }

        internal static ConnectResponseFrame CreateFrame(byte[] datagram, KnxConnection knx)
        {
            ConnectResponseFrame frame = new ConnectResponseFrame(knx);
            var knxDatagram = new KnxDatagram(datagram)
            {
                HeaderLength = datagram[0],
                ProtocolVersion = datagram[1],
                ServiceType = new[] { datagram[2], datagram[3] },
                TotalLength = datagram[4] + datagram[5],
                ChannelId = datagram[6],
                Status = datagram[7]
            };
           

            if (knxDatagram.ChannelId == 0x00 && knxDatagram.Status == 0x24)
            {
                frame.ErrorCode = "E_NO_MORE_CONNECTIONS";
                return frame;
            }
            else if (knxDatagram.Status == 0x2B || knxDatagram.Status == 0x2A)
            {
                /*if (knx is KnxConnectionTunnelingSecure knxIpSec)
                {
                    knxIpSec.IpSecureErrorOccured?.Invoke(knx, new IpSecureEventArgs("E_NO_TUNNELLING_ADDRESS", KnxError.IpSecInvalidIndividualAddress));
                    knx.Disconnect();
                }*/
            }
            else if (knxDatagram.Status == 0x26)
            {
                frame.ErrorCode = "E_DATA_CONNECTION";
                return frame;
            }
            else if (knxDatagram.Status == 0x27)
            {
                frame.ErrorCode = "E_KNX_CONNECTION";
                return frame;
            }
            else
            {
                byte[] iab = new byte[2];
                Array.Copy(datagram, 18, iab, 0, 2);
                string ia = KnxHelper.GetIndividualAddress(iab);
                

                if (!String.IsNullOrEmpty(knx.IndividualAddress) && ia != knx.IndividualAddress)
                {
                //    ((KnxConnectionTunnelingSecure) knx).IpSecureErrorOccured.Invoke(knx,
                //        new IpSecureEventArgs("E_DATA_CONNECTION",
                //            KnxError.IpSecRespondedIndividualAddressDiffersToGiven));
                //    knx.Disconnect();
                    return frame;
                }

                frame.IndividualAddress = ia;
                frame.ChannelId = knxDatagram.ChannelId;
                frame.IsValid = true;
            }
            return frame;
        }

        internal override byte[] ToFrame()
        {
            throw new NotImplementedException();
        }
    }
}
