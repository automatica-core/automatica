using System;
using System.Collections.Generic;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;

namespace P3.Driver.EnOcean.Data.Packets
{

    public enum Rorg
    {
        Rps = 0xF6,
        OneBs = 0xD5,
        FourBs = 0xA5,
        Vld = 0xD2,
        Msc = 0xD1,
        Adt = 0xA6,
        SmLrnReq = 0xC6,
        SmLrnAns = 0xC7,
        SmRec = 0xA7,
        SysEx = 0xC5,
        Sec = 0x30,
        SecEncaps = 0x1
    }

    public class RadioErp1Packet : EnOceanTelegram
    {
        private ReadOnlyMemory<byte> _idBase;

        public RadioErp1Packet() : base()
        {
           
        }

        public RadioErp1Packet(Rorg rorg, ReadOnlyMemory<byte> packet)
        {
            Data = packet;
            Rorg = rorg;
        }

        public Rorg Rorg { get; private set; }

        public ReadOnlyMemory<byte> Data { get; private set; }

        public ReadOnlyMemory<byte> SenderId { get; private set; }

        public string SenderIdString => Automatica.Core.Driver.Utility.Utils.ByteArrayToString(SenderId).Replace(" ", "");

        public EnOceanPacket Packet { get; private set; }

        public byte Status { get; private set; }

        public override void FromPacket(EnOceanPacket packet)
        {
            packet.ParseData();
            Packet = packet;
            Rorg = (Rorg) packet.Data.Span[0];

            var dataLength = GetRorgDataSize(Rorg);
            
            Data = packet.Data.Slice(1, dataLength);

            SenderId = packet.Data.Slice(dataLength + 1, 4);
            Status = packet.Data.Span[packet.Data.Length - 1];

            Logger.Logger.Instance.LogDebug($"RadioERP1 Packet {Rorg} from {Utils.ByteArrayToString(SenderId)}  Data: {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(Data)}");
        }

        private int GetRorgDataSize(Rorg rorg)
        {
            switch (rorg)
            {
                case Rorg.OneBs:
                case Rorg.Rps:
                    return 1;
                case Rorg.FourBs:
                    return 4;


                default:
                    return 0;
            }
        }

        public static bool IsTechIn(RadioErp1Packet radio)
        {
            switch (radio.Rorg)
            {
                case Rorg.Rps:
                    return true;
                case Rorg.OneBs:
                    return !Utils.IsBitSet(radio.Data.ToArray()[0], 3);
                case Rorg.FourBs:
                    return (radio.Data.ToArray()[3] & 0x16) > 0;
            }

            return false;
        }

        public override void SetIdBase(ReadOnlySpan<byte> id)
        {
            _idBase = new ReadOnlyMemory<byte>(id.ToArray());
            base.SetIdBase(id);
        }

        public override EnOceanPacket ToPacket()
        {
            var data = new List<byte>();

            data.Add((byte)Rorg);
            data.AddRange(Data.Span.ToArray()); //TODO: Change to Packet function
            //source id
            data.AddRange(_idBase.ToArray());
            data.Add(0x30);

            var optionalData = new List<byte>();
            //optional data
            optionalData.Add(3);
            optionalData.Add(0xFF);
            optionalData.Add(0xFF);
            optionalData.Add(0xFF);
            optionalData.Add(0xFF);
            optionalData.Add(0xFF); //dBM
            optionalData.Add(0); //security level

            var headerBuf = new List<byte>();
            headerBuf.Add(0x55);
            headerBuf.Add((byte)(data.Count >> 8));
            headerBuf.Add((byte)data.Count);
            headerBuf.Add(7);
            headerBuf.Add((byte)EnOcean.PacketType.RadioErp1);

            return EnOceanPacket.CreateNew(headerBuf.ToArray(), data.ToArray(), optionalData.ToArray());
        }
    }
}
