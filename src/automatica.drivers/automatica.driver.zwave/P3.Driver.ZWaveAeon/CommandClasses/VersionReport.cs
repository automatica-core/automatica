using System;
using P3.Driver.ZWaveAeon.Channel.Protocol;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class VersionReport : NodeReport
    {
        public readonly string Library;
        public readonly string Application;
        public readonly string Protocol;

        internal VersionReport(Node node, byte[] payload) : base(node)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length < 5)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload)}");

            Library = payload[0].ToString("d");
            Protocol = payload[1].ToString("d") + "." + payload[2].ToString("d2");
            Application = payload[3].ToString("d") + "." + payload[4].ToString("d2");
        }

        public override string ToString()
        {
            return $"Library:{Library}, Protocol:{Protocol}, Application:{Application}";
        }
    }
}
