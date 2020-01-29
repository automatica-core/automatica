using SecureRemotePassword;

namespace P3.Driver.HomeKit.Hap.Controllers.Srp
{
    public interface ISrpGenerator
    {
        byte[] GenerateSalt(int length);
        string DerivePrivateKey(string salt, string userName, string password);
        string DeriveVerifier(string privateKey);
        SrpEphemeral GenerateServerEphemeral(string verifier);
        SrpSession DeriveServerSession(string serverEphemeralSecret, string clientPublicEphemeral, string salt, string userName, string verifier,
            string clientSessionProof);
    }
}
