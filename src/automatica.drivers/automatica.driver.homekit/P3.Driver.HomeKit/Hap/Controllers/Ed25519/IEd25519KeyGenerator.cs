namespace P3.Driver.HomeKit.Hap.Controllers.Ed25519
{
    public class KeyPair
    {
        public KeyPair(byte[] publicKey, byte[] privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] PublicKey { get; }
        public byte[] PrivateKey { get; }
    }
    public interface IEd25519KeyGenerator
    {
        KeyPair GenerateNewPair();
    }
}
