using System.Net.Sockets;

namespace P3.Driver.HomeKit.Hap
{
    internal class HapSession
    {
        public bool IsVerified { get; internal set; }
        public byte[] SharedSecret { get; internal set; }
        public byte[] HkdfPairEncKey { get; internal set; }
        public byte[] PublicKey { get; internal set; }
        public byte[] PrivateKey { get; internal set; }
        public byte[] ClientPublicKey { get; internal set; }
        public bool SkipFirstEncryption { get; internal set; }
        public byte[] AccessoryToControllerKey { get; internal set; }
        public byte[] ControllerToAccessoryKey { get; internal set; }

        public long InboundBinaryMessageCount { get; internal set; }
        public long OutboundBinaryMessageCount { get; internal set; }
        public TcpClient Client { get; set; }

        public string ClientUsername { get; set; }
    }
}
