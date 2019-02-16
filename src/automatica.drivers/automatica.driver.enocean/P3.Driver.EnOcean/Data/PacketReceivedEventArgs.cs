using System;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.Data
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public EnOceanTelegram Telegram { get; }

        public PacketReceivedEventArgs(EnOceanTelegram telegram)
        {
            Telegram = telegram;
        }
    }
}
