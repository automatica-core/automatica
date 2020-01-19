using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.Controllers.Ed25519;
using P3.Driver.HomeKit.Hap.Controllers.Srp;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;
using SecureRemotePassword;
using Xunit;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace P3.Driver.HomeKit.UnitTests.Setup
{
    public class FixedSaltGenerator : ISrpGenerator
    {
        public const string ExpectedPrivateKey =
            "381409203e3082404854afdf9eb60bbc89b36d64de12216376390751915b109ca40bf18a29bc3bc9ae3660aef6f438487aa2ef2596cf991bd3d289e99dd50428";

        public const string ExpectedVerifier =
            "abf4cdcd2f096102b8011bf92eaeb3f3d59a64339a023615662dc241321ec0e1ca2569f79062e127a5c4ac95d1d6738bf4e1bbf69df6e9c12c4f3855eca4b3922b7dc653cbf3878604c223ba9d9a48de0114d803fa421b91944b2509a5c8419463b5c0b28e87c5dd1532249e56f97f591cc073ae7883d28e1e0dbb8ee08d1491ade1c1c929ba2a80ea45385a15e2f9536cd46ee0c2179185f3f54c66efc5bf48ccce3f70cf45d39b68290af98fa15fbf19c09ce873c9185fffcdca977e4fea2b26ca6944601acca8a29dfdebd800814aed563c10725b022708b0e7760e468507eefda1965d3b58f5f18ae52c814d1c1dda817274ccd7df127471d780922681961f518154e12cf068afe909b9f81118f129e2bd1fbcae8b58f64e8f65ce9069f85b1e26f16c75dbb9801bd71157663df785d31e713c91bee74bcb26e757c2c0ee0c714ff80904d0338bbc0370dab0e01d62362fbcf3363e6cbd758c7257edaa93b42294e351c3f06b7e307486f7132e9b122983e0bf8573b751ba38a3a1f4a3ad";

        public static SrpEphemeral ExpectedServerEphemeral = new SrpEphemeral
        {
            Secret = "7b215952dad95cbf8f7331586bf546efac2b4bf696dac9a424b082da37f0575f",
            Public = "18ed6bbc1b4338031679acbcd7f6aa4622266e2c1c92432f8c446e0197333a08f0fd0acbd77cee8aeccbf0fe5ad9f526476acb0a26e928620451152f1f5e0c19bf788c9bded644868354256d0ae5a0736b8b5c3a8e8ac7fad4b74b56732af5098254e9acba634a29a0f4d65d75bf9993f5590fa61cd336299703d121632032b1f95fb83cc1d6fa9c6b994dbff8ef68bf269aa62ded2efa1556988a52992ccbfd41dfbfe234490d7b5d310816bd046280e51740c8132d71677a73297080639bf4f39430b166539a07e045048a2a5d8c4f285deee8ea5fd1aa58432b681e519522d3491e528bb15b4f1ea39fb2c08cfa4bfdc11095e7fb1585018e75bb2a7e3965287d49a816d51852432fc093d884aabec2126bb6febd08fa18857e16bff8b05bf6c03b7b212e5917ea196444cc0bd2cd54fc2bbd25562b89e0f4c90aab68fa7ba999f2082683430d9bdbd01876b72dc3851d74ec973c710d19fc694100342c8139cdc1aad1155be550432d3abf3944643a6bdaa8b20615f7cbe1bc00bb8334b9"
        };

        public const string ExpectedM2Proof =
            "847786bbdb27e466f3533fa2016f5eaff57ffddc99c55d97d8cc634bcbd95c9c2ba0167e717ecd9fc19aa8b03987a9b0a1b3b676b8e33ce8d097a05bffbd4a24";

        public const string ExpectedClientProof =
            "cd09aaf139460ef72317848478c1bee19464b149f3145ecf67a07a6f8829443c3a13d1c5ea4e45cfba291fd7a8d1c8fdc088d709b87882947a3b6dee5738409d";
        public const string ExpectedClientPubKey = "a080de13894d7d0f183472a4ac1ac1be4efeea83685bd9e318e7e33516ee2637469e35cb3695d2885b9d000a053d8ebac30fb82348915f941e54b4386b2d6e4d7b99b91b94ca937a2026651f9d28b59596e7d5078b220050dc22e4962462f5d59b801c1854ea0238cfecbc3521580597d5abf5035bddf295caf55d528898e1c835d624f3d3e12a62f9ff1363c86b0e5d28cfdd16aed5bb6d13d93c6ab5985f0d5f5325a6c3d22db574e060694d6a373a98335ee0e72c036a413ec2374e7d235e1cb71953bf89ba9712a07d5598323ece6d64f7032add9d7f8787dce3abdadd06d2cad3b6df6376f0dc617c421fc918ad04277f91c722f92e264f2cea5d4af18bfa268350586190f3cf58f97ea5153970aa3ab48c636ddf043966a3183bc0514616f03bd5e9559cee68208c8e87a5ee5a223f9ad69fa96034053444dfc09280a0068995659007b0ffe0428c833a69c089d1299608575b6c5dc1a0b10c422b6b39574dfa7206f6222d99d822785efa2b6f597377b96107ac59c6722dfdb6f20861";

        private readonly ISrpGenerator _srpGenerator = new SrpGenerator(SrpParameters.Create3072<SHA512>());
        public byte[] GenerateSalt(int length)
        {
            return SrpInteger.FromHex("deff10eeac3d2c5cf370a2dae3310a4f").ToByteArray();
        }

        public string DerivePrivateKey(string salt, string userName, string password)
        {
            var privateKey = _srpGenerator.DerivePrivateKey(SrpInteger.FromByteArray(GenerateSalt(16)).ToHex(), "Pair-Setup", "031-45-154");
            
            Assert.Equal(ExpectedPrivateKey, privateKey);
            
            return privateKey;
        }

        public string DeriveVerifier(string privateKey)
        {
            var verifier =  _srpGenerator.DeriveVerifier(privateKey);

            Assert.Equal(ExpectedVerifier, verifier);
            return verifier;
        }

        public SrpEphemeral GenerateServerEphemeral(string verifier)
        {
            return ExpectedServerEphemeral;
        }

        public SrpSession DeriveServerSession(string serverEphemeralSecret, string clientPublicEphemeral, string salt, string userName,
            string verifier, string clientSessionProof)
        {
            var session = _srpGenerator.DeriveServerSession(serverEphemeralSecret, clientPublicEphemeral, salt, userName,
                verifier, clientSessionProof);

            Assert.Equal(ExpectedM2Proof, session.Proof);

            return session;
        }
    }

    internal class FixedEd25519KeyGenerator : IEd25519KeyGenerator
    {
        public KeyPair GenerateNewPair()
        {
            return new KeyPair(Automatica.Core.Driver.Utility.Utils.StringToByteArray("83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded"),
                Automatica.Core.Driver.Utility.Utils.StringToByteArray("3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded"));
        }
    }

    public class PairSetupTests
    {
        public PairSetupTests()
        {
            HapControllerServer.HapControllerId = "FC:22:3D:E3:CE:F2";
        }

        [Fact]
        public void M1SetupTest()
        {
            var fixedSrpGenerator = new FixedSaltGenerator();
            const string expectedTlvHexString =
                "0601020210deff10eeac3d2c5cf370a2dae3310a4f03ff18ed6bbc1b4338031679acbcd7f6aa4622266e2c1c92432f8c446e0197333a08f0fd0acbd77cee8aeccbf0fe5ad9f526476acb0a26e928620451152f1f5e0c19bf788c9bded644868354256d0ae5a0736b8b5c3a8e8ac7fad4b74b56732af5098254e9acba634a29a0f4d65d75bf9993f5590fa61cd336299703d121632032b1f95fb83cc1d6fa9c6b994dbff8ef68bf269aa62ded2efa1556988a52992ccbfd41dfbfe234490d7b5d310816bd046280e51740c8132d71677a73297080639bf4f39430b166539a07e045048a2a5d8c4f285deee8ea5fd1aa58432b681e519522d3491e528bb15b4f1ea39fb2c08cfa4bfdc11095e7fb1585018e75bb2a7e39038165287d49a816d51852432fc093d884aabec2126bb6febd08fa18857e16bff8b05bf6c03b7b212e5917ea196444cc0bd2cd54fc2bbd25562b89e0f4c90aab68fa7ba999f2082683430d9bdbd01876b72dc3851d74ec973c710d19fc694100342c8139cdc1aad1155be550432d3abf3944643a6bdaa8b20615f7cbe1bc00bb8334b9";
            
            var tlv = TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray("000100060101"));

            var setupController = new PairSetupController(NullLogger.Instance, "031-45-154")
            {
                SrpGenerator = fixedSrpGenerator
            };

            fixedSrpGenerator.DeriveVerifier(fixedSrpGenerator.DerivePrivateKey("", "", ""));
            
            var session = new ConnectionSession();
            var ret = setupController.Post(tlv, session);

            var expectedTlvResponse =
                TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(expectedTlvHexString));

            Assert.Equal(expectedTlvResponse.GetType(Constants.State), ret.TlvData.GetType(Constants.State));
            Assert.Equal(expectedTlvResponse.GetType(Constants.Salt), ret.TlvData.GetType(Constants.Salt));
            Assert.Equal(expectedTlvResponse.GetType(Constants.PublicKey), ret.TlvData.GetType(Constants.PublicKey));
        }

        [Fact]
        public void M2SetupTest()
        {
            var fixedSrpGenerator = new FixedSaltGenerator();
            var expectedTlvHexReponse =
                "0601040440847786bbdb27e466f3533fa2016f5eaff57ffddc99c55d97d8cc634bcbd95c9c2ba0167e717ecd9fc19aa8b03987a9b0a1b3b676b8e33ce8d097a05bffbd4a24";
            var inputData =
                "06010303ffa080de13894d7d0f183472a4ac1ac1be4efeea83685bd9e318e7e33516ee2637469e35cb3695d2885b9d000a053d8ebac30fb82348915f941e54b4386b2d6e4d7b99b91b94ca937a2026651f9d28b59596e7d5078b220050dc22e4962462f5d59b801c1854ea0238cfecbc3521580597d5abf5035bddf295caf55d528898e1c835d624f3d3e12a62f9ff1363c86b0e5d28cfdd16aed5bb6d13d93c6ab5985f0d5f5325a6c3d22db574e060694d6a373a98335ee0e72c036a413ec2374e7d235e1cb71953bf89ba9712a07d5598323ece6d64f7032add9d7f8787dce3abdadd06d2cad3b6df6376f0dc617c421fc918ad04277f91c722f92e264f2cea5d4af103818bfa268350586190f3cf58f97ea5153970aa3ab48c636ddf043966a3183bc0514616f03bd5e9559cee68208c8e87a5ee5a223f9ad69fa96034053444dfc09280a0068995659007b0ffe0428c833a69c089d1299608575b6c5dc1a0b10c422b6b39574dfa7206f6222d99d822785efa2b6f597377b96107ac59c6722dfdb6f208610440cd09aaf139460ef72317848478c1bee19464b149f3145ecf67a07a6f8829443c3a13d1c5ea4e45cfba291fd7a8d1c8fdc088d709b87882947a3b6dee5738409d";
            var tlv = TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(inputData));

            var session = new ConnectionSession
            {
                Verifier = FixedSaltGenerator.ExpectedVerifier,
                ServerEphemeral = fixedSrpGenerator.GenerateServerEphemeral(FixedSaltGenerator.ExpectedVerifier)
            };

            var setupController = new PairSetupController(NullLogger.Instance, "031-45-154")
            {
                SrpGenerator = fixedSrpGenerator
            };

            var ret = setupController.Post(tlv, session);

            var expectedTlvResponse =
                TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(expectedTlvHexReponse));


            Assert.Equal(expectedTlvResponse.GetType(Constants.State), ret.TlvData.GetType(Constants.State));
            Assert.Equal(expectedTlvResponse.GetType(Constants.Proof), ret.TlvData.GetType(Constants.Proof));
        }

        [Fact]
        public void M5SetupTest()
        {
            var clientPubKey = FixedSaltGenerator.ExpectedClientPubKey;
            var clientProof = FixedSaltGenerator.ExpectedClientProof;
            var inputData =
                "059ac2e1aa3fef6c4bc067cd9d26a05b53fa6ad29f953af80c90968757a18fcb270118987a5c26e50f2f8e274750584929d5d5a176a62af5ef0d4002ed6333ee75b0f75d0ee4dbef345f6ae15345cfdcb97e3300d18bf8f827701f6dfcc6cc213dff5ae1cffa03235853df30d8eb6ceec068dc64e9ef8049ce3d9396bbd32c7b2e329005023cf06ed8a60e3e51491b7f7193a807f1f244369a5ee9fd060105";
            var expectedTlvHexReponse =
                "0601060587b0e4a7aa382866e824db4159cc0ecd24900136be360fd6ae64481bf799b1ecc918ecb887285a8dcd5b6f90aa410253451fd1bc638cae716cbab166f763108633555a6bfa311662986ea2d1eb83b76d195724b9bbddf7902dfd3a16e1bbd5bb8e5a5a6e3a56e8eec21c3f06ed2cce21ea17426407d0a1b35c9d692f5a5ee15ecb7ac662ade25ab7";

            var tlv = TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(inputData));

            var fixedSrpGenerator = new FixedSaltGenerator();

            var session = new ConnectionSession
            {
                Salt = fixedSrpGenerator.GenerateSalt(16),
                Verifier = FixedSaltGenerator.ExpectedVerifier,
                ServerEphemeral = fixedSrpGenerator.GenerateServerEphemeral(FixedSaltGenerator.ExpectedVerifier)
            };
            session.ServerSession = fixedSrpGenerator.DeriveServerSession(session.ServerEphemeral.Secret, clientPubKey, SrpInteger.FromByteArray(session.Salt).ToHex(), "Pair-Setup", session.Verifier, clientProof);

            var setupController = new PairSetupController(NullLogger.Instance, "031-45-154")
            {
                SrpGenerator = fixedSrpGenerator, KeyGenerator = new FixedEd25519KeyGenerator()
            };

            var ret = setupController.Post(tlv, session);
           
            var expectedTlvResponse =
                TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(expectedTlvHexReponse));

            var decryptedPayload = TlvParser.Parse(Automatica.Core.Driver.Utility.Utils.StringToByteArray(
                "011146433a32323a33443a45333a43453a4632032083e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded0a402ac3063b1d193c4d365c546019c51cac8299a23a99c9f8eb2348c46af24b185a53f1f75a92d57193bdeb4dfbc47ccadccc4f9f4e3dc8191eef364d15cd921207"));
            var raw = setupController.HandlePairSetupM5Raw(session, out _);

            Assert.Equal(decryptedPayload.GetType(Constants.Identifier), raw.GetType(Constants.Identifier));
            Assert.Equal(decryptedPayload.GetType(Constants.PublicKey), raw.GetType(Constants.PublicKey));
            Assert.Equal(decryptedPayload.GetType(Constants.Signature), raw.GetType(Constants.Signature));

            var expectedEncryptedData =
                Automatica.Core.Driver.Utility.Utils.ByteArrayToString(expectedTlvResponse.GetType(Constants.EncryptedData).AsSpan());
            var retEncryptedData =
                Automatica.Core.Driver.Utility.Utils.ByteArrayToString(ret.TlvData.GetType(Constants.EncryptedData).AsSpan());

            Assert.Equal(expectedEncryptedData, retEncryptedData);
            Assert.Equal(expectedTlvResponse.GetType(Constants.State), ret.TlvData.GetType(Constants.State));
            Assert.Equal(expectedTlvResponse.GetType(Constants.EncryptedData), ret.TlvData.GetType(Constants.EncryptedData));

            var expectedHttpResponse =
                "485454502f312e3120323030204f4b0d0a436f6e74656e742d547970653a206170706c69636174696f6e2f70616972696e672b746c76380d0a446174653a2053756e2c203139204a616e20323032302031353a33313a313320474d540d0a436f6e6e656374696f6e3a206b6565702d616c6976650d0a5472616e736665722d456e636f64696e673a206368756e6b65640d0a0d0a38630d0a0601060587b0e4a7aa382866e824db4159cc0ecd24900136be360fd6ae64481bf799b1ecc918ecb887285a8dcd5b6f90aa410253451fd1bc638cae716cbab166f763108633555a6bfa311662986ea2d1eb83b76d195724b9bbddf7902dfd3a16e1bbd5bb8e5a5a6e3a56e8eec21c3f06ed2cce21ea17426407d0a1b35c9d692f5a5ee15ecb7ac662ade25ab70d0a300d0a0d0a";
            var expectedHttpResponseArray =
                Automatica.Core.Driver.Utility.Utils.StringToByteArray(expectedHttpResponse);

            var requestTime = new DateTime(2020, 01, 19, 15, 31, 13, DateTimeKind.Utc);
            var httpResponse =
                HttpServerConnection.GetHttpResponse("HTTP/1.1", PairSetupReturn.ContentType, TlvParser.Serialize(ret.TlvData), requestTime);

            Assert.Equal(expectedHttpResponseArray, httpResponse);
        } 
    }
}