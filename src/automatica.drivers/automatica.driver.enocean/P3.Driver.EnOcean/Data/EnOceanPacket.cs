using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using P3.Driver.EnOcean.Data.Packets;
using Automatica.Core.Driver.Utility;

namespace P3.Driver.EnOcean.Data
{
    public class EnOceanPacket
    {
        public ReadOnlyMemory<byte> Header { get;  }
        public ReadOnlyMemory<byte> Data { get;  }
        public ReadOnlyMemory<byte> OptionalData { get;  }
        public byte CrcData { get;  }
        public ReadOnlyMemory<byte> RawData { get; }

        public EnOcean.PacketType PacketType { get; set; }

        public byte SubTelNumber { get; private set; }
        public byte[] DestinationId { get; private set; }

        public string DestinationIdString => Utils.ByteArrayToString(DestinationId).Replace(" ", "");
        public byte Dbm { get; private set; }
        public bool SecurityLevel  { get; private set; }

        
        private EnOceanPacket(Span<byte> data)
        {
            RawData = new ReadOnlyMemory<byte>(data.ToArray());

            Header = data.Slice(0, 6).ToArray();

            var byteLen = data.Slice(1, 2);
            var optLen = Header.Span[3];

            byteLen.Reverse();

            var shortlen = BitConverter.ToUInt16(byteLen.ToArray(), 0);

            Data = data.Slice(6, shortlen).ToArray();
            OptionalData = data.Slice(6 + shortlen, optLen).ToArray();
        }
        

        private EnOceanPacket(ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> optionalData, byte crcData)
        {
            Header = header;
            Data = data;
            OptionalData = optionalData;
            CrcData = crcData;

            var completeDg = new List<byte>();
            completeDg.AddRange(header.ToArray());
            completeDg.AddRange(Data.ToArray());
            completeDg.AddRange(OptionalData.ToArray());
            completeDg.Add(CrcData);

            RawData = completeDg.ToArray();
        }
        public static EnOceanPacket Parse(ReadOnlyMemory<byte> header, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> optionalData, byte crcData)
        {
            var packet = new EnOceanPacket(header, data, optionalData, crcData);

            packet.ParseData();
            return packet;
        }

        public static EnOceanPacket Parse(Span<byte> data)
        {
            var packet = new EnOceanPacket(data);

            packet.ParseData();
            return packet;
        }

        public static EnOceanPacket CreateNew(Span<byte> header, Span<byte> data, Span<byte> optionalData)
        {
            var rawData = new List<byte>();
            
            byte headerCrc = 0;
            for(int i = 1; i < header.Length; i++)
            {
                headerCrc = EnOcean.Crc8(headerCrc, header[i]);
            }
            rawData.AddRange(header.ToArray());
            rawData.Add(headerCrc);

            rawData.AddRange(data.ToArray());
            rawData.AddRange(optionalData.ToArray());

            byte dataCrc = 0;
            foreach (var b in data)
            {
                dataCrc = EnOcean.Crc8(dataCrc, b);
            }
            foreach (var b in optionalData)
            {
                dataCrc = EnOcean.Crc8(dataCrc, b);
            }
            rawData.Add(dataCrc);

            var p = new EnOceanPacket(rawData.ToArray());
            return p;
        }

        internal void ParseData()
        {
            PacketType = (EnOcean.PacketType)Header.Span[4];

            if (PacketType == EnOcean.PacketType.RadioErp1)
            {
                SubTelNumber = OptionalData.Span[0];

                ReadOnlySpan<byte> t = OptionalData.Span;
                DestinationId = t.Slice(1, 4).ToArray();
                Dbm = t[5];
                SecurityLevel = t[6] == 1;
            }
        }

        public override string ToString()
        {
            return Utils.ByteArrayToString(Header) + Utils.ByteArrayToString(Data) + Utils.ByteArrayToString(OptionalData) + Utils.ByteArrayToString(new byte[] { CrcData });
        }
    }
}
