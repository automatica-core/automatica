using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver.Utility;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.Data;

namespace P3.Driver.EnOcean
{
    public abstract class BaseStream
    {
        /// <summary>
        /// Bytes already taken from the wire that have to be scanned again (resync after a bad header CRC).
        /// </summary>
        private readonly Queue<byte> _pending = new Queue<byte>();

        /// <summary>
        /// ESP3: the stick answers every command within 500 ms.
        /// </summary>
        public static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(1);

        // one command at a time, its RESPONSE comes through the normal receive path
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);
        private readonly object _responseLock = new object();
        private TaskCompletionSource<EnOceanPacket> _response;

        public abstract event EventHandler<PacketReceivedEventArgs> TelegramReceived;

        public abstract Task<bool> Open();
        public abstract Task<bool> Close();
        public abstract Task<EnOceanPacket> WriteFrame(EnOceanPacket frame);

        public abstract void Pause();
        public abstract void Continue();

        protected int PendingCount => _pending.Count;

        /// <summary>
        /// Sends a command with <paramref name="write"/> and waits for its RESPONSE, which the receive path hands over
        /// with <see cref="TryCompleteResponse"/>. Only the receive path reads from the wire, so a send started while a
        /// telegram is being handled (the reply to a valve) does not race it for the bytes of the answer.
        /// </summary>
        /// <returns>the RESPONSE, or null when the stick did not answer within <paramref name="timeout"/></returns>
        protected async Task<EnOceanPacket> SendAndWaitForResponse(Func<Task> write, TimeSpan timeout)
        {
            await _requestLock.WaitAsync();
            var response = new TaskCompletionSource<EnOceanPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                lock (_responseLock)
                {
                    _response = response;
                }

                await write();

                var done = await Task.WhenAny(response.Task, Task.Delay(timeout));
                if (done != response.Task)
                {
                    Logger.Logger.Instance.LogWarning($"No response from the EnOcean stick within {timeout.TotalMilliseconds} ms");
                    return null;
                }
                return await response.Task;
            }
            finally
            {
                lock (_responseLock)
                {
                    if (_response == response)
                    {
                        _response = null;
                    }
                }
                _requestLock.Release();
            }
        }

        /// <summary>
        /// Hands a received RESPONSE to the command waiting for it.
        /// </summary>
        /// <returns>true when a command took it, false for every other packet and for a RESPONSE nobody waits for</returns>
        protected bool TryCompleteResponse(EnOceanPacket packet)
        {
            if (packet == null || packet.PacketType != EnOcean.PacketType.Response)
            {
                return false;
            }

            TaskCompletionSource<EnOceanPacket> response;
            lock (_responseLock)
            {
                response = _response;
                _response = null;
            }

            return response != null && response.TrySetResult(packet);
        }

        protected bool TryTakePending(out byte value)
        {
            if (_pending.Count > 0)
            {
                value = _pending.Dequeue();
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Copies pending bytes into <paramref name="buffer"/>, returns how many were copied.
        /// </summary>
        protected int TakePending(Span<byte> buffer)
        {
            var read = 0;
            while (read < buffer.Length && _pending.Count > 0)
            {
                buffer[read++] = _pending.Dequeue();
            }
            return read;
        }

        /// <summary>
        /// Checks the ESP3 header CRC8H (over data length, optional length and packet type).
        /// On mismatch the bytes after the sync byte are queued again, so the next read resyncs on the next 0x55
        /// instead of trusting a bogus length.
        /// </summary>
        /// <param name="header">sync byte, data length (2), optional length, packet type, CRC8H</param>
        protected bool CheckHeaderCrc(ReadOnlySpan<byte> header)
        {
            var crc = EnOcean.ComputeCrc8(header.Slice(1, 4));
            if (crc == header[5])
            {
                return true;
            }

            Logger.Logger.Instance.LogWarning($"Invalid ESP3 header crc {header[5]:X2} (expected {crc:X2}), resync");

            var rest = _pending.ToArray();
            _pending.Clear();
            foreach (var b in header.Slice(1))
            {
                _pending.Enqueue(b);
            }
            foreach (var b in rest)
            {
                _pending.Enqueue(b);
            }
            return false;
        }

        /// <summary>
        /// Checks the ESP3 data CRC8D (over data and optional data).
        /// </summary>
        protected static bool CheckDataCrc(ReadOnlySpan<byte> header, ReadOnlySpan<byte> data, ReadOnlySpan<byte> optData, byte crcData)
        {
            var crc = EnOcean.ComputeCrc8(optData, EnOcean.ComputeCrc8(data));
            if (crc == crcData)
            {
                return true;
            }

            Logger.Logger.Instance.LogWarning($"Invalid ESP3 data crc {crcData:X2} (expected {crc:X2}), dropping frame " +
                                              $"{Utils.ByteArrayToString(header.ToArray())} {Utils.ByteArrayToString(data.ToArray())} {Utils.ByteArrayToString(optData.ToArray())}");
            return false;
        }
    }
}
