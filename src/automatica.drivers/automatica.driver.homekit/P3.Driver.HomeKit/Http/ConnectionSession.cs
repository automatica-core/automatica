using SecureRemotePassword;

namespace P3.Driver.HomeKit.Http
{
    public class ConnectionSession
    {
        public SrpEphemeral ServerEphemeral { get; set; }
        public string Verifier { get; set; }
        public SrpSession ServerSession { get; set; }

        public byte[] Salt { get; set; }
    }
}
