using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    /// <summary>
    /// Read-modify-write of inverter holding registers. Only registers that are on the allow list and only the bits in
    /// the allowed mask can change; everything else in a register is written back as it was read.
    /// </summary>
    internal static class SolarmanRegisterWriter
    {
        /// <summary>
        /// Reads <paramref name="count"/> registers from <paramref name="start"/>, applies <paramref name="update"/>
        /// (index in the range, current value) and writes the registers that changed with one FC16 request (Deye loggers
        /// do not accept FC6). The range is read again afterwards and returned, so callers dispatch what the inverter holds.
        /// </summary>
        /// <exception cref="InvalidOperationException">The update would write a register or a bit that is not allowed. Nothing is written.</exception>
        /// <exception cref="IOException">Reading, writing or verifying failed; a retry is safe because the write is idempotent.</exception>
        public static async Task<ushort[]> ReadModifyWrite(IModBusMasterDriver driver, byte deviceId, ushort start, int count,
            Func<int, ushort, ushort> update, ushort allowedMask, IReadOnlySet<int> writableRegisters, CancellationToken token = default)
        {
            if (driver == null)
            {
                throw new IOException("The connection to the Solarman logger is not open");
            }

            var current = await ReadRange(driver, deviceId, start, count, token);

            var next = new ushort[count];
            var firstChanged = -1;
            var lastChanged = -1;
            for (var i = 0; i < count; i++)
            {
                next[i] = update(i, current[i]);
                if (next[i] == current[i])
                {
                    continue;
                }

                var register = start + i;
                if (!writableRegisters.Contains(register))
                {
                    throw new InvalidOperationException($"Register {register} (0x{register:X4}) is not writable");
                }

                if (!SolarmanRegisterCodec.ChangesOnlyBits(current[i], next[i], allowedMask))
                {
                    throw new InvalidOperationException(
                        $"Writing 0x{next[i]:X4} to register {register} (0x{register:X4}, now 0x{current[i]:X4}) would change bits outside 0x{allowedMask:X4}");
                }

                if (firstChanged < 0)
                {
                    firstChanged = i;
                }
                lastChanged = i;
            }

            if (firstChanged < 0)
            {
                // the inverter already holds the requested state
                return current;
            }

            // registers between two changed ones are written back with the value just read
            var values = next.Skip(firstChanged).Take(lastChanged - firstChanged + 1).ToArray();
            for (var i = firstChanged; i <= lastChanged; i++)
            {
                if (!writableRegisters.Contains(start + i))
                {
                    throw new InvalidOperationException($"Register {start + i} (0x{start + i:X4}) is not writable");
                }
            }

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                var result = await driver.WriteMultipleHoldingRegisters(deviceId, (ushort)(start + firstChanged), values, cts.Token);
                if (result == null || result.ModBusRequestStatus != ModBusRequestStatus.Success)
                {
                    throw new IOException($"Writing registers {start + firstChanged}-{start + lastChanged} failed: {Describe(result)}");
                }
            }

            var readBack = await ReadRange(driver, deviceId, start, count, token);
            for (var i = firstChanged; i <= lastChanged; i++)
            {
                if (readBack[i] != next[i])
                {
                    throw new IOException(
                        $"Register {start + i} (0x{start + i:X4}) reads 0x{readBack[i]:X4} after writing 0x{next[i]:X4}");
                }
            }

            return readBack;
        }

        private static async Task<ushort[]> ReadRange(IModBusMasterDriver driver, byte deviceId, ushort start, int count, CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var result = await driver.ReadRegisters(deviceId, start, count, cts.Token);
            if (result is ModBusRegisterValueReturn { ModBusRequestStatus: ModBusRequestStatus.Success } values && values.Data.Count >= count)
            {
                return values.Data.Take(count).ToArray();
            }

            throw new IOException($"Reading registers {start}-{start + count - 1} failed: {Describe(result)}");
        }

        private static string Describe(ModBusReturn result)
        {
            return result switch
            {
                null => "no response",
                ModBusExceptionReturn exceptionReturn => $"exception {exceptionReturn}",
                _ => result.ModBusRequestStatus.ToString()
            };
        }
    }
}
