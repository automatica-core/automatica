using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.EBus.Wolf;

namespace P3.Driver.EBus
{
    public enum EBusResultStatus
    {
        Ok,
        /// <summary>no slave acknowledged the request</summary>
        NoAnswer,
        /// <summary>the slave rejected the request or the answer was broken, also after the retries</summary>
        Failed,
        /// <summary>other masters won the arbitration every time</summary>
        Busy,
        /// <summary>the bus was never free long enough</summary>
        Timeout
    }

    public sealed record EBusResult(EBusResultStatus Status, byte[] Response)
    {
        public bool IsOk => Status == EBusResultStatus.Ok;
    }

    public class EBusException : Exception
    {
        public EBusException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Listens to every telegram on the bus and sends master-slave requests as master <see cref="OwnAddress"/>.
    /// The adapter is a raw byte bridge: a request starts right after a SYN that follows a SYN (the bus is idle),
    /// the address byte must come back unchanged (arbitration won), then the rest of the telegram is sent and the answer read.
    /// </summary>
    public sealed class EBusConnection : IAsyncDisposable
    {
        internal static TimeSpan ByteTimeout = TimeSpan.FromMilliseconds(400);
        internal static TimeSpan SilenceTimeout = TimeSpan.FromSeconds(30);
        internal static TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
        internal const int MaxAttempts = 3;

        private readonly IEBusTransport _transport;
        private readonly ILogger _logger;
        private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private readonly ConcurrentQueue<PendingRequest> _requests = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly List<byte> _frame = new(64);

        private byte[] _chunk = Array.Empty<byte>();
        private int _index;
        private Task _pump = Task.CompletedTask;
        private bool _addressConflictLogged;

        public EBusConnection(IEBusTransport transport, byte ownAddress, ILogger logger = null)
        {
            if (!EBusProtocol.IsMaster(ownAddress))
            {
                throw new ArgumentException($"0x{ownAddress:x2} is not an eBUS master address", nameof(ownAddress));
            }

            _transport = transport;
            OwnAddress = ownAddress;
            _logger = logger ?? NullLogger.Instance;
        }

        public byte OwnAddress { get; }

        /// <summary>
        /// Every valid telegram of the other devices. Called on the receive loop: keep it short.
        /// </summary>
        public event Action<EBusTelegram> TelegramReceived;

        /// <summary>
        /// Completes when the connection is closed or broken (faulted)
        /// </summary>
        public Task Completion { get; private set; } = Task.CompletedTask;

        public async Task StartAsync(CancellationToken token = default)
        {
            await _transport.ConnectAsync(token);
            _pump = Task.Run(() => PumpAsync(_cts.Token));
            Completion = Task.Run(() => LoopAsync(_cts.Token));
        }

        public async Task<EBusResult> RequestAsync(byte target, byte primary, byte secondary, byte[] data, CancellationToken token = default)
        {
            if (!EBusProtocol.IsSlave(target))
            {
                throw new ArgumentException($"0x{target:x2} is not a slave address", nameof(target));
            }

            if (Completion.IsCompleted)
            {
                throw new EBusException("The eBUS connection is closed");
            }

            var request = new PendingRequest(EBusProtocol.BuildMasterPart(OwnAddress, target, primary, secondary, data), DateTime.UtcNow + RequestTimeout);
            _requests.Enqueue(request);
            await using var _ = token.Register(() => request.Result.TrySetCanceled(token));
            return await request.Result.Task;
        }

        /// <returns>the raw register value, null if the device does not have it</returns>
        /// <exception cref="EBusException">the device did not answer</exception>
        public async Task<ushort?> ReadWolfRegisterAsync(byte target, ushort register, CancellationToken token = default)
        {
            var result = await RequestAsync(target, WolfRegister.Primary, WolfRegister.ReadSecondary, WolfRegister.ReadRequest(register), token);
            if (!result.IsOk)
            {
                throw new EBusException($"Register {register} of 0x{target:x2}: {result.Status}");
            }

            if (!WolfRegister.TryParseReadResponse(result.Response, out var value))
            {
                throw new EBusException($"Register {register} of 0x{target:x2}: unexpected answer {string.Join(" ", result.Response.Select(a => a.ToString("x2")))}");
            }

            return value;
        }

