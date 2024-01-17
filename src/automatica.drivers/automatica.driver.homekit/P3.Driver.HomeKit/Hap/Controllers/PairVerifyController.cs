using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Elliptic;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal class PairVerifyReturn
    {
        public string ContentType { get; set; } = "application/pairing+tlv8";
        public Tlv TlvData { get; set; }
        public bool Ok { get; set; }

        public HapSession HapSession { get; set; }

    }

    internal class PairVerifyController : BaseController
    {
        private readonly ILogger _logger;

        public PairVerifyController(ILogger logger)
        {
            _logger = logger;
        }


        public PairVerifyReturn Post(Tlv parts, HapSession session)
        {
            var state = parts.GetTypeAsInt(Constants.State);

            if (state == 1)
            {
                _logger.LogDebug("* Pair Verify Step 1/4");
                _logger.LogDebug("* Verify Start Request");

                var clientPublicKey = parts.GetType(Constants.PublicKey);

                byte[] privateKey = new byte[32];

                Random random = new Random();
                random.NextBytes(privateKey);
                    
                var publicKey = Curve25519.GetPublicKey(privateKey);
                var sharedSecret = Curve25519.GetSharedSecret(privateKey, clientPublicKey);

                var serverUsername = Encoding.UTF8.GetBytes(HapControllerServer.HapControllerId);

                var material = publicKey.Concat(serverUsername).Concat(clientPublicKey).ToArray();
                var accessoryLtsk = StringToByteArray(HapControllerServer.HapControllerLtsk);

                var proof = Chaos.NaCl.Ed25519.Sign(material, accessoryLtsk);

                var hdkf = new HkdfSha512();
                var hkdfEncKey = hdkf.DeriveBytes(SharedSecret.Import(sharedSecret), Encoding.UTF8.GetBytes("Pair-Verify-Encrypt-Salt"),
                    Encoding.UTF8.GetBytes("Pair-Verify-Encrypt-Info"), 32);

                var encoder = new Tlv();
                encoder.AddType(Constants.Identifier, serverUsername);
                encoder.AddType(Constants.Signature, proof);
                var plaintext = TlvParser.Serialize(encoder);

                var zeros = new byte[] { 0, 0, 0, 0 };
                var nonce = new Nonce(zeros, Encoding.UTF8.GetBytes("PV-Msg02"));

                var encryptedOutput = AeadAlgorithm.ChaCha20Poly1305.Encrypt(
                    Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdfEncKey, KeyBlobFormat.RawSymmetricKey), nonce, Array.Empty<byte>(), plaintext);

                var responseTlv = new Tlv();
                responseTlv.AddType(Constants.State, 2);
                responseTlv.AddType(Constants.EncryptedData, encryptedOutput);
                responseTlv.AddType(Constants.PublicKey, publicKey);

                // Store the details on the session.
                //
                session.ClientPublicKey = clientPublicKey;
                session.PrivateKey = privateKey;
                session.PublicKey = publicKey;
                session.SharedSecret = sharedSecret;
                session.HkdfPairEncKey = hkdfEncKey;


                var encSalt = Encoding.UTF8.GetBytes("Control-Salt");
                var infoRead = Encoding.UTF8.GetBytes("Control-Read-Encryption-Key");
                var infoWrite = Encoding.UTF8.GetBytes("Control-Write-Encryption-Key");

                session.AccessoryToControllerKey = hdkf.DeriveBytes(SharedSecret.Import(sharedSecret, SharedSecretBlobFormat.RawSharedSecret), encSalt, infoRead, 32);
                session.ControllerToAccessoryKey = hdkf.DeriveBytes(SharedSecret.Import(sharedSecret, SharedSecretBlobFormat.RawSharedSecret), encSalt, infoWrite, 32);

                return new PairVerifyReturn
                {
                    TlvData = responseTlv,
                    Ok = true,
                    HapSession = session
                };
            }

            if (state == 3)
            {

                _logger.LogDebug("* Pair Verify Step 3/4");
                _logger.LogDebug("* Verify Finish Request");

                var encryptedData = parts.GetType(Constants.EncryptedData);
                var zeros = new byte[] { 0, 0, 0, 0 };
                var nonce = new Nonce(zeros, Encoding.UTF8.GetBytes("PV-Msg03"));

                var decrypt = AeadAlgorithm.ChaCha20Poly1305.Decrypt(Key.Import(AeadAlgorithm.ChaCha20Poly1305, session.HkdfPairEncKey, KeyBlobFormat.RawSymmetricKey), nonce, new byte[0], encryptedData, out var output);

                if (!decrypt)
                {
                    _logger.LogWarning($"Error decrypting message...");
                    var errorTlv = new Tlv();
                    errorTlv.AddType(Constants.State, 4);
                    errorTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                    return new PairVerifyReturn
                    {
                        TlvData = errorTlv,
                        Ok = false
                    };
                }

                var subData = TlvParser.Parse(output);
                var clientUserName = subData.GetType(Constants.Identifier);
                var signature = subData.GetType(Constants.Signature);

                var clientPublicKey = StringToByteArray(HapControllerServer.HapControllerLtpk);
                var material = session.ClientPublicKey.Concat(clientUserName).Concat(session.PublicKey).ToArray();

                session.ClientUsername = Automatica.Core.Driver.Utility.Utils.ByteArrayToString(in clientUserName);
              
                if (!Chaos.NaCl.Ed25519.Verify(signature, material, clientPublicKey))
                {
                    _logger.LogWarning($"Error decrypting message...");
                    var errorTlv = new Tlv();
                    errorTlv.AddType(Constants.State, 4);
                    errorTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                    return new PairVerifyReturn
                    {
                        TlvData = errorTlv,
                        Ok = false
                    };
                }

                var responseTlv = new Tlv();
                responseTlv.AddType(Constants.State, 4);

                session.IsVerified = true;
                session.SkipFirstEncryption = true;

                return new PairVerifyReturn
                {
                    Ok = true,
                    TlvData = responseTlv
                };
            }

            return null;
        }
    }
}
