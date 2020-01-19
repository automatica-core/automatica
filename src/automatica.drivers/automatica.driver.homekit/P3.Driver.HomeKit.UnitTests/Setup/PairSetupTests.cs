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
            "c915cc83e4b1e67151edcb03ab5cbcfa7bb8c6e4866472e2f13fb0b6e13b15a1209c4271bba964858ca799c16005489cecd49d8704e67949069de6016a832966";

        public const string ExpectedClientProof =
            "83709658ff56231a1b18cc5471996da7fa32a0b75b89234cb85e142b04725cdf00e44025f75fde79e19af3fe40252219a5d12cb9b357194600f7a7393304fbbe";
        public const string ExpectedClientPubKey = "e62a359cc393882a48266619348ecdde3e7bba8d4094477d62cfe289574d1603424dd2fe61234226058472f05c5e47a79f821145c857862aeed353de5c13a1b71c6291ed85a817c223374ceb450c82f20360b8bee85a6b09beff5e9c015a5ebeb465b11cc9de4b8c9f117dae9fd80b4fde39d8f4c30620f3b2ac466bf617882ac566abe45c52f415ee25d32c45723395d68050928dc39dc55c7981d95c07427b3d58e4f3c48623e289b5d7119f00fcd056eeb0e9b3a99c7d218beecc7b49ca7ac54af2afc7dbcfc9b557169baaa863b63e7ef08ed45b3162c4d4e68e318d615139c1ff410ad54116f75753393e07e08148bae370e7c2866666ac004a0d58c49eac8101c38bc2b6b60874a3092c073a98fe68b4bf025c0d8500be93aadd9cb79ea0bbe657d711d8926f911e1163bc62bb5da36c3cafddcabdee5b3a5cc4bf702d3f2227ff5e28d3c98faa998c2e9e862fa6833e0695abcf699ae73dc00d1e3151db3653fbe5ad1c196e64e817b05701750e9fd7f3f2e021ebe3b6dcb8611679a1";

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
            return new KeyPair(Automatica.Core.Driver.Utility.Utils.StringToByteArray("5dd92eb7ce51dbb500a572b2f1508a5b95a566ea0210fc50bc6d47dd8337e8ff"),
                Automatica.Core.Driver.Utility.Utils.StringToByteArray("3f3c5ad7a5d971aabae3c355007cbde4d4fa05983881c7413d09b6e85cefef225dd92eb7ce51dbb500a572b2f1508a5b95a566ea0210fc50bc6d47dd8337e8ff"));
        }
    }

    public class PairSetupTests
    {
        public PairSetupTests()
        {
            HapControllerServer.HapControllerId = "BC:22:3D:E3:CE:F2";
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
                "0601040440c915cc83e4b1e67151edcb03ab5cbcfa7bb8c6e4866472e2f13fb0b6e13b15a1209c4271bba964858ca799c16005489cecd49d8704e67949069de6016a832966";
            var inputData =
                "06010303ffe62a359cc393882a48266619348ecdde3e7bba8d4094477d62cfe289574d1603424dd2fe61234226058472f05c5e47a79f821145c857862aeed353de5c13a1b71c6291ed85a817c223374ceb450c82f20360b8bee85a6b09beff5e9c015a5ebeb465b11cc9de4b8c9f117dae9fd80b4fde39d8f4c30620f3b2ac466bf617882ac566abe45c52f415ee25d32c45723395d68050928dc39dc55c7981d95c07427b3d58e4f3c48623e289b5d7119f00fcd056eeb0e9b3a99c7d218beecc7b49ca7ac54af2afc7dbcfc9b557169baaa863b63e7ef08ed45b3162c4d4e68e318d615139c1ff410ad54116f75753393e07e08148bae370e7c2866666ac004a0d58c403819eac8101c38bc2b6b60874a3092c073a98fe68b4bf025c0d8500be93aadd9cb79ea0bbe657d711d8926f911e1163bc62bb5da36c3cafddcabdee5b3a5cc4bf702d3f2227ff5e28d3c98faa998c2e9e862fa6833e0695abcf699ae73dc00d1e3151db3653fbe5ad1c196e64e817b05701750e9fd7f3f2e021ebe3b6dcb8611679a1044083709658ff56231a1b18cc5471996da7fa32a0b75b89234cb85e142b04725cdf00e44025f75fde79e19af3fe40252219a5d12cb9b357194600f7a7393304fbbe";
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
                "059af9b2e85eeadbe6f1fad04fba763d23761ca239a87e7de936edc058cf1e4c515cf05a5bc15667f2022eeb3115c3a1a106cfe551dc85bba72bbba934fb5ad11e2ab52962fe147e265bc07a03918c41916a27699b6c9615603511fc7a1122c10b5dc591eb2a478f5a3d45272d465ca5cd77b51ec399277499499ba89c332049f72b427ae57124b13aec77d5702a88fb3f45afd7eac571675ae7f4fe060105";
            var expectedTlvHexReponse =
                "060106058794579cfbde9949880df9722e0c47dcaaff9a295e6b45d4722b843ce14bfe1c23d4e9f292d990dc4a1384114cf91da47270496d4154d5e0b67cbce2814660afe158f5ae449c80437ea3acc92d14b2bf90eaabd79296fab0e636ad12a78b4c7d45edb01f3deb18ffbcc75af4141ef3dcc1687e9b546d400389fe4efdd6bab24dd8f9cb89261868b4";


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
                "011142433a32323a33443a45333a43453a463203205dd92eb7ce51dbb500a572b2f1508a5b95a566ea0210fc50bc6d47dd8337e8ff0a40e3cef76c54ae51533f02a42e86e244cc4632e9bc906bb186388870f31948f0306d6216ab426df7837afa24adcf533e95acf766223ceac48fb873f928bce54500"));
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
        }
    }
}