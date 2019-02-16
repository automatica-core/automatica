namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    class NodeInformation : Message
    {
        public NodeInformation(byte[] payload)
            : base(FrameHeader.SOF, MessageType.Response, Channel.Function.ApplicationUpdate)
        {
        }
   }
}
