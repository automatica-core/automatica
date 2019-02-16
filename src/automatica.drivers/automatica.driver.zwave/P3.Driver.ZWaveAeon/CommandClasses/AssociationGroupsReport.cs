using System;
using P3.Driver.ZWaveAeon.Channel.Protocol;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class AssociationGroupsReport : NodeReport
    {
        public readonly byte GroupsSupported;

        internal AssociationGroupsReport(Node node, byte[] payload) : base(node)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payload.Length < 1)
                throw new ReponseFormatException($"The response was not in the expected format. {GetType().Name}: Payload: {BitConverter.ToString(payload)}");

            GroupsSupported = payload[0];
        }

        public override string ToString()
        {
            return $"GroupsSupported:{GroupsSupported}";
        }
    }
}
