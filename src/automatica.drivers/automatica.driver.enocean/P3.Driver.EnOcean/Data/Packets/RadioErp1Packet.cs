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
        Ute = 0xD4,
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

        /// <summary>
        /// Status byte for sent RPS telegrams: T21 = 1 and NU = 1, i.e. a rocker press (PTM2xx).
        /// </summary>
        public const byte StatusRpsPressed = 0x30;

        /// <summary>
        /// Status byte for sent RPS telegrams: T21 = 1 and NU = 0, i.e. a rocker release (PTM2xx).
        /// </summary>
        public const byte StatusRpsReleased = 0x20;

        public RadioErp1Packet(Rorg rorg, ReadOnlyMemory<byte> packet) : this(rorg, packet, StatusRpsPressed)
        {
        }

        public RadioErp1Packet(Rorg rorg, ReadOnlyMemory<byte> packet, byte status)
        {
            Data = packet;
            Rorg = rorg;
            Status = status;
        }

        public Rorg Rorg { get; private set; }

        public ReadOnlyMemory<byte> Data { get; private set; }

        public ReadOnlyMemory<byte> SenderId { get; private set; }

        public string SenderIdString => Automatica.Core.Driver.Utility.Utils.ByteArrayToString(SenderId).Replace(" ", "");

        public EnOceanPacket Packet { get; private set; }

        public byte Status { get; private set; }

        /// <summary>
        /// Sent telegrams: the sender id is the id base of the USB stick plus this offset (0..127). Actuators learn the
        /// sender id, a different offset per actuator keeps them apart.
        /// </summary>
        public byte SenderOffset { get; set; }

        /// <summary>
        /// The id base plus <see cref="SenderOffset"/>.
        /// </summary>
        public static byte[] SenderIdFor(ReadOnlySpan<byte> idBase, byte offset)
        {
            var sender = idBase.ToArray();
            if (sender.Length != 4 || offset == 0)
            {
                return sender;
            }

            var id = ((uint)sender[0] << 24 | (uint)sender[1] << 16 | (uint)sender[2] << 8 | sender[3]) + offset;
            return new[] { (byte)(id >> 24), (byte)(id >> 16), (byte)(id >> 8), (byte)id };
        }

        public override void FromPacket(EnOceanPacket packet)
        {
            packet.ParseData();
            Packet = packet;
            Rorg = (Rorg) packet.Data.Span[0];

            var dataLength = GetRorgDataSize(packet.Data.Length);
            
            Data = packet.Data.Slice(1, dataLength);

            SenderId = packet.Data.Slice(dataLength + 1, 4);
            Status = packet.Data.Span[packet.Data.Length - 1];

            Logger.Logger.Instance.LogDebug($"RadioERP1 Packet {Rorg} from {Utils.ByteArrayToString(SenderId)}  Data: {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(Data)}");
        }

        /// <summary>
        /// ERP1 payload layout is RORG (1) + user data (n) + sender id (4) + status (1), so the user data length
        /// follows from the packet length for every RORG (RPS/1BS = 1, 4BS = 4, VLD/UTE/MSC = variable).
        /// </summary>
        private static int GetRorgDataSize(int packetDataLength)
        {
            const int overhead = 1 + 4 + 1;
            if (packetDataLength < overhead)
            {
                throw new ArgumentException($"RadioERP1 packet too short ({packetDataLength} bytes)");
            }

            return packetDataLength - overhead;
        }

        /// <summary>
        /// True if the telegram can be used to teach in a device. RPS has no LRN bit, so every RPS telegram counts.
        /// </summary>
        public static bool IsTechIn(RadioErp1Packet radio)
        {
            if (radio.Rorg == Rorg.Rps)
            {
                return true;
            }

            return IsLrnTeachIn(radio);
        }

        /// <summary>
        /// True only for 1BS/4BS telegrams whose LRN bit (DB0.3) is cleared, i.e. real teach-in telegrams
        /// whose data bytes carry no values.
        /// </summary>
        public static bool IsLrnTeachIn(RadioErp1Packet radio)
        {
            var data = radio.Data.Span;
            switch (radio.Rorg)
            {
                case Rorg.OneBs:
                    return data.Length >= 1 && !Utils.IsBitSet(data[0], 3);
                case Rorg.FourBs:
                    return data.Length >= 4 && !Utils.IsBitSet(data[3], 3);
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
            data.AddRange(SenderIdFor(_idBase.Span, SenderOffset));
            data.Add(Status);

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
