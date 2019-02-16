namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    class ControllerFunctionEvent : ControllerFunctionMessage
    {
        public ControllerFunctionEvent(Function function, byte[] payload)
            : base(MessageType.Request, function, payload)
        {
        }
    }
}
