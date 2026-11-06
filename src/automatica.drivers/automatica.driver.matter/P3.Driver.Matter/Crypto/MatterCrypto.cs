using System.Security.Cryptography;

namespace P3.Driver.Matter.Crypto
{
    /// <summary>The crypto primitives of spec chapter 3 (P-256 / SHA-256 / HKDF / AES-CCM-128).</summary>
    public static class MatterCrypto
    {
        public const int SymmetricKeyLength = 16;
        public const int MicLength = 16;
        public const int NonceLength = 13;
        public const int PublicKeyLength = 65;
        public const int SignatureLength = 64;

        public static byte[] Sha256(params byte[][] parts)
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var p in parts)
            {
                sha.AppendData(p);
            }

            return sha.GetHashAndReset();
        }

        public static byte[] HmacSha256(byte[] key, byte[] data) => HMACSHA256.HashData(key, data);

        /// <summary>Crypto_KDF: HKDF-SHA256 (an empty salt means "no salt").</summary>
        public static byte[] Kdf(byte[] inputKey, byte[] salt, byte[] info, int length)
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, inputKey, length, salt ?? Array.Empty<byte>(), info ?? Array.Empty<byte>());
        }

        public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, int length)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, length);
        }

        public static byte[] RandomBytes(int count) => RandomNumberGenerator.GetBytes(count);

        /// <summary>AES-CCM-128 encryption, returns ciphertext || MIC.</summary>
        public static byte[] AeadEncrypt(byte[] key, byte[] nonce, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad)
        {
            var output = new byte[plaintext.Length + MicLength];
            using var ccm = new AesCcm(key);
            ccm.Encrypt(nonce, plaintext, output.AsSpan(0, plaintext.Length), output.AsSpan(plaintext.Length), aad);
            return output;
        }

        /// <summary>AES-CCM-128 decryption of ciphertext || MIC; throws <see cref="CryptographicException"/> on a bad MIC.</summary>
        public static byte[] AeadDecrypt(byte[] key, byte[] nonce, ReadOnlySpan<byte> ciphertextAndMic, ReadOnlySpan<byte> aad)
        {
            if (ciphertextAndMic.Length < MicLength)
            {
                throw new CryptographicException("Encrypted payload shorter than the MIC");
            }

            var len = ciphertextAndMic.Length - MicLength;
            var output = new byte[len];
            using var ccm = new AesCcm(key);
            ccm.Decrypt(nonce, ciphertextAndMic[..len], ciphertextAndMic[len..], output, aad);
            return output;
        }

        public static ECDsa CreateSigningKey() => ECDsa.Create(ECCurve.NamedCurves.nistP256);

        public static byte[] ExportPublicKey(ECDsa key) => EncodePoint(key.ExportParameters(false).Q);

        public static byte[] ExportPublicKey(ECDiffieHellman key) => EncodePoint(key.ExportParameters(false).Q);

        public static byte[] EncodePoint(ECPoint q)
        {
            var result = new byte[PublicKeyLength];
            result[0] = 0x04;
            Pad32(q.X).CopyTo(result, 1);
            Pad32(q.Y).CopyTo(result, 33);
            return result;
        }

        private static byte[] Pad32(byte[] v)
        {
            if (v.Length == 32)
            {
                return v;
            }

            var r = new byte[32];
            v.CopyTo(r, 32 - v.Length);
            return r;
        }

        public static ECPoint DecodePoint(ReadOnlySpan<byte> uncompressed)
        {
            if (uncompressed.Length != PublicKeyLength || uncompressed[0] != 0x04)
            {
                throw new MatterProtocolException("Expected an uncompressed P-256 public key");
            }

            return new ECPoint { X = uncompressed.Slice(1, 32).ToArray(), Y = uncompressed.Slice(33, 32).ToArray() };
        }

        public static ECDsa ImportPublicKey(ReadOnlySpan<byte> uncompressed)
        {
            return ECDsa.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = DecodePoint(uncompressed) });
        }

        /// <summary>ECDSA-SHA256 with the raw r||s (64 byte) signature format Matter uses on the wire.</summary>
        public static byte[] Sign(ECDsa key, ReadOnlySpan<byte> data)
        {
            return key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public static bool Verify(ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            using var key = ImportPublicKey(publicKey);
            return key.VerifyData(data, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public static ECDiffieHellman CreateEphemeralKey() => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        /// <summary>Crypto_ECDH: the X coordinate of the shared point.</summary>
        public static byte[] Ecdh(ECDiffieHellman own, ReadOnlySpan<byte> peerPublicKey)
        {
            using var peer = ECDiffieHellman.Create(new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = DecodePoint(peerPublicKey) });
            return own.DeriveRawSecretAgreement(peer.PublicKey);
        }

        public static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b) => CryptographicOperations.FixedTimeEquals(a, b);
    }
}
