using System;
using System.Threading.Tasks;
using P3.Driver.EnOcean.Data;

namespace P3.Driver.EnOcean
{
    public abstract class BaseStream
    {
        public abstract event EventHandler<PacketReceivedEventArgs> TelegramReceived;

        public abstract Task<bool> Open();
        public abstract Task<bool> Close();
        public abstract Task<EnOceanPacket> WriteFrame(EnOceanPacket frame);

        public abstract void Pause();
        public abstract void Continue();
    }
}
