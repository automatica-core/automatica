
using System;

namespace P3.Driver.EnOcean.Data.Packets
{
    public abstract class EnOceanTelegram
    {
    

        public abstract void FromPacket(EnOceanPacket packet);
        public abstract EnOceanPacket ToPacket();

        public virtual void SetIdBase(ReadOnlySpan<byte> id)
        {

        }
    }
}
