using System;

namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    class UnknownMessage : Message
    {
        public readonly byte[] Payload;

        public UnknownMessage(FrameHeader header, MessageType type, Function function, byte[] payload)
            : base(header, type, function)
        {
            Payload = payload;
        }

        public override string ToString()
        {
            return string.Concat(base.ToString(), " ", $"Payload:{BitConverter.ToString(Payload)}");
        }
    }
}