        private async Task PumpAsync(CancellationToken token)
        {
            var buffer = new byte[512];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var read = await _transport.ReadAsync(buffer, token);
                    if (read == 0)
                    {
                        break;
                    }

                    _chunks.Writer.TryWrite(buffer.AsSpan(0, read).ToArray());
                }

                _chunks.Writer.TryComplete(new EBusException("The eBUS adapter closed the connection"));
            }
            catch (Exception e)
            {
                _chunks.Writer.TryComplete(e);
            }
        }

        private bool HasBufferedBytes => _index < _chunk.Length || _chunks.Reader.TryPeek(out _);

        /// <returns>the next byte, -1 on timeout</returns>
        private async ValueTask<int> NextByteAsync(TimeSpan timeout, CancellationToken token)
        {
            while (_index >= _chunk.Length)
            {
                if (_chunks.Reader.TryRead(out var chunk))
                {
                    _chunk = chunk;
                    _index = 0;
                    continue;
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(timeout);
                try
                {
                    if (!await _chunks.Reader.WaitToReadAsync(timeoutCts.Token))
                    {
                        throw new EBusException("The eBUS connection is closed");
                    }
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    return -1;
                }
            }

            return _chunk[_index++];
        }

        private ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token) => _transport.WriteAsync(data, token);

