using P3.Driver.Matter.Commissioning;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    public class ProtocolTests
    {
        [Fact]
        public void Tlv_round_trip()
        {
            var bytes = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutUInt(1, 42);
                w.PutInt(2, -5);
                w.PutBool(3, true);
                w.PutString(4, "hello");
                w.PutBytes(5, new byte[] { 1, 2, 3 });
                w.PutNull(6);
                w.StartArray(7);
                w.PutUInt(null, 0x1_0000_0000);
                w.PutUInt(null, 7);
                w.EndContainer();
                w.EndContainer();
            });

            var root = TlvReader.Decode(bytes);
            Assert.Equal(42ul, root.GetUInt(1));
            Assert.Equal(-5, root.GetInt(2));
            Assert.True(root.GetBool(3));
            Assert.Equal("hello", root.GetString(4));
            Assert.Equal(new byte[] { 1, 2, 3 }, root.GetBytes(5));
            Assert.True(root.Require(6).IsNull);
            Assert.Equal(new ulong[] { 0x1_0000_0000, 7 }, root.Require(7).Children.Select(c => c.AsUInt()));
        }

        [Fact]
        public void Tlv_uses_the_smallest_integer_encoding()
        {
            // anonymous uint8 = control 0x04
            Assert.Equal(new byte[] { 0x04, 0x2A }, TlvWriter.Encode(w => w.PutUInt(null, 42)));
            // context tag 1, uint16 = control 0x25
            Assert.Equal(new byte[] { 0x25, 0x01, 0x00, 0x01 }, TlvWriter.Encode(w => w.PutUInt(1, 256)));
        }

        [Fact]
        public void Manual_pairing_code_matches_the_spec_example()
        {
            Assert.Equal("34970112332", SetupPayload.ManualPairingCode(20202021, 3840));
            Assert.Equal("3497-011-2332", SetupPayload.FormatManualCode("34970112332"));
        }

        [Fact]
        public void Qr_code_matches_the_sdk_example()
        {
            Assert.Equal("MT:-24J042C00KA0648G00", SetupPayload.QrCode(0xFFF1, 0x8001, 20202021, 3840, SetupPayload.DiscoveryCapabilityBle));
        }

        [Fact]
        public void Generated_passcodes_are_valid()
        {
            for (var i = 0; i < 100; i++)
            {
                Assert.True(SetupPayload.IsValidPasscode(SetupPayload.GeneratePasscode()));
            }

            Assert.False(SetupPayload.IsValidPasscode(11111111));
            Assert.False(SetupPayload.IsValidPasscode(12345678));
        }

        [Fact]
        public void Spake2_prover_and_verifier_agree()
        {
            var salt = MatterCrypto.RandomBytes(16);
            var (w0, w1) = Spake2Plus.ComputeW0W1(20202021, salt, 1000);
            var (vw0, l) = Spake2Plus.ComputeVerifier(20202021, salt, 1000);
            Assert.Equal(w0, vw0);

            var context = MatterCrypto.Sha256("test"u8.ToArray());
            var prover = new Spake2PlusProver(context, w0, w1);
            var verifier = new Spake2PlusVerifier(context, vw0, l);
            verifier.ProcessX(prover.X);
            prover.ProcessY(verifier.Y);
            Assert.True(prover.VerifyCB(verifier.CB));
            Assert.True(verifier.VerifyCA(prover.CA));
            Assert.Equal(prover.Ke, verifier.Ke);
        }

        [Fact]
        public void Spake2_rejects_a_wrong_passcode()
        {
            var salt = MatterCrypto.RandomBytes(16);
            var (w0, w1) = Spake2Plus.ComputeW0W1(11223344, salt, 1000);
            var (vw0, l) = Spake2Plus.ComputeVerifier(20202021, salt, 1000);
            var context = MatterCrypto.Sha256("test"u8.ToArray());
            var prover = new Spake2PlusProver(context, w0, w1);
            var verifier = new Spake2PlusVerifier(context, vw0, l);
            verifier.ProcessX(prover.X);
            prover.ProcessY(verifier.Y);
            Assert.False(prover.VerifyCB(verifier.CB));
            Assert.False(verifier.VerifyCA(prover.CA));
        }

        [Fact]
        public void Aes_ccm_round_trip_and_tamper_detection()
        {
            var key = MatterCrypto.RandomBytes(16);
            var nonce = MessageHeader.BuildNonce(0, 12345, 0x1122334455667788);
            var aad = new byte[] { 9, 9, 9 };
            var cipher = MatterCrypto.AeadEncrypt(key, nonce, "payload"u8, aad);
            Assert.Equal("payload"u8.ToArray(), MatterCrypto.AeadDecrypt(key, nonce, cipher, aad));

            cipher[0] ^= 1;
            Assert.ThrowsAny<Exception>(() => MatterCrypto.AeadDecrypt(key, nonce, cipher, aad));
        }

        [Fact]
        public void Message_header_round_trip()
        {
            var header = new MessageHeader { SessionId = 0x1234, MessageCounter = 0xAABBCCDD, SourceNodeId = 0x0102030405060708 };
            var decoded = MessageHeader.Decode(header.Encode(), out var length);
            Assert.Equal(header.Encode().Length, length);
            Assert.Equal(header.SessionId, decoded.SessionId);
            Assert.Equal(header.MessageCounter, decoded.MessageCounter);
            Assert.Equal(header.SourceNodeId, decoded.SourceNodeId);

            var payload = new PayloadHeader { Opcode = 0x08, ExchangeId = 77, ProtocolId = ProtocolId.InteractionModel, IsInitiator = true, NeedsAck = true, AckedMessageCounter = 5 };
            var p = PayloadHeader.Decode(payload.Encode(), out _);
            Assert.True(p.IsInitiator);
            Assert.True(p.NeedsAck);
            Assert.Equal(5u, p.AckedMessageCounter);
            Assert.Equal((ushort)77, p.ExchangeId);
        }

        [Fact]
        public void Reception_state_rejects_duplicates()
        {
            var state = new ReceptionState();
            Assert.True(state.Accept(100));
            Assert.False(state.Accept(100));
            Assert.True(state.Accept(102));
            Assert.True(state.Accept(101));
            Assert.False(state.Accept(101));
        }
    }
}
