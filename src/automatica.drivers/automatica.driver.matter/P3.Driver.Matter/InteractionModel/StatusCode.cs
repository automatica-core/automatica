namespace P3.Driver.Matter.InteractionModel
{
    /// <summary>Interaction model status codes (spec 8.10).</summary>
    public enum StatusCode : byte
    {
        Success = 0x00,
        Failure = 0x01,
        InvalidSubscription = 0x7D,
        UnsupportedAccess = 0x7E,
        UnsupportedEndpoint = 0x7F,
        InvalidAction = 0x80,
        UnsupportedCommand = 0x81,
        InvalidCommand = 0x85,
        UnsupportedAttribute = 0x86,
        ConstraintError = 0x87,
        UnsupportedWrite = 0x88,
        ResourceExhausted = 0x89,
        NotFound = 0x8B,
        UnreportableAttribute = 0x8C,
        InvalidDataType = 0x8D,
        UnsupportedRead = 0x8F,
        DataVersionMismatch = 0x92,
        Timeout = 0x94,
        Busy = 0x9C,
        UnsupportedCluster = 0xC3,
        NoUpstreamSubscription = 0xC5,
        NeedsTimedInteraction = 0xC6,
        UnsupportedEvent = 0xC7,
        PathsExhausted = 0xC8,
        TimedRequestMismatch = 0xC9,
        FailsafeRequired = 0xCA,
        InvalidInState = 0xCB,
        NoCommandResponse = 0xCC
    }

    public static class ImOpcode
    {
        public const byte StatusResponse = 0x01;
        public const byte ReadRequest = 0x02;
        public const byte SubscribeRequest = 0x03;
        public const byte SubscribeResponse = 0x04;
        public const byte ReportData = 0x05;
        public const byte WriteRequest = 0x06;
        public const byte WriteResponse = 0x07;
        public const byte InvokeRequest = 0x08;
        public const byte InvokeResponse = 0x09;
        public const byte TimedRequest = 0x0A;
    }
}
