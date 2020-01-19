using System.Security.Cryptography;

namespace P3.Driver.HomeKit.Hap.Controllers.Ed25519
{
    public class Ed25519KeyGenerator : IEd25519KeyGenerator
    {
        public KeyPair GenerateNewPair()
        {
            var seed = new byte[32];
            RandomNumberGenerator.Create().GetBytes(seed);
            Chaos.NaCl.Ed25519.KeyPairFromSeed(out var publicKey, out var privateKey, seed);

            return new KeyPair(publicKey, privateKey);
        }
    }
}
