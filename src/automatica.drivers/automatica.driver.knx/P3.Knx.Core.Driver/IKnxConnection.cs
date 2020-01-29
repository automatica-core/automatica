using System.Net;

namespace P3.Knx.Core.Driver
{
    internal interface IKnxConnection
    {
        byte ChannelId { get; }
        byte ActionMessageCode { get; }
        IPEndPoint LocalEndpoint { get; }
        bool UseNat { get;  }
        byte GenerateSequenceNumber();
    }
}
