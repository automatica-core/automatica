using System;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.Data
{
    public class PacketSentEventArgs : EventArgs
    {
        public EnOceanPacket Packet{ get; }
        public EnOceanTelegram Telegram { get; }

        public PacketSentEventArgs(EnOceanPacket packet, EnOceanTelegram telegram)
        {
            Packet = packet;
            Telegram = telegram;
        }
    }
}
