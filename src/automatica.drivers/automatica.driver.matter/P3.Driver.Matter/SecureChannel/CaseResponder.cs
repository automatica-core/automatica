using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.SecureChannel
{
    /// <summary>The CASE responder (spec 4.14.2): Sigma1 / Sigma2 / Sigma3 without session resumption.</summary>
    public sealed class CaseResponder
    {
        private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);
        private static readonly byte[] Sigma2Nonce = "NCASE_Sigma2N"u8.ToArray();
        private static readonly byte[] Sigma3Nonce = "NCASE_Sigma3N"u8.ToArray();

        private readonly SessionManager _sessions;
        private readonly Func<IEnumerable<Fabric>> _fabrics;
        private readonly ILogger _logger;

        public CaseResponder(SessionManager sessions, Func<IEnumerable<Fabric>> fabrics, ILogger logger)
        {
            _sessions = sessions;
            _fabrics = fabrics;
            _logger = logger;
        }

        public async Task HandleAsync(MessageExchange exchange, MatterMessage first)
        {
            if (exchange.Session.IsSecure)
            {
                return;
            }

            var sigma1 = TlvReader.Decode(first.Payload);
            var initiatorRandom = sigma1.GetBytes(1);
            var initiatorSessionId = (ushort)sigma1.GetUInt(2);
            var destinationId = sigma1.GetBytes(3);
            var initiatorEphPub = sigma1.GetBytes(4);
            var peerParameters = SessionParametersTlv.Read(sigma1.Get(5));

            var fabric = _fabrics().FirstOrDefault(f => MatterCrypto.FixedTimeEquals(f.ComputeDestinationId(initiatorRandom), destinationId));
            if (fabric == null)
            {
                _logger.LogInformation("CASE from {Peer}: no matching fabric", exchange.Session.Peer);
                await PaseResponder.SendStatus(exchange, StatusReport.Error(SecureChannelStatus.NoSharedTrustRoots));
                return;
            }

            using var ephemeral = MatterCrypto.CreateEphemeralKey();
            var ephPub = MatterCrypto.ExportPublicKey(ephemeral);
            byte[] shared;
            try
            {
                shared = MatterCrypto.Ecdh(ephemeral, initiatorEphPub);
            }
            catch (Exception e) when (e is CryptographicException or MatterProtocolException)
            {
                await PaseResponder.SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                return;
            }

            var responderRandom = MatterCrypto.RandomBytes(32);
            var localSessionId = _sessions.AllocateSessionId();
            var ipk = fabric.OperationalIpk;

            var tbs2 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, fabric.Noc);
                if (fabric.Icac != null)
                {
                    w.PutBytes(2, fabric.Icac);
                }

                w.PutBytes(3, ephPub);
                w.PutBytes(4, initiatorEphPub);
                w.EndContainer();
            });
            var signature2 = MatterCrypto.Sign(fabric.OperationalKey, tbs2);
            var tbe2 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, fabric.Noc);
                if (fabric.Icac != null)
                {
                    w.PutBytes(2, fabric.Icac);
                }

                w.PutBytes(3, signature2);
                w.PutBytes(4, MatterCrypto.RandomBytes(16));
                w.EndContainer();
            });

            var s2kSalt = Concat(ipk, responderRandom, ephPub, MatterCrypto.Sha256(first.Payload));
            var s2k = MatterCrypto.Kdf(shared, s2kSalt, "Sigma2"u8.ToArray(), 16);
            var encrypted2 = MatterCrypto.AeadEncrypt(s2k, Sigma2Nonce, tbe2, ReadOnlySpan<byte>.Empty);

            var sigma2 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, responderRandom);
                w.PutUInt(2, localSessionId);
                w.PutBytes(3, ephPub);
                w.PutBytes(4, encrypted2);
                SessionParametersTlv.Write(w, 5);
                w.EndContainer();
            });
            await exchange.SendAsync(SecureChannelOpcode.Sigma2, sigma2);

            MatterMessage m3;
            try
            {
                m3 = await exchange.ReceiveAsync(StepTimeout);
            }
            catch (TimeoutException)
            {
                _logger.LogInformation("CASE with {Peer} timed out waiting for Sigma3", exchange.Session.Peer);
                return;
            }

            if (m3.Opcode != SecureChannelOpcode.Sigma3)
            {
                if (m3.Opcode == SecureChannelOpcode.StatusReport)
                {
                    _logger.LogInformation("CASE aborted by {Peer}: {Status}", exchange.Session.Peer, StatusReport.Decode(m3.Payload));
                    return;
                }

                await PaseResponder.SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                return;
            }

            var s3kSalt = Concat(ipk, MatterCrypto.Sha256(first.Payload, sigma2));
            var s3k = MatterCrypto.Kdf(shared, s3kSalt, "Sigma3"u8.ToArray(), 16);

            MatterCertificate noc;
            try
            {
                var tbe3 = TlvReader.Decode(MatterCrypto.AeadDecrypt(s3k, Sigma3Nonce, TlvReader.Decode(m3.Payload).GetBytes(1), ReadOnlySpan<byte>.Empty));
                var initiatorNoc = tbe3.GetBytes(1);
                var initiatorIcac = tbe3.GetBytesOrNull(2);
                var signature3 = tbe3.GetBytes(3);

                noc = ValidateChain(fabric, initiatorNoc, initiatorIcac);
                var tbs3 = TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    w.PutBytes(1, initiatorNoc);
                    if (initiatorIcac != null)
                    {
                        w.PutBytes(2, initiatorIcac);
                    }

                    w.PutBytes(3, initiatorEphPub);
                    w.PutBytes(4, ephPub);
                    w.EndContainer();
                });
                if (!MatterCrypto.Verify(noc.PublicKey, tbs3, signature3))
                {
                    throw new MatterProtocolException("Sigma3 signature is invalid");
                }
            }
            catch (Exception e) when (e is CryptographicException or MatterProtocolException)
            {
                _logger.LogWarning("CASE from {Peer} rejected: {Message}", exchange.Session.Peer, e.Message);
                await PaseResponder.SendStatus(exchange, StatusReport.Error(SecureChannelStatus.InvalidParameter));
                return;
            }

            var keys = MatterCrypto.Kdf(shared, Concat(ipk, MatterCrypto.Sha256(first.Payload, sigma2, m3.Payload)), "SessionKeys"u8.ToArray(), 48);
            var session = new SecureSession(exchange.Session.Peer)
            {
                IsPase = false,
                LocalSessionId = localSessionId,
                PeerSessionId = initiatorSessionId,
                DecryptKey = keys[..16],
                EncryptKey = keys[16..32],
                AttestationChallenge = keys[32..48],
                LocalNodeId = fabric.NodeId,
                PeerNodeId = noc.SubjectNodeId ?? 0,
                CaseAuthenticatedTags = noc.SubjectCats,
                Fabric = fabric
            };
            session.PeerParameters = peerParameters;
            _sessions.Add(session);

            await exchange.SendAsync(SecureChannelOpcode.StatusReport, StatusReport.Success().Encode());
            _logger.LogInformation("CASE session {Session} established with node {Node:X16} on fabric {Fabric}", localSessionId, session.PeerNodeId, fabric.FabricIndex);
        }

        internal static MatterCertificate ValidateChain(Fabric fabric, byte[] nocBytes, byte[] icacBytes)
        {
            var noc = new MatterCertificate(nocBytes);
            var issuerKey = fabric.RootPublicKey;
            if (icacBytes != null)
            {
                var icac = new MatterCertificate(icacBytes);
                if (!icac.VerifySignature(fabric.RootPublicKey))
                {
                    throw new MatterProtocolException("ICAC is not signed by the fabric root");
                }

                issuerKey = icac.PublicKey;
            }

            if (!noc.VerifySignature(issuerKey))
            {
                throw new MatterProtocolException("NOC signature is invalid");
            }

            if (noc.SubjectFabricId != fabric.FabricId)
            {
                throw new MatterProtocolException("NOC belongs to another fabric");
            }

            if (noc.SubjectNodeId == null)
            {
                throw new MatterProtocolException("NOC has no node id");
            }

            return noc;
        }

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    }
}
