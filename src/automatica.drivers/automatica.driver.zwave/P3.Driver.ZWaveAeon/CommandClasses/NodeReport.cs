using System;

namespace P3.Driver.ZWaveAeon.CommandClasses
{
    public class NodeReport 
    {
        public readonly Node Node;

        public NodeReport(Node node)
        {
            if ((Node = node) == null)
                throw new ArgumentNullException(nameof(node));
        }
    }
}
