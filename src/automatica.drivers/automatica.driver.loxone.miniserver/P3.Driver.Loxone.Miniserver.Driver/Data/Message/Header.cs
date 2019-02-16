using System;

namespace P3.Driver.Loxone.Miniserver.Driver.Data.Message
{
    public enum HeaderIdentifier
    {
        TextMessage,
        BinaryFile,
        EventTableOfValueStates,
        EventTableOfTextStates,
        EventTableOfDaytimerStates,
        OutOfService,
        KeepAlive,
        EventTableOfWeatherStates
    }
    public class Header
    {
        public Header(Span<byte> header)
        {
            if(header == null)
            {
                throw new ArgumentException("Header must be set");
            }
            if(header.Length != 8)
            {
                throw new ArgumentException("Header must have size of 8");
            }

            Identifier = (HeaderIdentifier)header[1];
            Info = header[2];

            var len = header.Slice(4, 4);
            DataLength = BitConverter.ToUInt32(len.ToArray(), 0);          
        }

        public HeaderIdentifier Identifier { get; set; }
        public byte Info { get; set; }
        public uint DataLength { get; set; }
    }
}
