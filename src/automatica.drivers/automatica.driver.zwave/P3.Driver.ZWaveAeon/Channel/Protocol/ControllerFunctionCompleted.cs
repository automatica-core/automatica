namespace P3.Driver.ZWaveAeon.Channel.Protocol
{
    class ControllerFunctionCompleted : ControllerFunctionMessage
    {
        public ControllerFunctionCompleted(Function function, byte[] payload)
            : base(MessageType.Response, function, payload)
        {
        }
    }
}
