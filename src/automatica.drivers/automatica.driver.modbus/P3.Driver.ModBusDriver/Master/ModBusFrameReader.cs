using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.ModBusDriver.Master
{
    /// <summary>
    /// Exact frame reads on a stream: a single Read may return only a part of a frame.
    /// </summary>
    internal static class ModBusFrameReader
    {
        public const int RtuExceptionFrameLength = 5; // slave, function | 0x80, exception code, crc (2)

        /// <summary>
        /// Length of a complete RTU response (slave id, function, data, crc)
        /// </summary>
        public static int GetRtuResponseLength(ModBusFunction function, int numberOfRegisters)
        {
            var readLength = 5;

            switch (function)
            {
                case ModBusFunction.ReadInputRegisters:
                case ModBusFunction.ReadHoldingRegisters:
                    readLength += numberOfRegisters * 2;
                    break;
                case ModBusFunction.ReadDiscreteInputs:
                case ModBusFunction.ReadCoils:
                    readLength += (numberOfRegisters + 7) / 8;
                    break;
                case ModBusFunction.WriteMultipleRegisters:
                case ModBusFunction.WriteSingleRegister:
                case ModBusFunction.WriteMultipleCoils:
                case ModBusFunction.WriteSingleCoil:
                    readLength += 3;
                    break;
            }

            return readLength;
        }

        /// <summary>
        /// Reads one RTU response until its expected length (or the exception reply length) is received, then checks the crc.
        /// A read returning 0 bytes (serial read timeout) is retried until <paramref name="token"/> is cancelled.
        /// </summary>
        /// <returns>the complete frame including the crc, an exception reply (function | 0x80) is returned as 5 byte frame</returns>
        /// <exception cref="TimeoutException">the frame was not received completely in time</exception>
        /// <exception cref="InvalidDataException">invalid crc</exception>
        public static async Task<byte[]> ReadRtuFrameAsync(Stream stream, ModBusFunction function, int numberOfRegisters, CancellationToken token)
        {
            var expectedLength = GetRtuResponseLength(function, numberOfRegisters);
            var buffer = new byte[Math.Max(expectedLength, RtuExceptionFrameLength)];
            var received = 0;

            while (received < expectedLength)
            {
                if (token.IsCancellationRequested)
                {
                    throw new TimeoutException($"ModBus rtu frame incomplete, received {received} of {expectedLength} bytes");
                }

                int read;
                try
                {
                    read = await stream.ReadAsync(buffer, received, expectedLength - received, token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"ModBus rtu frame incomplete, received {received} of {expectedLength} bytes");
                }
                catch (TimeoutException)
                {
                    read = 0;
                }

                if (read <= 0)
                {
                    try
                    {
                        await Task.Delay(5, token);
                    }
                    catch (OperationCanceledException)
                    {
                        // checked at the start of the loop
                    }
                    continue;
                }

                received += read;

                if (received >= 2 && (buffer[1] & 0x80) != 0)
                {
                    // exception reply, it is shorter than the expected response
                    expectedLength = RtuExceptionFrameLength;
                }
            }

            var frame = buffer.Take(expectedLength).ToArray();

            var readCrc = frame.Skip(frame.Length - 2);
            var calcCrc = ModbusUtility.CalculateCrc(frame.SkipLast(2).ToArray());
            if (!readCrc.SequenceEqual(calcCrc))
            {
                throw new InvalidDataException($"Invalid crc in ModBus rtu frame {BitConverter.ToString(frame)}");
            }

            return frame;
        }

        /// <summary>
        /// Fills <paramref name="buffer"/> completely
        /// </summary>
        /// <returns>false if the remote side closed the stream (a read returned 0)</returns>
        public static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken token)
        {
            var received = 0;
            while (received < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer, received, buffer.Length - received, token);
                if (read == 0)
                {
                    return false;
                }

                received += read;
            }

            return true;
        }
    }
}
