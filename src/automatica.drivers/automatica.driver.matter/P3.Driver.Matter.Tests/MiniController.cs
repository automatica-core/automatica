using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Channels;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.SecureChannel;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    /// <summary>
    /// A minimal Matter commissioner used to drive the bridge over real UDP: PASE, the commissioning
    /// commands, CASE and interaction model requests. It acks every reliable message right away.
    /// </summary>
    internal sealed class MiniController : IDisposable
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly UdpClient _udp;
        private readonly IPEndPoint _device;
        private readonly Channel<MatterMessage> _inbox = Channel.CreateUnbounded<MatterMessage>();
        private readonly Dictionary<ushort, ControllerSession> _sessions = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly ulong _ephemeralNodeId = BitConverter.ToUInt64(MatterCrypto.RandomBytes(8));
        private uint _counter = (uint)RandomNumberGenerator.GetInt32(1, 1 << 20);
        private ushort _exchangeId = (ushort)RandomNumberGenerator.GetInt32(1, 1000);

        public sealed class ControllerSession
        {
            public ushort LocalSessionId;
            public ushort PeerSessionId;
            public byte[] I2R;
            public byte[] R2I;
            public byte[] AttestationChallenge;
            public bool IsPase;
            public ulong LocalNodeId;
            public ulong PeerNodeId;
            public readonly ReceptionState Reception = new();
        }

        public MiniController(int devicePort)
        {
            _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            _device = new IPEndPoint(IPAddress.Loopback, devicePort);
            _ = Task.Run(ReceiveLoop);
        }

        /// <summary>The session requests go out on (null = unsecured).</summary>
        public ControllerSession Session { get; set; }

        public List<MatterMessage> Received { get; } = new();

        private async Task ReceiveLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udp.ReceiveAsync(_cts.Token);
                }
                catch
                {
                    return;
                }

                var data = result.Buffer;
                var header = MessageHeader.Decode(data, out var hlen);
                byte[] plain;
                ControllerSession session = null;
                if (header.SessionId == 0)
                {
                    plain = data[hlen..];
                }
                else
                {
                    if (!_sessions.TryGetValue(header.SessionId, out session))
                    {
                        continue;
                    }

                    var nonce = MessageHeader.BuildNonce(header.SecurityFlags, header.MessageCounter, session.IsPase ? 0 : session.PeerNodeId);
                    plain = MatterCrypto.AeadDecrypt(session.R2I, nonce, data.AsSpan(hlen), data.AsSpan(0, hlen));
                    if (!session.Reception.Accept(header.MessageCounter))
                    {
                        continue;
                    }
                }

                var ph = PayloadHeader.Decode(plain, out var plen);
                var message = new MatterMessage { Header = header, PayloadHeader = ph, Payload = plain[plen..] };
                if (ph.NeedsAck)
                {
                    await SendRawAsync(session, new PayloadHeader
                    {
                        Opcode = 0x10,
                        ExchangeId = ph.ExchangeId,
                        ProtocolId = ProtocolId.SecureChannel,
                        IsInitiator = !ph.IsInitiator,
                        AckedMessageCounter = header.MessageCounter
                    }, Array.Empty<byte>());
                }

                if (ph.ProtocolId == ProtocolId.SecureChannel && ph.Opcode == 0x10)
                {
                    continue;
                }

                lock (Received)
                {
                    Received.Add(message);
                }

                _inbox.Writer.TryWrite(message);
            }
        }

        private async Task SendRawAsync(ControllerSession session, PayloadHeader ph, byte[] payload)
        {
            var plain = ph.Encode().Concat(payload).ToArray();
            var counter = Interlocked.Increment(ref _counter);
            byte[] datagram;
            if (session == null)
            {
                var header = new MessageHeader { SessionId = 0, MessageCounter = counter, SourceNodeId = _ephemeralNodeId };
                datagram = header.Encode().Concat(plain).ToArray();
            }
            else
            {
                var header = new MessageHeader { SessionId = session.PeerSessionId, MessageCounter = counter };
                var headerBytes = header.Encode();
                var nonce = MessageHeader.BuildNonce(0, counter, session.IsPase ? 0 : session.LocalNodeId);
                datagram = headerBytes.Concat(MatterCrypto.AeadEncrypt(session.I2R, nonce, plain, headerBytes)).ToArray();
            }

            await _udp.SendAsync(datagram, _device);
        }

        public ushort NewExchange() => ++_exchangeId;

        public Task SendAsync(ushort exchangeId, bool initiator, ushort protocol, byte opcode, byte[] payload, ControllerSession session = null)
        {
            return SendRawAsync(session ?? Session, new PayloadHeader
            {
                Opcode = opcode,
                ExchangeId = exchangeId,
                ProtocolId = protocol,
                IsInitiator = initiator,
                NeedsAck = true
            }, payload);
        }

        public async Task<MatterMessage> NextAsync(Func<MatterMessage, bool> filter, TimeSpan? timeout = null)
        {
            using var cts = new CancellationTokenSource(timeout ?? Timeout);
            while (true)
            {
                var m = await _inbox.Reader.ReadAsync(cts.Token);
                if (filter(m))
                {
                    return m;
                }
            }
        }

        public async Task<MatterMessage> RequestAsync(ushort protocol, byte opcode, byte[] payload, ushort? exchangeId = null)
        {
            var ex = exchangeId ?? NewExchange();
            await SendAsync(ex, true, protocol, opcode, payload);
            return await NextAsync(m => m.PayloadHeader.ExchangeId == ex && !m.PayloadHeader.IsInitiator);
        }

        // ------------------------------------------------------------------ PASE

        public async Task<ControllerSession> EstablishPaseAsync(uint passcode)
        {
            Session = null;
            var ex = NewExchange();
            var localSessionId = (ushort)RandomNumberGenerator.GetInt32(1, 0xFFFF);
            var request = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, MatterCrypto.RandomBytes(32));
                w.PutUInt(2, localSessionId);
                w.PutUInt(3, 0);
                w.PutBool(4, false);
                w.EndContainer();
            });
            var response = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.PbkdfParamRequest, request, ex);
            Assert.Equal(SecureChannelOpcode.PbkdfParamResponse, response.Opcode);
            var resp = TlvReader.Decode(response.Payload);
            var parameters = resp.Require(4);
            var (w0, w1) = Spake2Plus.ComputeW0W1(passcode, parameters.GetBytes(2), (int)parameters.GetUInt(1));
            var context = MatterCrypto.Sha256("CHIP PAKE V1 Commissioning"u8.ToArray(), request, response.Payload);
            var prover = new Spake2PlusProver(context, w0, w1);

            var pake2 = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Pake1, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, prover.X);
                w.EndContainer();
            }), ex);
            if (pake2.Opcode == SecureChannelOpcode.StatusReport)
            {
                return null;
            }

            var p2 = TlvReader.Decode(pake2.Payload);
            prover.ProcessY(p2.GetBytes(1));
            Assert.True(prover.VerifyCB(p2.GetBytes(2)));

            var status = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Pake3, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, prover.CA);
                w.EndContainer();
            }), ex);
            Assert.Equal(SecureChannelOpcode.StatusReport, status.Opcode);
            if (!StatusReport.Decode(status.Payload).IsSuccess)
            {
                return null;
            }

            var keys = MatterCrypto.Kdf(prover.Ke, Array.Empty<byte>(), "SessionKeys"u8.ToArray(), 48);
            var session = new ControllerSession
            {
                LocalSessionId = localSessionId,
                PeerSessionId = (ushort)resp.GetUInt(3),
                I2R = keys[..16],
                R2I = keys[16..32],
                AttestationChallenge = keys[32..],
                IsPase = true
            };
            _sessions[localSessionId] = session;
            Session = session;
            return session;
        }

        /// <summary>A PASE attempt with a wrong passcode; returns the device's answer to Pake1 or Pake3.</summary>
        public async Task<StatusReport> FailPaseAsync(uint wrongPasscode)
        {
            Session = null;
            var ex = NewExchange();
            var request = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, MatterCrypto.RandomBytes(32));
                w.PutUInt(2, 4242);
                w.PutUInt(3, 0);
                w.PutBool(4, false);
                w.EndContainer();
            });
            var response = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.PbkdfParamRequest, request, ex);
            if (response.Opcode == SecureChannelOpcode.StatusReport)
            {
                return StatusReport.Decode(response.Payload);
            }

            var resp = TlvReader.Decode(response.Payload);
            var parameters = resp.Require(4);
            var (w0, w1) = Spake2Plus.ComputeW0W1(wrongPasscode, parameters.GetBytes(2), (int)parameters.GetUInt(1));
            var prover = new Spake2PlusProver(MatterCrypto.Sha256("CHIP PAKE V1 Commissioning"u8.ToArray(), request, response.Payload), w0, w1);
            var pake2 = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Pake1, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, prover.X);
                w.EndContainer();
            }), ex);
            prover.ProcessY(TlvReader.Decode(pake2.Payload).GetBytes(1));
            var status = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Pake3, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, prover.CA);
                w.EndContainer();
            }), ex);
            return StatusReport.Decode(status.Payload);
        }

        // ------------------------------------------------------------------ CASE

        public async Task<ControllerSession> EstablishCaseAsync(TestCertificateAuthority ca, byte[] ipkEpochKey, ulong controllerNodeId, ECDsa controllerKey,
            byte[] controllerNoc, ulong deviceNodeId)
        {
            Session = null;
            var ex = NewExchange();
            var localSessionId = (ushort)RandomNumberGenerator.GetInt32(1, 0xFFFF);
            var compressed = Fabric.ComputeCompressedFabricId(ca.RootPublicKey, ca.FabricId);
            var ipk = MatterCrypto.Kdf(ipkEpochKey, compressed, "GroupKey v1.0"u8.ToArray(), 16);
            var initiatorRandom = MatterCrypto.RandomBytes(32);
            var destination = initiatorRandom.Concat(ca.RootPublicKey).Concat(BitConverter.GetBytes(ca.FabricId)).Concat(BitConverter.GetBytes(deviceNodeId)).ToArray();
            using var eph = MatterCrypto.CreateEphemeralKey();
            var ephPub = MatterCrypto.ExportPublicKey(eph);

            var sigma1 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, initiatorRandom);
                w.PutUInt(2, localSessionId);
                w.PutBytes(3, MatterCrypto.HmacSha256(ipk, destination));
                w.PutBytes(4, ephPub);
                w.EndContainer();
            });
            var sigma2Message = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Sigma1, sigma1, ex);
            if (sigma2Message.Opcode != SecureChannelOpcode.Sigma2)
            {
                return null;
            }

            var sigma2 = TlvReader.Decode(sigma2Message.Payload);
            var responderRandom = sigma2.GetBytes(1);
            var responderEph = sigma2.GetBytes(3);
            var shared = MatterCrypto.Ecdh(eph, responderEph);
            var s2k = MatterCrypto.Kdf(shared, ipk.Concat(responderRandom).Concat(responderEph).Concat(MatterCrypto.Sha256(sigma1)).ToArray(), "Sigma2"u8.ToArray(), 16);
            var tbe2 = TlvReader.Decode(MatterCrypto.AeadDecrypt(s2k, "NCASE_Sigma2N"u8.ToArray(), sigma2.GetBytes(4), ReadOnlySpan<byte>.Empty));
            var deviceNoc = new Certificates.MatterCertificate(tbe2.GetBytes(1));
            Assert.True(deviceNoc.VerifySignature(ca.RootPublicKey), "device NOC must chain to our root");
            var tbs2 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, tbe2.GetBytes(1));
                w.PutBytes(3, responderEph);
                w.PutBytes(4, ephPub);
                w.EndContainer();
            });
            Assert.True(MatterCrypto.Verify(deviceNoc.PublicKey, tbs2, tbe2.GetBytes(3)), "Sigma2 signature");

            var tbs3 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, controllerNoc);
                w.PutBytes(3, ephPub);
                w.PutBytes(4, responderEph);
                w.EndContainer();
            });
            var tbe3 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, controllerNoc);
                w.PutBytes(3, MatterCrypto.Sign(controllerKey, tbs3));
                w.EndContainer();
            });
            var s3k = MatterCrypto.Kdf(shared, ipk.Concat(MatterCrypto.Sha256(sigma1, sigma2Message.Payload)).ToArray(), "Sigma3"u8.ToArray(), 16);
            var sigma3 = TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBytes(1, MatterCrypto.AeadEncrypt(s3k, "NCASE_Sigma3N"u8.ToArray(), tbe3, ReadOnlySpan<byte>.Empty));
                w.EndContainer();
            });
            var status = await RequestAsync(ProtocolId.SecureChannel, SecureChannelOpcode.Sigma3, sigma3, ex);
            Assert.Equal(SecureChannelOpcode.StatusReport, status.Opcode);
            Assert.True(StatusReport.Decode(status.Payload).IsSuccess);

            var keys = MatterCrypto.Kdf(shared, ipk.Concat(MatterCrypto.Sha256(sigma1, sigma2Message.Payload, sigma3)).ToArray(), "SessionKeys"u8.ToArray(), 48);
            var session = new ControllerSession
            {
                LocalSessionId = localSessionId,
                PeerSessionId = (ushort)sigma2.GetUInt(2),
                I2R = keys[..16],
                R2I = keys[16..32],
                AttestationChallenge = keys[32..],
                IsPase = false,
                LocalNodeId = controllerNodeId,
                PeerNodeId = deviceNodeId
            };
            _sessions[localSessionId] = session;
            Session = session;
            return session;
        }

        // ------------------------------------------------------------------ interaction model

        /// <summary>Invokes a command; returns the InvokeResponseIB (struct with tag 0 = command data or 1 = status).</summary>
        public async Task<TlvElement> InvokeAsync(ushort endpoint, uint cluster, uint command, Action<TlvWriter> fields = null, bool timed = false)
        {
            var ex = NewExchange();
            if (timed)
            {
                var timedResponse = await RequestAsync(ProtocolId.InteractionModel, ImOpcode.TimedRequest, TlvWriter.Encode(w =>
                {
                    w.StartStructure();
                    w.PutUInt(0, 5000);
                    w.EndContainer();
                }), ex);
                Assert.Equal(ImOpcode.StatusResponse, timedResponse.Opcode);
            }

            var response = await RequestAsync(ProtocolId.InteractionModel, ImOpcode.InvokeRequest, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutBool(0, false);
                w.PutBool(1, timed);
                w.StartArray(2);
                w.StartStructure();
                w.StartList(0);
                w.PutUInt(0, endpoint);
                w.PutUInt(1, cluster);
                w.PutUInt(2, command);
                w.EndContainer();
                w.StartStructure(1);
                fields?.Invoke(w);
                w.EndContainer();
                w.EndContainer();
                w.EndContainer();
                w.PutUInt(0xFF, 11);
                w.EndContainer();
            }), ex);
            if (response.Opcode == ImOpcode.StatusResponse)
            {
                throw new MatterStatusException((StatusCode)TlvReader.Decode(response.Payload).GetUInt(0));
            }

            Assert.Equal(ImOpcode.InvokeResponse, response.Opcode);
            return TlvReader.Decode(response.Payload).Require(1).Children.Single();
        }

        /// <summary>Invokes a command that must answer with a response command; returns its fields.</summary>
        public async Task<TlvElement> InvokeWithResponseAsync(ushort endpoint, uint cluster, uint command, Action<TlvWriter> fields = null, bool timed = false)
        {
            var ib = await InvokeAsync(endpoint, cluster, command, fields, timed);
            var data = ib.Get(0);
            if (data == null)
            {
                throw new MatterStatusException((StatusCode)ib.Require(1).Require(1).GetUInt(0));
            }

            return data.Require(1);
        }

        public static StatusCode InvokeStatus(TlvElement invokeResponseIb) => (StatusCode)invokeResponseIb.Require(1).Require(1).GetUInt(0);

        public static byte? InvokeClusterStatus(TlvElement invokeResponseIb) => (byte?)invokeResponseIb.Require(1).Require(1).GetUIntOrNull(1);

        /// <summary>Reads paths (null = wildcard), following chunked reports; returns all AttributeReportIBs.</summary>
        public async Task<List<TlvElement>> ReadAsync(params (ushort? Endpoint, uint? Cluster, uint? Attribute)[] paths)
        {
            var ex = NewExchange();
            await SendAsync(ex, true, ProtocolId.InteractionModel, ImOpcode.ReadRequest, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.StartArray(0);
                foreach (var p in paths)
                {
                    w.StartList();
                    if (p.Endpoint.HasValue) w.PutUInt(2, p.Endpoint.Value);
                    if (p.Cluster.HasValue) w.PutUInt(3, p.Cluster.Value);
                    if (p.Attribute.HasValue) w.PutUInt(4, p.Attribute.Value);
                    w.EndContainer();
                }

                w.EndContainer();
                w.PutBool(3, true);
                w.PutUInt(0xFF, 11);
                w.EndContainer();
            }));
            return await CollectReportsAsync(ex, false);
        }

        public async Task<List<TlvElement>> CollectReportsAsync(ushort exchangeId, bool deviceIsInitiator)
        {
            var reports = new List<TlvElement>();
            while (true)
            {
                var m = await NextAsync(x => x.PayloadHeader.ExchangeId == exchangeId && x.PayloadHeader.IsInitiator == deviceIsInitiator);
                Assert.Equal(ImOpcode.ReportData, m.Opcode);
                var report = TlvReader.Decode(m.Payload);
                reports.AddRange(report.Get(1)?.Children ?? new List<TlvElement>());
                if (report.GetBool(4))
                {
                    return reports;
                }

                await SendStatusResponseAsync(exchangeId, !deviceIsInitiator);
                if (!report.GetBool(3))
                {
                    return reports;
                }
            }
        }

        public Task SendStatusResponseAsync(ushort exchangeId, bool initiator) =>
            SendAsync(exchangeId, initiator, ProtocolId.InteractionModel, ImOpcode.StatusResponse, TlvWriter.Encode(w =>
            {
                w.StartStructure();
                w.PutUInt(0, 0);
                w.PutUInt(0xFF, 11);
                w.EndContainer();
            }));

        public void Dispose()
        {
            _cts.Cancel();
            _udp.Dispose();
        }
    }

    internal static class ReportExtensions
    {
        public static TlvElement Data(this IEnumerable<TlvElement> reports, ushort endpoint, uint cluster, uint attribute)
        {
            return reports.Select(r => r.Get(1)).Where(d => d != null).FirstOrDefault(d =>
            {
                var p = d.Require(1);
                return p.GetUInt(2) == endpoint && p.GetUInt(3) == cluster && p.GetUInt(4) == attribute;
            })?.Require(2);
        }
    }
}
