using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace P3.Driver.Matter.Commissioning
{
    /// <summary>Onboarding payloads (spec 5.1): the QR code text and the 11-digit manual pairing code.</summary>
    public static class SetupPayload
    {
        public const byte DiscoveryCapabilitySoftAp = 0x01;
        public const byte DiscoveryCapabilityBle = 0x02;
        public const byte DiscoveryCapabilityOnNetwork = 0x04;

        private const string Base38Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-.";

        private static readonly uint[] InvalidPasscodes =
        {
            0, 11111111, 22222222, 33333333, 44444444, 55555555, 66666666, 77777777, 88888888, 99999999, 12345678, 87654321
        };

        public static bool IsValidPasscode(uint passcode) => passcode is >= 1 and <= 99999998 && !InvalidPasscodes.Contains(passcode);

        public static uint GeneratePasscode()
        {
            while (true)
            {
                var p = (uint)RandomNumberGenerator.GetInt32(1, 99999999);
                if (IsValidPasscode(p))
                {
                    return p;
                }
            }
        }

        public static string ManualPairingCode(uint passcode, ushort discriminator)
        {
            var shortDiscriminator = (discriminator >> 8) & 0x0F;
            var chunk1 = shortDiscriminator >> 2;
            var chunk2 = ((shortDiscriminator & 0x03) << 14) | (int)(passcode & 0x3FFF);
            var chunk3 = passcode >> 14;
            var digits = $"{chunk1}{chunk2:D5}{chunk3:D4}";
            return digits + Verhoeff.Compute(digits);
        }

        /// <summary>Formats a manual code as 4-3-4 like Matter labels do.</summary>
        public static string FormatManualCode(string code) => code.Length == 11 ? $"{code[..4]}-{code[4..7]}-{code[7..]}" : code;

        public static string QrCode(ushort vendorId, ushort productId, uint passcode, ushort discriminator,
            byte discoveryCapabilities = DiscoveryCapabilityOnNetwork, byte commissioningFlow = 0)
        {
            BigInteger bits = 0;
            var offset = 0;

            void Put(ulong value, int width)
            {
                bits |= new BigInteger(value & ((1UL << width) - 1)) << offset;
                offset += width;
            }

            Put(0, 3);
            Put(vendorId, 16);
            Put(productId, 16);
            Put(commissioningFlow, 2);
            Put(discoveryCapabilities, 8);
            Put(discriminator, 12);
            Put(passcode, 27);
            Put(0, 4);

            var bytes = new byte[11];
            var raw = bits.ToByteArray(isUnsigned: true, isBigEndian: false);
            Array.Copy(raw, bytes, Math.Min(raw.Length, bytes.Length));
            return "MT:" + Base38Encode(bytes);
        }

        public static string Base38Encode(byte[] data)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < data.Length; i += 3)
            {
                var len = Math.Min(3, data.Length - i);
                uint value = 0;
                for (var j = len - 1; j >= 0; j--)
                {
                    value = (value << 8) | data[i + j];
                }

                var chars = len switch { 3 => 5, 2 => 4, _ => 2 };
                for (var c = 0; c < chars; c++)
                {
                    sb.Append(Base38Alphabet[(int)(value % 38)]);
                    value /= 38;
                }
            }

            return sb.ToString();
        }
    }

    internal static class Verhoeff
    {
        private static readonly int[,] D =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 }, { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
            { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 }, { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 }, { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
            { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 }, { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 }, { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
            { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }
        };

        private static readonly int[,] P =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 }, { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 },
            { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 }, { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 }, { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
            { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 }, { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 }
        };

        private static readonly int[] Inv = { 0, 4, 3, 2, 1, 5, 6, 7, 8, 9 };

        public static char Compute(string digits)
        {
            var c = 0;
            for (var i = 0; i < digits.Length; i++)
            {
                var digit = digits[digits.Length - 1 - i] - '0';
                c = D[c, P[(i + 1) % 8, digit]];
            }

            return (char)('0' + Inv[c]);
        }
    }
}
