using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using P3.Driver.HomeKit.Hap.Controllers.Ed25519;
using P3.Driver.HomeKit.Hap.Controllers.Srp;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;
using SecureRemotePassword;

namespace P3.Driver.HomeKit.Hap.Controllers
{
    internal sealed class PairSetupReturn
    {
        public Tlv TlvData { get; set; }
        public int State { get; set; }
        public bool Ok { get; set; } = false;
        public const string ContentType = "application/pairing+tlv8";

        public string Ltsk { get; set; }
        public string Ltpk { get; set; }

    }

    internal sealed class PairSetupController : BaseController
    {
        private readonly ILogger _logger; //todo: log more data
        private static string _code;

        public ISrpGenerator SrpGenerator { get; internal set; } = new SrpGenerator(SrpParameters.Create3072<SHA512>());
        public IEd25519KeyGenerator KeyGenerator { get; internal set; } = new Ed25519KeyGenerator();

        private const string Username = "Pair-Setup";

        public PairSetupController(ILogger logger, string code)
        {
            _logger = logger;
            _code = code;
        }

        public PairSetupReturn Post(Tlv parts, ConnectionSession session)
        {

            var state = parts.GetTypeAsInt(Constants.State);

            if (session.Salt == null)
            {
                session.Salt = SrpGenerator.GenerateSalt(16);
            }

            _logger.LogDebug($"State is {state}");

            if (state == 1) //srp sign up
            {
                var saltInt = SrpInteger.FromByteArray(session.Salt);
                
                var privateKey = SrpGenerator.DerivePrivateKey(saltInt.ToHex(), Username, _code);
                session.Verifier = SrpGenerator.DeriveVerifier(privateKey);

                session.ServerEphemeral = SrpGenerator.GenerateServerEphemeral(session.Verifier);
               
                var responseTlv = new Tlv();
                responseTlv.AddType(Constants.State, 2);
                responseTlv.AddType(Constants.PublicKey, StringToByteArray(session.ServerEphemeral.Public));
                responseTlv.AddType(Constants.Salt, session.Salt);

                _logger.LogDebug($"return salt {saltInt.ToHex()}, pub {session.ServerEphemeral.Public} and state 2");

                return new PairSetupReturn
                {
                    State = 1,
                    TlvData = responseTlv,
                    Ok = true
                };
            }

            if (state == 3) //srp authenticate
            {
                _logger.LogDebug("Pair Setup Step 3/5");
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
                    session.ServerSession = SrpGenerator.DeriveServerSession(session.ServerEphemeral.Secret, iOsPublicKey.ToHex(), SrpInteger.FromByteArray(session.Salt).ToHex(), Username, session.Verifier,
                        iOsProof.ToHex());
                    _logger.LogInformation("Verification was successful. Generating Server Proof (M2)");

                    responseTlv.AddType(Constants.Proof, StringToByteArray(session.ServerSession.Proof));


                    _logger.LogDebug($"return proof {session.ServerSession.Proof}, secret {session.ServerEphemeral.Secret} and state 4");
                }
                catch(Exception e)
                {
                    ok = false;
                    _logger.LogError(e,"Verification failed as iOS provided code was incorrect");
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
                return HandlePairSetupM5(parts, session);
            }

            return null;
        }

        internal Tlv HandlePairSetupM5Raw(ConnectionSession session, out KeyPair keyPair)
        {
            var hdkf = new HkdfSha512();
            var accessory = hdkf.DeriveBytes(
                SharedSecret.Import(SrpInteger.FromHex(session.ServerSession.Key).ToByteArray()),
                Encoding.UTF8.GetBytes("Pair-Setup-Accessory-Sign-Salt"),
                Encoding.UTF8.GetBytes("Pair-Setup-Accessory-Sign-Info"), 32);

            keyPair = KeyGenerator.GenerateNewPair();

            var serverUsername = Encoding.UTF8.GetBytes(HapControllerServer.HapControllerId);
            var material = accessory.Concat(serverUsername).Concat(keyPair.PublicKey).ToArray();

            var signature = Chaos.NaCl.Ed25519.Sign(material, keyPair.PrivateKey);


            var encoder = new Tlv();
            encoder.AddType(Constants.Identifier, serverUsername);
            encoder.AddType(Constants.PublicKey, keyPair.PublicKey);
            encoder.AddType(Constants.Signature, signature);

            return encoder;
        }

        internal PairSetupReturn HandlePairSetupM5(Tlv parts, ConnectionSession session)
        {
            _logger.LogDebug("Pair Setup Step 5/5");
            _logger.LogDebug("Exchange Response");

            try
            {
                var iOsEncryptedData = parts.GetType(Constants.EncryptedData).AsSpan(); // A 
                var zeros = new byte[] { 0, 0, 0, 0 };
                var nonce = new Nonce(zeros, Encoding.UTF8.GetBytes("PS-Msg05"));
                var hdkf = new HkdfSha512();
                var hkdfEncKey = hdkf.DeriveBytes(
                    SharedSecret.Import(SrpInteger.FromHex(session.ServerSession.Key).ToByteArray()),
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
                    SharedSecret.Import(SrpInteger.FromHex(session.ServerSession.Key).ToByteArray()),
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

                var m5Response = HandlePairSetupM5Raw(session, out var keyPair);
                var plaintext = TlvParser.Serialize(m5Response);

                _logger.LogDebug($"Decrypted payload {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(plaintext.AsSpan())}");


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
                    Ltsk = ByteArrayToString(keyPair.PrivateKey),
                    Ltpk = ByteArrayToString(ltpk)
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{e}, Could not exchange request");
                throw;
            }
        }
    }
}
