using System;

namespace P3.Knx.Core.Baos.Driver.Data
{
    public class DatapointValue
    {
        public byte State { get; set; }
        public ushort DatapointId { get; set; }
        public byte Length { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
    }
}
