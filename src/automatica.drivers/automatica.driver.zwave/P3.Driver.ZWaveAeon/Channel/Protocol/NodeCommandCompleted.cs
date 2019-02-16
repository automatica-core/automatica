namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    class NodeCommandCompleted : Message
    {
        public readonly byte CallbackID;
        public readonly TransmissionState TransmissionState;

        public NodeCommandCompleted(byte[] payload) : 
            base(FrameHeader.SOF, MessageType.Request, Channel.Function.SendData)
        {
            CallbackID = payload[0];
            TransmissionState = (TransmissionState)payload[1];
        }

        public override string ToString()
        {
            return string.Concat(base.ToString(), " ", $"CallbackID:{CallbackID}, {TransmissionState}");
        }
    }
}
