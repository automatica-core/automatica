using System;
using SecureRemotePassword;

namespace P3.Driver.HomeKit.Hap.Controllers.Srp
{
    internal class SrpGenerator : ISrpGenerator
    {
        private readonly SrpClient _srpClient;
        private readonly SrpServer _srpServer;
        public SrpGenerator(SrpParameters parameters)
        {
            _srpClient = new SrpClient(parameters);
            _srpServer = new SrpServer(parameters);
        }
        public byte[] GenerateSalt(int length)
        {
            var rnd = new Random();
            var salt = new byte[length];
            rnd.NextBytes(salt);

            return salt;
        }

        public string DerivePrivateKey(string salt, string userName, string password)
        {
            return _srpClient.DerivePrivateKey(salt, userName, password);
        }

        public string DeriveVerifier(string privateKey)
        {
            return _srpClient.DeriveVerifier(privateKey);
        }

        public SrpEphemeral GenerateServerEphemeral(string verifier)
        {
            return _srpServer.GenerateEphemeral(verifier);
        }

        public SrpSession DeriveServerSession(string serverEphemeralSecret, string clientPublicEphemeral, string salt, string userName, string verifier,
            string clientSessionProof)
        {
            return _srpServer.DeriveSession(serverEphemeralSecret, clientPublicEphemeral, salt, userName, verifier,
                clientSessionProof);
        }
    }
}
