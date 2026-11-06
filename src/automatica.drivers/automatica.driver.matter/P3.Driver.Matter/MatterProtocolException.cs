namespace P3.Driver.Matter
{
    public class MatterProtocolException : Exception
    {
        public MatterProtocolException(string message) : base(message)
        {
        }

        public MatterProtocolException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary>Raised by cluster handlers to answer a request with a specific interaction model status.</summary>
    public class MatterStatusException : Exception
    {
        public InteractionModel.StatusCode Status { get; }
        public byte? ClusterStatus { get; }

        public MatterStatusException(InteractionModel.StatusCode status, string message = null, byte? clusterStatus = null)
            : base(message ?? status.ToString())
        {
            Status = status;
            ClusterStatus = clusterStatus;
        }
    }
}
