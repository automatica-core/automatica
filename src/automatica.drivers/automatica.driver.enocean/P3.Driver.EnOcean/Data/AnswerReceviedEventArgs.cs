using System;

namespace P3.Driver.EnOcean.Data
{
    public class AnswerReceviedEventArgs : EventArgs
    {
        public EnOceanPacket Packet{ get; }

        public AnswerReceviedEventArgs(EnOceanPacket packet)
        {
            Packet = packet;
        }
    }
}
