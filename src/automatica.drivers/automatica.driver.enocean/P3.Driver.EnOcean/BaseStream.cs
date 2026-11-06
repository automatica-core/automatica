using System;
using System.Collections.Generic;
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

        public abstract event EventHandler<PacketReceivedEventArgs> TelegramReceived;

        public abstract Task<bool> Open();
        public abstract Task<bool> Close();
        public abstract Task<EnOceanPacket> WriteFrame(EnOceanPacket frame);

        public abstract void Pause();
        public abstract void Continue();

        protected int PendingCount => _pending.Count;

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
