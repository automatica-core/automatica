using System;
using System.Collections.Generic;
using System.Text;
using P3.Driver.EnOcean.Data.Packets;

namespace P3.Driver.EnOcean.Data
{
    public static class EnOceanTelegramFactory
    {
        public static EnOceanTelegram FromPacket(EnOceanPacket packet)
        {
            EnOceanTelegram telegram;
            switch (packet.PacketType)
            {
                case EnOcean.PacketType.RadioErp1:
                {
                    telegram = new RadioErp1Packet();
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            telegram.FromPacket(packet);
            return telegram;
        }

    }
}
