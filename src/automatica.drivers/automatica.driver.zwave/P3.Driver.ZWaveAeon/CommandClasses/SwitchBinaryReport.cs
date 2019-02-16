using System;
using P3.Driver.ZWaveAeon.Channel.Protocol;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class SwitchBinaryReport : NodeReport
    {
        public readonly bool Value;

        internal SwitchBinaryReport(Node node, byte[] payload) : base(node)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length < 1)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload)}");

            Value = payload[0] == 0xFF;
        }

        public override string ToString()
        {
            return $"Value:{Value}";
        }
    }
}
