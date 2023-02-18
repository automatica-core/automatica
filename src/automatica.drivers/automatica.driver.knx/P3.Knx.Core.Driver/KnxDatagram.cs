namespace P3.Knx.Core.Driver
{
    public class KnxDatagram
    {
        private readonly byte[] datagram;

        public KnxDatagram(byte[] datagram)
        {
            this.datagram = datagram;
        }
        // HEADER
        public int HeaderLength { get; set; }
        public byte ProtocolVersion { get; set; }
        public byte[] ServiceType { get; set; }
        public int TotalLength { get; set; }

        // CONNECTION
        public byte ChannelId { get; set; }
        public byte Status { get; set; }

        // CEMI
        public byte MessageCode { get; set; }
        public int AditionalInfoLength { get; set; }
        public byte[] AditionalInfo { get; set; }
        public byte ControlField1 { get; set; }
        public byte ControlField2 { get; set; }
        public string SourceAddress { get; set; }
        public string DestinationAddress { get; set; }
        public int DataLength { get; set; }
        public byte[] Apdu { get; set; }
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return Automatica.Core.Driver.Utility.Utils.ByteArrayToString(datagram);
        }
    }
}