        private async Task LoopAsync(CancellationToken token)
        {
            var previous = -1;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var b = await NextByteAsync(SilenceTimeout, token);
                    if (b < 0)
                    {
                        throw new EBusException($"No data from the eBUS adapter for {SilenceTimeout.TotalSeconds:0} s");
                    }

                    if (b != EBusProtocol.Syn)
                    {
                        _frame.Add((byte)b);
                        previous = b;
                        continue;
                    }

                    PublishFrame();
                    ExpireRequests();

                    if (previous == EBusProtocol.Syn && !HasBufferedBytes && _requests.TryPeek(out var request))
                    {
                        previous = await TransactAsync(request, token);
                        continue;
                    }

                    previous = b;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                while (_requests.TryDequeue(out var request))
                {
                    request.Result.TrySetException(new EBusException("The eBUS connection is closed"));
                }
            }
        }

        private void PublishFrame()
        {
            if (_frame.Count == 0)
            {
                return;
            }

            var telegram = EBusTelegram.TryParse(_frame.ToArray());
            _frame.Clear();
            if (telegram == null)
            {
                return;
            }

            if (telegram.Source == OwnAddress && !_addressConflictLogged)
            {
                _addressConflictLogged = true;
                _logger.LogWarning($"Another device on the bus uses the master address 0x{OwnAddress:x2} of this driver, choose a free one");
            }

            try
            {
                TelegramReceived?.Invoke(telegram);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error handling eBUS telegram {telegram}");
            }
        }

        private void ExpireRequests()
        {
            while (_requests.TryPeek(out var request) && (request.Result.Task.IsCompleted || request.Deadline < DateTime.UtcNow))
            {
                _requests.TryDequeue(out _);
                request.Result.TrySetResult(new EBusResult(EBusResultStatus.Timeout, null));
            }
        }

        private void Finish(PendingRequest request, EBusResultStatus status, byte[] response = null)
        {
            _requests.TryDequeue(out _);
            request.Result.TrySetResult(new EBusResult(status, response));
        }

        /// <summary>
        /// Called right after an idle SYN. Returns the last byte seen on the bus (the "previous" byte of the receive loop).
        /// </summary>
        private async Task<int> TransactAsync(PendingRequest request, CancellationToken token)
        {
            var master = request.MasterPart;
            request.Attempts++;
            var lastAttempt = request.Attempts >= MaxAttempts;

            await WriteAsync(master.AsMemory(0, 1), token);
            int echo;
            do
            {
                // the SYN generator can send the next SYN while our address is on its way (network latency):
                // our address then follows that SYN and still starts the arbitration
                echo = await NextByteAsync(ByteTimeout, token);
            } while (echo == EBusProtocol.Syn);

            if (echo != OwnAddress)
            {
                // another master started at the same time (or we were too late): its telegram goes on
                if (lastAttempt)
                {
                    Finish(request, EBusResultStatus.Busy);
                }

                if (echo >= 0 && echo != EBusProtocol.Syn)
                {
                    _frame.Add((byte)echo);
                }

                return echo;
            }

            await WriteAsync(master.AsMemory(1), token);
            for (var i = 1; i < master.Length; i++)
            {
                var b = await NextByteAsync(ByteTimeout, token);
                if (b != master[i])
                {
                    // collision in the middle of the telegram: give the bus back
                    if (lastAttempt)
                    {
                        Finish(request, EBusResultStatus.Busy);
                    }

                    return await GiveBackAsync(b, token);
                }
            }

            var ack = await NextByteAsync(ByteTimeout, token);
            if (ack == EBusProtocol.Syn)
            {
                // nobody answered, the SYN generator already released the bus
                Finish(request, EBusResultStatus.NoAnswer);
                return ack;
            }

            if (ack < 0)
            {
                Finish(request, EBusResultStatus.NoAnswer);
                return await GiveBackAsync(ack, token);
            }

            if (ack != EBusProtocol.Ack)
            {
                if (lastAttempt)
                {
                    Finish(request, EBusResultStatus.Failed);
                }

                return await GiveBackAsync(ack, token);
            }

            var length = await NextByteAsync(ByteTimeout, token);
            if (length is < 0 or > 16 || length == EBusProtocol.Syn)
            {
                if (lastAttempt)
                {
                    Finish(request, EBusResultStatus.Failed);
                }

                return await GiveBackAsync(length, token);
            }

            // NN data CRC, escaped
            var escaped = new List<byte>();
            byte[] body;
            while ((body = EBusProtocol.Unescape(escaped.ToArray())).Length < length + 1 || escaped[^1] == EBusProtocol.EscapeByte)
            {
                var b = await NextByteAsync(ByteTimeout, token);
                if (b < 0 || b == EBusProtocol.Syn)
                {
                    if (lastAttempt)
                    {
                        Finish(request, EBusResultStatus.Failed);
                    }

                    return await GiveBackAsync(b, token);
                }

                escaped.Add((byte)b);
            }

            var response = body.AsSpan(0, length).ToArray();
            var crcData = new byte[length + 1];
            crcData[0] = (byte)length;
            response.CopyTo(crcData, 1);
            if (EBusProtocol.Checksum(crcData) != body[length])
            {
                if (lastAttempt)
                {
                    Finish(request, EBusResultStatus.Failed);
                }

                return await GiveBackAsync(-1, token, EBusProtocol.Nack);
            }

            await WriteAsync(new[] { EBusProtocol.Ack, EBusProtocol.Syn }, token);
            Finish(request, EBusResultStatus.Ok, response);
            return -1;
        }

        /// <summary>
        /// Ends our telegram with a SYN (after an optional NACK) unless the bus already got one
        /// </summary>
        private async Task<int> GiveBackAsync(int last, CancellationToken token, byte? before = null)
        {
            if (last == EBusProtocol.Syn)
            {
                return last;
            }

            await WriteAsync(before.HasValue ? new[] { before.Value, EBusProtocol.Syn } : new[] { EBusProtocol.Syn }, token);
            return -1;
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            await _transport.DisposeAsync();
            try
            {
                await Task.WhenAll(_pump, Completion);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "eBUS connection closed with an error");
            }

            _cts.Dispose();
        }

        private sealed class PendingRequest
        {
            public PendingRequest(byte[] masterPart, DateTime deadline)
            {
                MasterPart = masterPart;
                Deadline = deadline;
            }

            public byte[] MasterPart { get; }
            public DateTime Deadline { get; }
            public int Attempts { get; set; }
            public TaskCompletionSource<EBusResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
