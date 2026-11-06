using System.Threading.Channels;
using P3.Driver.Matter.Messages;
using P3.Driver.Matter.Sessions;

namespace P3.Driver.Matter.Exchanges
{
    /// <summary>
    /// One exchange (spec 4.10) with the reliable messaging protocol (spec 4.12): piggybacked or standalone
    /// acks, at most one outstanding reliable message, retransmissions with exponential backoff.
    /// </summary>
    public sealed class MessageExchange
    {
        public const int MaxRetransmissions = 4;
        private static readonly TimeSpan StandaloneAckTimeout = TimeSpan.FromMilliseconds(150);

        private readonly ExchangeManager _manager;
        private readonly Channel<MatterMessage> _inbox = Channel.CreateUnbounded<MatterMessage>();
        private readonly object _lock = new();
        private uint? _pendingAck;
        private int _pendingAckGeneration;
        private Outstanding _outstanding;
        private bool _closed;

        private sealed class Outstanding
        {
            public uint Counter;
            public byte[] Data;
            public readonly TaskCompletionSource<bool> Acked = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly CancellationTokenSource Cancel = new();
        }

        internal MessageExchange(ExchangeManager manager, Session session, ushort exchangeId, bool isInitiator, ushort protocolId)
        {
            _manager = manager;
            Session = session;
            ExchangeId = exchangeId;
            IsInitiator = isInitiator;
            ProtocolId = protocolId;
        }

        public Session Session { get; }
        public SecureSession SecureSession => Session as SecureSession;
        public ushort ExchangeId { get; }
        public bool IsInitiator { get; }
        public ushort ProtocolId { get; }
        public bool IsClosed => _closed;

        /// <summary>Set by a TimedRequest: the following Write/Invoke must arrive before this instant.</summary>
        public DateTime? TimedDeadline { get; set; }

        internal (ulong SessionKey, ushort ExchangeId, bool IsInitiator) Key => (ExchangeManager.SessionKey(Session), ExchangeId, IsInitiator);

        internal void Deliver(MatterMessage message)
        {
            _inbox.Writer.TryWrite(message);
        }

        internal void SetPendingAck(uint counter)
        {
            int generation;
            lock (_lock)
            {
                if (_pendingAck.HasValue && _pendingAck != counter)
                {
                    // a second reliable message before we answered the first: ack the older one right away
                    var older = _pendingAck.Value;
                    _ = _manager.SendStandaloneAckAsync(this, older);
                }

                _pendingAck = counter;
                generation = ++_pendingAckGeneration;
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(StandaloneAckTimeout);
                uint? ack = null;
                lock (_lock)
                {
                    if (_pendingAckGeneration == generation && _pendingAck.HasValue)
                    {
                        ack = _pendingAck;
                        _pendingAck = null;
                    }
                }

                if (ack.HasValue)
                {
                    await _manager.SendStandaloneAckAsync(this, ack.Value);
                }
            });
        }

        internal uint? TakePendingAck()
        {
            lock (_lock)
            {
                var ack = _pendingAck;
                _pendingAck = null;
                return ack;
            }
        }

        internal void OnAck(uint counter)
        {
            Outstanding o;
            lock (_lock)
            {
                o = _outstanding;
                if (o == null || o.Counter != counter)
                {
                    return;
                }

                _outstanding = null;
            }

            o.Cancel.Cancel();
            o.Acked.TrySetResult(true);
        }

        /// <summary>Sends a message on this exchange. Reliable messages are retransmitted until acked.</summary>
        public async Task SendAsync(byte opcode, byte[] payload, bool reliable = true, ushort? protocolId = null)
        {
            if (_closed)
            {
                throw new MatterProtocolException("Exchange is closed");
            }

            // only one reliable message may be in flight: wait for the previous one
            Outstanding previous;
            lock (_lock)
            {
                previous = _outstanding;
            }

            if (previous != null)
            {
                await previous.Acked.Task;
            }

            var header = new PayloadHeader
            {
                Opcode = opcode,
                ExchangeId = ExchangeId,
                ProtocolId = protocolId ?? ProtocolId,
                IsInitiator = IsInitiator,
                NeedsAck = reliable,
                AckedMessageCounter = TakePendingAck()
            };

            var (data, counter) = _manager.Encode(Session, header, payload);
            if (reliable)
            {
                var o = new Outstanding { Counter = counter, Data = data };
                lock (_lock)
                {
                    _outstanding = o;
                }

                _ = RetransmitLoop(o);
            }

            await _manager.Transport.SendAsync(data, Session.Peer);
        }

        private async Task RetransmitLoop(Outstanding o)
        {
            try
            {
                for (var attempt = 0; attempt <= MaxRetransmissions; attempt++)
                {
                    await Task.Delay(_manager.RetransmissionTimeout(Session, attempt), o.Cancel.Token);
                    if (attempt == MaxRetransmissions)
                    {
                        break;
                    }

                    await _manager.Transport.SendAsync(o.Data, Session.Peer);
                }

                lock (_lock)
                {
                    if (_outstanding == o)
                    {
                        _outstanding = null;
                    }
                }

                o.Acked.TrySetResult(false);
                _manager.OnDeliveryFailed(this);
            }
            catch (OperationCanceledException)
            {
                // acked
            }
        }

        /// <summary>Waits until the last reliable message was acknowledged; false if delivery failed.</summary>
        public async Task<bool> WaitForAckAsync()
        {
            Outstanding o;
            lock (_lock)
            {
                o = _outstanding;
            }

            return o == null || await o.Acked.Task;
        }

        public async Task<MatterMessage> ReceiveAsync(TimeSpan timeout, CancellationToken token = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);
            try
            {
                return await _inbox.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException($"No response on exchange {ExchangeId} within {timeout.TotalSeconds:0.#}s");
            }
            catch (ChannelClosedException)
            {
                throw new MatterProtocolException("Exchange closed while waiting for a message");
            }
        }

        /// <summary>Flushes a pending ack, lets the last reliable message finish and unregisters the exchange.</summary>
        public async Task CloseAsync()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _inbox.Writer.TryComplete();
            var ack = TakePendingAck();
            if (ack.HasValue)
            {
                await _manager.SendStandaloneAckAsync(this, ack.Value);
            }

            await WaitForAckAsync();
            _manager.Remove(this);
        }

        internal void Abort()
        {
            _closed = true;
            _inbox.Writer.TryComplete();
            Outstanding o;
            lock (_lock)
            {
                o = _outstanding;
                _outstanding = null;
            }

            if (o != null)
            {
                o.Cancel.Cancel();
                o.Acked.TrySetResult(false);
            }
        }
    }
}
