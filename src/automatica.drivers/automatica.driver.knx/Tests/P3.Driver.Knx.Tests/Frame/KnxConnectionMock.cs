using System.Net;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.Tests.Frame
{
    public class KnxConnectionMock : IKnxConnection
    {
        public byte ChannelId => 0;
        public byte ActionMessageCode => 0;
        public IPEndPoint LocalEndpoint { get; }
        public bool UseNat => false;
        public byte GenerateSequenceNumber()
        {
            return 0;
        }
    }
}
