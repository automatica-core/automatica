using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Transport;

namespace P3.Driver.Matter.Exchanges
{
    public delegate Task ProtocolHandler(MessageExchange exchange, MatterMessage firstMessage);

    /// <summary>Decrypts incoming datagrams, routes them to exchanges and encodes outgoing messages.</summary>
    public sealed class ExchangeManager
    {
        public const byte OpcodeStandaloneAck = 0x10;

        private readonly ILogger _logger;
        private readonly SessionManager _sessions;
        private readonly ConcurrentDictionary<(ulong, ushort, bool), MessageExchange> _exchanges = new();
        private readonly Dictionary<ushort, ProtocolHandler> _handlers = new();
        private int _nextExchangeId = RandomNumberGenerator.GetInt32(0, 0x10000);

        public ExchangeManager(UdpTransport transport, SessionManager sessions, ILogger logger)
        {
            Transport = transport;
            _sessions = sessions;
            _logger = logger;
            transport.MessageReceived = OnDatagram;
            sessions.SessionRemoved += s =>
            {
                foreach (var ex in _exchanges.Values.Where(e => e.Session == s).ToList())
                {
                    ex.Abort();
                    Remove(ex);
                }
            };
        }

        public UdpTransport Transport { get; }

        /// <summary>Raised when a reliable message was never acknowledged (the peer is gone).</summary>
        public event Action<MessageExchange> DeliveryFailed;

        public void RegisterProtocol(ushort protocolId, ProtocolHandler handler) => _handlers[protocolId] = handler;

        internal static ulong SessionKey(Session session)
        {
            return session switch
            {
                SecureSession s => s.LocalSessionId,
                UnsecuredSession u => (1UL << 63) ^ u.EphemeralNodeId,
                _ => 0
            };
        }

        public MessageExchange Initiate(SecureSession session, ushort protocolId)
        {
            var id = (ushort)Interlocked.Increment(ref _nextExchangeId);
            var exchange = new MessageExchange(this, session, id, true, protocolId);
            _exchanges[exchange.Key] = exchange;
            return exchange;
        }

        internal void Remove(MessageExchange exchange) => _exchanges.TryRemove(exchange.Key, out _);

        internal void OnDeliveryFailed(MessageExchange exchange)
        {
            _logger.LogDebug("Message on exchange {Exchange} to {Peer} was not acknowledged", exchange.ExchangeId, exchange.Session.Peer);
            DeliveryFailed?.Invoke(exchange);
        }

        internal TimeSpan RetransmissionTimeout(Session session, int attempt)
        {
            var active = DateTime.UtcNow - session.LastActivity < TimeSpan.FromMilliseconds(session.PeerParameters.ActiveThresholdMs);
            var baseMs = active ? session.PeerParameters.ActiveIntervalMs : session.PeerParameters.IdleIntervalMs;
            var ms = baseMs * 1.1 * Math.Pow(1.6, Math.Max(0, attempt - 1)) * (1 + Random.Shared.NextDouble() * 0.25);
            return TimeSpan.FromMilliseconds(ms);
        }

        internal (byte[] Data, uint Counter) Encode(Session session, PayloadHeader payloadHeader, byte[] payload)
        {
            var plain = payloadHeader.Encode().Concat(payload ?? Array.Empty<byte>()).ToArray();
            if (session is SecureSession s)
            {
                var counter = s.NextSendCounter();
                var header = new MessageHeader { SessionId = s.PeerSessionId, SecurityFlags = 0, MessageCounter = counter };
                var headerBytes = header.Encode();
                var nonce = MessageHeader.BuildNonce(0, counter, s.IsPase ? 0 : s.LocalNodeId);
                var cipher = MatterCrypto.AeadEncrypt(s.EncryptKey, nonce, plain, headerBytes);
                return (headerBytes.Concat(cipher).ToArray(), counter);
            }

            var u = (UnsecuredSession)session;
            var unsecuredCounter = _sessions.NextUnsecuredCounter();
            var unsecuredHeader = new MessageHeader { SessionId = 0, MessageCounter = unsecuredCounter, DestinationNodeId = u.EphemeralNodeId };
            return (unsecuredHeader.Encode().Concat(plain).ToArray(), unsecuredCounter);
        }

        internal async Task SendStandaloneAckAsync(MessageExchange exchange, uint counter)
        {
            await SendStandaloneAckAsync(exchange.Session, exchange.ExchangeId, exchange.IsInitiator, counter);
        }

