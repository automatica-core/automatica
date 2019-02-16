using System;

namespace P3.Driver.ZWaveAeon.Channel
{
    public class NodeUpdateEventArgs : EventArgs
    {
        public readonly byte NodeID;

        public NodeUpdateEventArgs(byte nodeID)
        {
            if ((NodeID = nodeID) == 0)
                throw new ArgumentOutOfRangeException(nameof(NodeID), nodeID, "NodeID can not be 0");
        }
    }
}
