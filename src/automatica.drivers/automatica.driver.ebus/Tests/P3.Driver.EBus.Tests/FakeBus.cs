using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using P3.Driver.EBus.Wolf;

namespace P3.Driver.EBus.Tests
{
    /// <summary>
    /// A bus behind a raw adapter: a SYN generator, the echo of every written byte, a Wolf device answering register reads
    /// and other masters whose telegrams can be injected.
    /// </summary>
    internal sealed class FakeBus : IEBusTransport
    {
        private enum State
        {
            Idle,
            Master,
            AwaitMasterAck
        }

        private readonly Channel<byte[]> _toDriver = Channel.CreateUnbounded<byte[]>();
        private readonly object _lock = new();
        private readonly List<byte> _master = new();
        private byte[] _pending = Array.Empty<byte>();
        private int _pendingIndex;
        private Timer _synGenerator;
        private State _state;
        private bool _busy;

        public byte SlaveAddress { get; set; } = 0x08;
        public Dictionary<ushort, ushort> Registers { get; } = new();

        /// <summary>
        /// how often a higher priority master starts at the same time as the driver (and wins)
        /// </summary>
        public int LoseArbitration { get; set; }

        /// <summary>the telegram the winning master sends, escaped, without the trailing SYN</summary>
        public byte[] CompetingTelegram { get; set; } = Hex("10 03 05 07 09 bb 03 3e 02 00 80 ff 6a ff c8 00");

        /// <summary>the unescaped master parts the driver sent</summary>
        public List<byte[]> Requests { get; } = new();

        public bool Disposed { get; private set; }

        public static byte[] Hex(string hex) => hex.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();

        public Task ConnectAsync(CancellationToken token)
        {
            _synGenerator = new Timer(_ =>
            {
                lock (_lock)
                {
                    if (!_busy)
                    {
                        Emit(EBusProtocol.Syn);
                    }
                }
            }, null, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
            return Task.CompletedTask;
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token)
        {
            if (_pendingIndex >= _pending.Length)
            {
                if (!await _toDriver.Reader.WaitToReadAsync(token) || !_toDriver.Reader.TryRead(out var chunk))
                {
                    return 0;
                }

                _pending = chunk;
                _pendingIndex = 0;
            }

            var count = Math.Min(buffer.Length, _pending.Length - _pendingIndex);
            _pending.AsMemory(_pendingIndex, count).CopyTo(buffer);
            _pendingIndex += count;
            return count;
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token)
        {
            lock (_lock)
            {
                foreach (var b in data.Span)
                {
                    Handle(b);
                }
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Another device sends a telegram (escaped, without SYN)
        /// </summary>
        public void Inject(byte[] telegram)
        {
            lock (_lock)
            {
                _busy = true;
                Emit(telegram);
                Emit(EBusProtocol.Syn);
                _busy = false;
            }
        }

        /// <summary>
        /// The adapter drops the connection
        /// </summary>
        public void Close() => _toDriver.Writer.TryComplete();

        private void Emit(params byte[] bytes) => _toDriver.Writer.TryWrite(bytes);

        private void Handle(byte b)
        {
            switch (_state)
            {
                case State.Idle when EBusProtocol.IsMaster(b):
                    if (LoseArbitration > 0)
                    {
                        LoseArbitration--;
                        _busy = true;
                        Emit(CompetingTelegram);
                        Emit(EBusProtocol.Syn);
                        _busy = false;
                        return;
                    }

                    _busy = true;
                    _master.Clear();
                    _master.Add(b);
                    Emit(b);
                    _state = State.Master;
                    return;
                case State.Master when b == EBusProtocol.Syn:
                    // the master gave up its telegram
                    Emit(b);
                    _state = State.Idle;
                    _busy = false;
                    return;
                case State.Master:
                    Emit(b);
                    _master.Add(b);
                    var telegram = EBusProtocol.Unescape(_master.ToArray());
                    if (telegram.Length < 5 || telegram.Length < 6 + telegram[4] || _master[^1] == EBusProtocol.EscapeByte)
                    {
                        return;
                    }

                    Requests.Add(telegram);
                    if (telegram[1] != SlaveAddress)
                    {
                        // nobody there: the SYN generator takes over
                        _state = State.Idle;
                        _busy = false;
                        return;
                    }

                    byte[] data;
                    if (telegram[2] == WolfRegister.Primary && telegram[3] == WolfRegister.ReadSecondary)
                    {
                        var register = (ushort)(telegram[6] | telegram[7] << 8);
                        var value = Registers.TryGetValue(register, out var v) ? v : WolfRegister.NotAvailable;
                        data = new[] { (byte)(value & 0xFF), (byte)(value >> 8) };
                    }
                    else
                    {
                        data = new byte[] { 0x19 };
                    }

                    var response = new byte[data.Length + 2];
                    response[0] = (byte)data.Length;
                    data.CopyTo(response, 1);
                    response[^1] = EBusProtocol.Checksum(response.AsSpan(0, response.Length - 1));
                    Emit(EBusProtocol.Ack);
                    Emit(EBusProtocol.Escape(response));
                    _state = State.AwaitMasterAck;
                    return;
                case State.AwaitMasterAck:
                    Emit(b);
                    if (b == EBusProtocol.Syn)
                    {
                        _state = State.Idle;
                        _busy = false;
                    }

                    return;
                default:
                    Emit(b);
                    return;
            }
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            _synGenerator?.Dispose();
            _toDriver.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }
}
