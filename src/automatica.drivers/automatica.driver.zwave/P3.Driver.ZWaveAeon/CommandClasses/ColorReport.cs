using System;
using P3.Driver.ZWaveAeon.Channel.Protocol;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class ColorReport : NodeReport
    {
        public readonly ColorComponent Component;

        internal ColorReport(Node node, byte[] payload) : base(node)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length < 2)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload)}");

            Component = new ColorComponent(payload[0], payload[1]);
        }

        public override string ToString()
        {
            return $"Component:{Component}";
        }
    }
}
