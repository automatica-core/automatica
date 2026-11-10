using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    /// <summary>
    /// Converts raw inverter registers to values and back. Kept free of driver state so it can be unit tested.
    /// </summary>
    internal static class SolarmanRegisterCodec
    {
        /// <summary>
        /// Deye time of use "charge" registers (172-177): bit 0 = charge from the grid, bit 1 = charge from the generator.
        /// </summary>
        public const ushort GridChargeBit = 0x0001;
        public const ushort GeneratorChargeBit = 0x0002;

        /// <summary>
        /// Decodes a 16 bit register. Signed registers (rule 2 in the ha-solarman maps) are two's complement int16.
        /// The offset is subtracted before scaling, like the ha-solarman maps define it (e.g. temperatures: (raw - 1000) * 0.1).
        /// </summary>
        public static object Decode16(ushort raw, bool signed, int offset, double scale)
        {
            int val = signed ? unchecked((short)raw) : raw;
            return ApplyOffsetAndScale(val, offset, scale);
        }

        /// <summary>
        /// Decodes a 32 bit value stored low word first (Deye energy counters).
        /// </summary>
        public static object Decode32(ushort lowWord, ushort highWord, bool signed, int offset, double scale)
        {
            var raw = lowWord | ((uint)highWord << 16);
            long val = signed ? unchecked((int)raw) : raw;
            return ApplyOffsetAndScale(val, offset, scale);
        }

        private static object ApplyOffsetAndScale(long val, int offset, double scale)
        {
            if (offset > 0)
            {
                val -= offset;
            }

            if (scale > 1.0 || scale < 1.0)
            {
                return Math.Round(val * scale, 2);
            }

            if (val is >= int.MinValue and <= int.MaxValue)
            {
                return (int)val;
            }

            return val;
        }

        /// <summary>
        /// Deye device state (register 500): 0 standby, 1 self-test, 2 normal, 3 alarm, 4 fault.
        /// </summary>
        public const ushort DeviceStateNormal = 2;

        /// <summary>
        /// Deye device relay status (register 552): bit 0 inverter relay, bit 2 grid relay, bit 3 generator relay.
        /// </summary>
        public const ushort GridRelayBit = 0x0004;

        /// <summary>
        /// Deye generator port mode (register 133): 0 generator input, 1 SmartLoad output, 2 micro-inverter input.
        /// </summary>
        public const ushort GeneratorPortSmartLoad = 1;

        public static bool IsDeviceStateNormal(ushort raw) => raw == DeviceStateNormal;

        public static int GridRelayClosed(ushort raw) => (raw & GridRelayBit) != 0 ? 1 : 0;

        public static bool IsSmartLoadOutput(ushort raw) => raw == GeneratorPortSmartLoad;

        public static bool IsGridChargeEnabled(ushort raw)
        {
            return (raw & GridChargeBit) != 0;
        }

        /// <summary>
        /// True as soon as one time of use slot charges from the grid.
        /// </summary>
        public static bool IsGridChargeEnabledInAnySlot(IEnumerable<ushort> raw)
        {
            return raw.Any(IsGridChargeEnabled);
        }

        /// <summary>
        /// Sets or clears only the grid charge bit, every other bit (generator charge, reserved bits) stays as it is.
        /// </summary>
        public static ushort WithGridCharge(ushort raw, bool enabled)
        {
            return enabled ? (ushort)(raw | GridChargeBit) : (ushort)(raw & ~GridChargeBit);
        }

        /// <summary>
        /// True if <paramref name="next"/> differs from <paramref name="current"/> in no other bit than the ones in <paramref name="allowedMask"/>.
        /// </summary>
        public static bool ChangesOnlyBits(ushort current, ushort next, ushort allowedMask)
        {
            return ((current ^ next) & ~allowedMask & 0xFFFF) == 0;
        }

        /// <summary>
        /// Converts a value written to a boolean data point (bool, 0/1, "true"/"false", "on"/"off").
        /// </summary>
        public static bool ToBool(object value)
        {
            switch (value)
            {
                case null:
                    throw new ArgumentNullException(nameof(value));
                case bool b:
                    return b;
                case string s:
                {
                    var trimmed = s.Trim();
                    if (bool.TryParse(trimmed, out var parsed))
                    {
                        return parsed;
                    }

                    if (trimmed.Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (trimmed.Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    {
                        return number != 0;
                    }

                    throw new FormatException($"Cannot convert \"{s}\" to a boolean");
                }
                case IConvertible convertible:
                    return convertible.ToDouble(CultureInfo.InvariantCulture) != 0;
                default:
                    throw new FormatException($"Cannot convert {value.GetType().Name} to a boolean");
            }
        }
    }
}