        private async Task SendStandaloneAckAsync(Session session, ushort exchangeId, bool weAreInitiator, uint counter)
        {
            if (session is SecureSession { IsClosed: true })
            {
                return;
            }

            var header = new PayloadHeader
            {
                Opcode = OpcodeStandaloneAck,
                ExchangeId = exchangeId,
                ProtocolId = Messages.ProtocolId.SecureChannel,
                IsInitiator = weAreInitiator,
                AckedMessageCounter = counter
            };
            var (data, _) = Encode(session, header, Array.Empty<byte>());
            await Transport.SendAsync(data, session.Peer);
        }

        private void OnDatagram(byte[] data, IPEndPoint from)
        {
            MessageHeader header;
            int headerLength;
            try
            {
                header = MessageHeader.Decode(data, out headerLength);
            }
            catch (MatterProtocolException e)
            {
                _logger.LogTrace("Dropping malformed datagram from {From}: {Message}", from, e.Message);
                return;
            }

            if (header.IsGroupSession)
            {
                return;
            }

            Session session;
            byte[] plain;
            if (header.SessionId == 0)
            {
                if (!header.SourceNodeId.HasValue)
                {
                    return;
                }

                session = _sessions.GetOrCreateUnsecured(from, header.SourceNodeId.Value);
                plain = data[headerLength..];
            }
            else
            {
                var secure = _sessions.Get(header.SessionId);
                if (secure == null || (header.SecurityFlags & MessageHeader.SecurityFlagPrivacy) != 0)
                {
                    _logger.LogTrace("Dropping message for unknown session {Session} from {From}", header.SessionId, from);
                    return;
                }

                try
                {
                    var nonce = MessageHeader.BuildNonce(header.SecurityFlags, header.MessageCounter, secure.IsPase ? 0 : secure.PeerNodeId);
                    plain = MatterCrypto.AeadDecrypt(secure.DecryptKey, nonce, data.AsSpan(headerLength), data.AsSpan(0, headerLength));
                }
                catch (CryptographicException)
                {
                    _logger.LogDebug("Dropping message with a bad MIC on session {Session}", header.SessionId);
                    return;
                }

                session = secure;
            }

            PayloadHeader payloadHeader;
            int payloadHeaderLength;
            try
            {
                payloadHeader = PayloadHeader.Decode(plain, out payloadHeaderLength);
            }
            catch (MatterProtocolException)
            {
                return;
            }

            var duplicate = !session.Reception.Accept(header.MessageCounter);
            session.Peer = from;
            session.LastActivity = DateTime.UtcNow;

            var message = new MatterMessage
            {
                Header = header,
                PayloadHeader = payloadHeader,
                Payload = plain[payloadHeaderLength..]
            };

            var key = (SessionKey(session), payloadHeader.ExchangeId, !payloadHeader.IsInitiator);
            _exchanges.TryGetValue(key, out var exchange);

            if (payloadHeader.AckedMessageCounter.HasValue)
            {
                exchange?.OnAck(payloadHeader.AckedMessageCounter.Value);
            }

            if (duplicate)
            {
                if (payloadHeader.NeedsAck)
                {
                    _ = SendStandaloneAckAsync(session, payloadHeader.ExchangeId, !payloadHeader.IsInitiator, header.MessageCounter);
                }

                return;
            }

            if (payloadHeader.ProtocolId == Messages.ProtocolId.SecureChannel && payloadHeader.Opcode == OpcodeStandaloneAck)
            {
                return;
            }

            _logger.LogTrace("RX {Session} {Message}", session is SecureSession ss ? ss.LocalSessionId : 0, message);

            if (exchange == null || exchange.IsClosed)
            {
                if (!payloadHeader.IsInitiator || !_handlers.TryGetValue(payloadHeader.ProtocolId, out var handler))
                {
                    if (payloadHeader.NeedsAck)
                    {
                        _ = SendStandaloneAckAsync(session, payloadHeader.ExchangeId, !payloadHeader.IsInitiator, header.MessageCounter);
                    }

                    return;
                }

                if (exchange != null)
                {
                    Remove(exchange);
                }

                exchange = new MessageExchange(this, session, payloadHeader.ExchangeId, false, payloadHeader.ProtocolId);
                _exchanges[exchange.Key] = exchange;
                if (payloadHeader.NeedsAck)
                {
                    exchange.SetPendingAck(header.MessageCounter);
                }

                var ex = exchange;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(ex, message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Matter exchange {Exchange} (protocol {Protocol}) failed", ex.ExchangeId, ex.ProtocolId);
                    }
                    finally
                    {
                        try
                        {
                            await ex.CloseAsync();
                        }
                        catch (Exception e)
                        {
                            _logger.LogDebug(e, "Closing exchange failed");
                            Remove(ex);
                        }
                    }
                });
                return;
            }

            if (payloadHeader.NeedsAck)
            {
                exchange.SetPendingAck(header.MessageCounter);
            }

            exchange.Deliver(message);
        }
    }
}
