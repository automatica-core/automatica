using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap.TlvData;
using SecureRemotePassword;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal sealed class PairSetupReturn
    {
        public Tlv TlvData { get; set; }
        public int State { get; set; }
        public bool Ok { get; set; } = false;
        public string ContentType { get; set; } = "application/pairing+tlv8";

        public string Ltsk { get; set; }
        public string Ltpk { get; set; }

    }

    internal sealed class PairSetupController : BaseController
    {
        private readonly ILogger _logger; //todo: log more data
        private static string _code;
        private static byte[] _salt;

        private string _verifier;
        private string _privateKey;
        private SrpEphemeral _serverEphemeral;
        private SrpInteger _saltInt;
        private SrpServer _server;
        private SrpSession _serverSession;

        private const string Username = "Pair-Setup";

        public PairSetupController(ILogger logger, string code)
        {
            _logger = logger;
            _code = code;
        }

        public PairSetupReturn Post(Tlv parts)
        {
            var customParams = SrpParameters.Create3072<SHA512>();

            var state = parts.GetTypeAsInt(Constants.State);

            _logger.LogDebug($"State is {state}");

            if (state == 1) //srp sign up
            {
                var rnd = new Random();
                _salt = new byte[16];
                rnd.NextBytes(_salt);

                _saltInt = SrpInteger.FromByteArray(_salt);
                
                var srp = new SrpClient(customParams);
                _privateKey = srp.DerivePrivateKey(_saltInt.ToHex(), Username, _code);
                _verifier = srp.DeriveVerifier(_privateKey);

                _server = new SrpServer(customParams);
                _serverEphemeral  = _server.GenerateEphemeral(_verifier);
               
                var responseTlv = new Tlv();
                responseTlv.AddType(Constants.State, 2);
                responseTlv.AddType(Constants.PublicKey, StringToByteArray(_serverEphemeral.Public));
                responseTlv.AddType(Constants.Salt, _salt);

                _logger.LogDebug($"return salt {_salt}, pub {_serverEphemeral.Public} and state 2");

                return new PairSetupReturn
                {
                    State = 1,
                    TlvData = responseTlv,
                    Ok = true
                };
            }

            if (state == 3) //srp authenticate
            {
                _logger.LogDebug("Pair Setup Step 3/6");
                _logger.LogDebug("SRP Verify Request");

                var pubKey = parts.GetType(Constants.PublicKey);
                var proof = parts.GetType(Constants.Proof);

                var iOsPublicKey = SrpInteger.FromByteArray(pubKey);
                var iOsProof = SrpInteger.FromByteArray(proof); 
          
           
                var responseTlv = new Tlv();
                responseTlv.AddType(Constants.State, 4);
                var ok = true;
                try
                {
                    _serverSession = _server.DeriveSession(_serverEphemeral.Secret, iOsPublicKey.ToHex(), _saltInt.ToHex(), Username, _verifier,
                        iOsProof.ToHex());
                    _logger.LogInformation("Verification was successful. Generating Server Proof (M2)");

                    responseTlv.AddType(Constants.Proof, StringToByteArray(_serverSession.Proof));


                    _logger.LogDebug($"return proof {_serverSession.Proof}, secret {_serverEphemeral.Secret} and state 4");
                }
                catch(Exception)
                {
                    ok = false;
                    _logger.LogError("Verification failed as iOS provided code was incorrect");
                    responseTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                }
                return new PairSetupReturn
                {
                    State = 3,
                    Ok = ok,
                    TlvData = responseTlv
                };

            }

            if (state == 5)
            {
                _logger.LogDebug("Pair Setup Step 5/6");
                _logger.LogDebug("Exchange Response");

                try
                {

                    var iOsEncryptedData = parts.GetType(Constants.EncryptedData).AsSpan(); // A 
                    var zeros = new byte[] {0, 0, 0, 0};
                    var nonce = new Nonce(zeros, Encoding.UTF8.GetBytes("PS-Msg05"));
                    var hdkf = new HkdfSha512();
                    var hkdfEncKey = hdkf.DeriveBytes(
                        SharedSecret.Import(SrpInteger.FromHex(_serverSession.Key).ToByteArray()),
                        Encoding.UTF8.GetBytes("Pair-Setup-Encrypt-Salt"),
                        Encoding.UTF8.GetBytes("Pair-Setup-Encrypt-Info"), 32);


                    var decrypt = AeadAlgorithm.ChaCha20Poly1305.Decrypt(
                        Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdfEncKey, KeyBlobFormat.RawSymmetricKey), nonce,
                        new byte[0], iOsEncryptedData, out var output);
                    var responseTlv = new Tlv();
                    responseTlv.AddType(Constants.State, 6);
                    if (!decrypt)
                    {
                        responseTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                        return new PairSetupReturn
                        {
                            State = 5,
                            TlvData = responseTlv,
                            Ok = false
                        };
                    }

                    var subData = TlvParser.Parse(output);

                    byte[] username = subData.GetType(Constants.Identifier);
                    byte[] ltpk = subData.GetType(Constants.PublicKey);
                    byte[] proof = subData.GetType(Constants.Signature);


                    var okm = hdkf.DeriveBytes(
                        SharedSecret.Import(SrpInteger.FromHex(_serverSession.Key).ToByteArray()),
                        Encoding.UTF8.GetBytes("Pair-Setup-Controller-Sign-Salt"),
                        Encoding.UTF8.GetBytes("Pair-Setup-Controller-Sign-Info"), 32);

                    var completeData = okm.Concat(username).Concat(ltpk).ToArray();


                    if (!SignatureAlgorithm.Ed25519.Verify(
                        PublicKey.Import(SignatureAlgorithm.Ed25519, ltpk, KeyBlobFormat.RawPublicKey), completeData,
                        proof))
                    {
                        var errorTlv = new Tlv();
                        errorTlv.AddType(Constants.Error, ErrorCodes.Authentication);
                        return new PairSetupReturn
                        {
                            State = 5,
                            TlvData = errorTlv,
                            Ok = false
                        };
                    }

                    var accessory = hdkf.DeriveBytes(
                        SharedSecret.Import(SrpInteger.FromHex(_serverSession.Key).ToByteArray()),
                        Encoding.UTF8.GetBytes("Pair-Setup-Accessory-Sign-Salt"),
                        Encoding.UTF8.GetBytes("Pair-Setup-Accessory-Sign-Info"), 32);


                    var seed = new byte[32];
                    RandomNumberGenerator.Create().GetBytes(seed);
                    Chaos.NaCl.Ed25519.KeyPairFromSeed(out var accessoryLtpk, out var accessoryLtsk, seed);

                    var serverUsername = Encoding.UTF8.GetBytes(HapControllerServer.HapControllerId);
                    var material = accessory.Concat(serverUsername).Concat(accessoryLtpk).ToArray();

                    var signature = Chaos.NaCl.Ed25519.Sign(material, accessoryLtsk);


                    var encoder = new Tlv();
                    encoder.AddType(Constants.Identifier, serverUsername);
                    encoder.AddType(Constants.PublicKey, accessoryLtpk);
                    encoder.AddType(Constants.Signature, signature);

                    var plaintext = TlvParser.Serialize(encoder);

                    var nonce6 = new Nonce(zeros, Encoding.UTF8.GetBytes("PS-Msg06"));

                    var encryptedOutput = AeadAlgorithm.ChaCha20Poly1305.Encrypt(
                        Key.Import(AeadAlgorithm.ChaCha20Poly1305, hkdfEncKey, KeyBlobFormat.RawSymmetricKey), nonce6,
                        new byte[0], plaintext);

                    responseTlv.AddType(Constants.EncryptedData, encryptedOutput);

                    return new PairSetupReturn
                    {
                        State = 5,
                        TlvData = responseTlv,
                        Ok = true,
                        Ltsk = ByteArrayToString(accessoryLtsk),
                        Ltpk = ByteArrayToString(ltpk)
                    };
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{e}, Could not exchange request");
                    throw;
                }
            }

            return null;
        }

    }
}
