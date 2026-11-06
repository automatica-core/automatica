using Microsoft.Extensions.Logging;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.SecureChannel
{
    /// <summary>The verifier of an open commissioning window.</summary>
    public sealed class PasscodeVerifier
    {
        public byte[] W0 { get; init; }
        public byte[] L { get; init; }
        public byte[] Salt { get; init; }
        public int Iterations { get; init; }

        public static PasscodeVerifier FromPasscode(uint passcode, byte[] salt, int iterations)
        {
            var (w0, l) = Spake2Plus.ComputeVerifier(passcode, salt, iterations);
            return new PasscodeVerifier { W0 = w0, L = l, Salt = salt, Iterations = iterations };
        }

        /// <summary>From the 97 byte w0 || L blob of AdministratorCommissioning.OpenCommissioningWindow.</summary>
        public static PasscodeVerifier FromSerialized(byte[] verifier, byte[] salt, int iterations)
        {
            if (verifier.Length != 32 + 65)
            {
                throw new MatterProtocolException("PAKE verifier must be 97 bytes");
            }

            return new PasscodeVerifier { W0 = verifier[..32], L = verifier[32..], Salt = salt, Iterations = iterations };
        }
    }

    public interface IPaseContext
    {
        /// <summary>The verifier while a commissioning window is open, otherwise null.</summary>
        PasscodeVerifier CurrentVerifier { get; }

        void OnPaseEstablished(SecureSession session);

        void OnPaseFailed();
    }

    /// <summary>The PASE responder (spec 4.14.1): PBKDFParam exchange followed by SPAKE2+.</summary>
    public sealed class PaseResponder
    {
        private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);
        private readonly SessionManager _sessions;
        private readonly IPaseContext _context;
        private readonly ILogger _logger;

        public PaseResponder(SessionManager sessions, IPaseContext context, ILogger logger)
        {
            _sessions = sessions;
            _context = context;
            _logger = logger;
        }

        public async Task HandleAsync(MessageExchange exchange, MatterMessage first)
        {
            var verifier = _context.CurrentVerifier;
            if (verifier == null || exchange.Session.IsSecure)
            {
                _logger.LogInformation("Rejecting PASE from {Peer}: commissioning window is closed", exchange.Session.Peer);
                await SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                return;
            }

            var request = TlvReader.Decode(first.Payload);
            var initiatorRandom = request.GetBytes(1);
            var initiatorSessionId = (ushort)request.GetUInt(2);
            var passcodeId = request.GetUInt(3);
            var hasParameters = request.GetBool(4);
            if (passcodeId != 0 || initiatorRandom.Length != 32)
            {
                await SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                return;
            }

            var peerParameters = SessionParametersTlv.Read(request.Get(5));
            var localSessionId = _sessions.AllocateSessionId();
            var responderRandom = MatterCrypto.RandomBytes(32);
            var response = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, initiatorRandom);
                w.PutBytes(2, responderRandom);
                w.PutUInt(3, localSessionId);
                if (!hasParameters)
                {
                    w.StartStructure(4);
                    w.PutUInt(1, (uint)verifier.Iterations);
                    w.PutBytes(2, verifier.Salt);
                    w.EndContainer();
                }

                SessionParametersTlv.Write(w, 5);
                w.EndContainer();
            });

            await exchange.SendAsync(SecureChannelOpcode.PbkdfParamResponse, response);
            var context = MatterCrypto.Sha256("CHIP PAKE V1 Commissioning"u8.ToArray(), first.Payload, response);

            var pake1 = await Expect(exchange, SecureChannelOpcode.Pake1);
            if (pake1 == null)
            {
                return;
            }

            var spake = new Spake2PlusVerifier(context, verifier.W0, verifier.L);
            try
            {
                spake.ProcessX(TlvReader.Decode(pake1.Payload).GetBytes(1));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogInformation("PASE: invalid pA from {Peer}: {Message}", exchange.Session.Peer, e.Message);
                await SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                _context.OnPaseFailed();
                return;
            }

            await exchange.SendAsync(SecureChannelOpcode.Pake2, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, spake.Y);
                w.PutBytes(2, spake.CB);
                w.EndContainer();
            }));

            var pake3 = await Expect(exchange, SecureChannelOpcode.Pake3);
            if (pake3 == null)
            {
                return;
            }

            if (!spake.VerifyCA(TlvReader.Decode(pake3.Payload).GetBytes(1)))
            {
                _logger.LogWarning("PASE from {Peer} failed: wrong setup code", exchange.Session.Peer);
                await SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                _context.OnPaseFailed();
                return;
            }

            var keys = MatterCrypto.Kdf(spake.Ke, Array.Empty<byte>(), "SessionKeys"u8.ToArray(), 48);
            var session = new SecureSession(exchange.Session.Peer)
            {
                IsPase = true,
                LocalSessionId = localSessionId,
                PeerSessionId = initiatorSessionId,
                DecryptKey = keys[..16],
                EncryptKey = keys[16..32],
                AttestationChallenge = keys[32..48]
            };
            session.PeerParameters = peerParameters;
            _sessions.Add(session);

            await exchange.SendAsync(SecureChannelOpcode.StatusReport, StatusReport.Success().Encode());
            _logger.LogInformation("PASE session {Session} established with {Peer}", localSessionId, exchange.Session.Peer);
            _context.OnPaseEstablished(session);
        }

        private async Task<MatterMessage> Expect(MessageExchange exchange, byte opcode)
        {
            MatterMessage message;
            try
            {
                message = await exchange.ReceiveAsync(StepTimeout);
            }
            catch (TimeoutException)
            {
                _logger.LogInformation("PASE with {Peer} timed out", exchange.Session.Peer);
                return null;
            }

            if (message.ProtocolId == ProtocolId.SecureChannel && message.Opcode == opcode)
            {
                return message;
            }

            if (message.Opcode == SecureChannelOpcode.StatusReport)
            {
                _logger.LogInformation("PASE aborted by {Peer}: {Status}", exchange.Session.Peer, StatusReport.Decode(message.Payload));
                return null;
            }

            await SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
            return null;
        }

        internal static Task SendStatus(MessageExchange exchange, StatusReport report)
        {
            return exchange.SendAsync(SecureChannelOpcode.StatusReport, report.Encode(), protocolId: ProtocolId.SecureChannel);
        }
    }
}
