using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace P3.Logic.Surveillance.Sia
{
    public enum SiaFormat
    {
        /// <summary>
        /// SIA DC-04 ("SIA-DCS"), free text allowed
        /// </summary>
        SiaDcs = 0,

        /// <summary>
        /// Contact ID DC-05 ("ADM-CID"), event and zone only
        /// </summary>
        ContactId = 1
    }

    public enum SiaResponseKind
    {
        Ack,
        Nak,
        Duh,
        Invalid
    }

    public sealed record SiaAccount(string Account, string Prefix, string Receiver);

    public sealed record SiaResponse(SiaResponseKind Kind, int Sequence, string Detail);

    /// <summary>
    /// SIA DC-09 frames (ANSI/SIA DC-09-2013): &lt;LF&gt;CRC 0LLL "ID" seq Rrcvr Lpref #acct [data]_HH:MM:SS,MM-DD-YYYY &lt;CR&gt;.
    /// With a key the part after "[" is AES-CBC encrypted (zero IV) and hex encoded, the ID gets a "*".
    /// </summary>
    public static class SiaProtocol
    {
        private const string PadChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// CRC-16/ARC (poly 0xA001 reflected, init 0) as used by DC-09
        /// </summary>
        public static ushort Crc16(string text)
        {
            ushort crc = 0;
            foreach (var b in Encoding.ASCII.GetBytes(text))
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }
            }
            return crc;
        }

        public static string Timestamp(DateTimeOffset time) =>
            time.UtcDateTime.ToString("_HH:mm:ss,MM-dd-yyyy", CultureInfo.InvariantCulture);

        /// <summary>
        /// 0, 32, 48 or 64 hex characters; null if invalid
        /// </summary>
        public static byte[] ParseKey(string hex)
        {
            hex = (hex ?? "").Trim();
            if (hex.Length == 0)
            {
                return Array.Empty<byte>();
            }
            if (!(hex.Length is 32 or 48 or 64) || !Regex.IsMatch(hex, "^[0-9a-fA-F]+$"))
            {
                return null;
            }
            return Convert.FromHexString(hex);
        }

        /// <summary>
        /// The message for the data block: SIA DCS "FA01^Kitchen" → "NFA01^Kitchen" (new event), Contact ID "1130 01 015" stays
        /// </summary>
        public static string EventData(SiaFormat format, string message)
        {
            message = (message ?? "").Trim();
            if (format == SiaFormat.ContactId)
            {
                var digits = Regex.Replace(message, "\\s", "");
                if (!Regex.IsMatch(digits, "^[136][0-9A-Fa-f]{3}[0-9A-Fa-f]{2}[0-9A-Fa-f]{3}$"))
                {
                    throw new FormatException("contact id needs Q EEE GG CCC, e.g. 1130 01 015");
                }
                return $"{digits.Substring(0, 4)} {digits.Substring(4, 2)} {digits.Substring(6, 3)}";
            }

            if (!Regex.IsMatch(message, "^[A-Z]{2}[0-9]{0,4}(\\^.*)?$"))
            {
                throw new FormatException("SIA event needs two upper case letters and a zone, e.g. FA01^Kitchen");
            }
            if (message.IndexOfAny(new[] { '[', ']', '|', '\r', '\n' }) >= 0)
            {
                throw new FormatException("SIA event must not contain [ ] | or line breaks");
            }
            return "N" + message;
        }

        /// <summary>
        /// An event frame; <paramref name="data"/> from <see cref="EventData"/>, null for a supervision (NULL) message
        /// </summary>
        public static string BuildFrame(SiaFormat format, SiaAccount account, int sequence, string data, DateTimeOffset time, byte[] key, Func<int, string> padding = null)
        {
            var encrypted = key is { Length: > 0 };
            var id = data == null ? "NULL" : format == SiaFormat.ContactId ? "ADM-CID" : "SIA-DCS";

            var header = new StringBuilder();
            header.Append('"').Append(encrypted ? "*" : "").Append(id).Append('"');
            header.Append((sequence % 10000).ToString("0000", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(account.Receiver))
            {
                header.Append('R').Append(account.Receiver.ToUpperInvariant());
            }
            header.Append('L').Append(string.IsNullOrEmpty(account.Prefix) ? "0" : account.Prefix.ToUpperInvariant());
            header.Append('#').Append(account.Account.ToUpperInvariant());
            header.Append('[');

            var inner = (data == null ? "" : $"#{account.Account.ToUpperInvariant()}|{data}") + "]" + Timestamp(time);
            var body = encrypted ? Encrypt(inner, key, padding) : inner;

            return Wrap(header + body);
        }

        private static string Wrap(string content)
        {
            var crc = Crc16(content).ToString("X4", CultureInfo.InvariantCulture);
            var length = content.Length.ToString("X3", CultureInfo.InvariantCulture);
            return $"\n{crc}0{length}{content}\r";
        }

        /// <summary>
        /// "pad|" + text, padded to the block size, AES-CBC with a zero IV, upper case hex
        /// </summary>
        public static string Encrypt(string text, byte[] key, Func<int, string> padding = null)
        {
            var padLength = 16 - (text.Length + 1) % 16;
            if (padLength == 16)
            {
                padLength = 0;
            }
            var pad = (padding ?? RandomPad)(padLength);
            var plain = Encoding.ASCII.GetBytes(pad + "|" + text);

            using var aes = Aes.Create();
            aes.Key = key;
            return Convert.ToHexString(aes.EncryptCbc(plain, new byte[16], PaddingMode.None));
        }

        public static string Decrypt(string hex, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            var plain = Encoding.ASCII.GetString(aes.DecryptCbc(Convert.FromHexString(hex), new byte[16], PaddingMode.None));
            var bar = plain.IndexOf('|');
            return bar >= 0 ? plain.Substring(bar + 1) : plain;
        }

        private static string RandomPad(int length)
        {
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = PadChars[RandomNumberGenerator.GetInt32(PadChars.Length)];
            }
            return new string(chars);
        }

        private static readonly Regex FrameRegex = new(
            "^\\n?(?<crc>[0-9A-Fa-f]{4})0(?<len>[0-9A-Fa-f]{3})(?<content>\"(?<enc>\\*)?(?<id>[A-Z\\-]+)\"(?<seq>[0-9]{4}).*?\\[(?<body>[^\\r]*))\\r?$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Parses the receiver's answer (ACK, NAK or DUH) and checks CRC and length
        /// </summary>
        public static SiaResponse ParseResponse(string frame, byte[] key)
        {
            if (string.IsNullOrEmpty(frame))
            {
                return new SiaResponse(SiaResponseKind.Invalid, -1, "no answer");
            }

            var match = FrameRegex.Match(frame.TrimEnd('\0'));
            if (!match.Success)
            {
                return new SiaResponse(SiaResponseKind.Invalid, -1, "malformed answer");
            }

            var content = match.Groups["content"].Value;
            if (Crc16(content) != Convert.ToUInt16(match.Groups["crc"].Value, 16))
            {
                return new SiaResponse(SiaResponseKind.Invalid, -1, "CRC error");
            }
            if (content.Length != Convert.ToInt32(match.Groups["len"].Value, 16))
            {
                return new SiaResponse(SiaResponseKind.Invalid, -1, "length error");
            }

            var sequence = int.Parse(match.Groups["seq"].Value, CultureInfo.InvariantCulture);
            var detail = match.Groups["body"].Value;
            if (match.Groups["enc"].Success && key is { Length: > 0 } && detail.Length > 0 && Regex.IsMatch(detail, "^[0-9A-Fa-f]+$") && detail.Length % 32 == 0)
            {
                try
                {
                    detail = Decrypt(detail, key);
                }
                catch (CryptographicException)
                {
                    return new SiaResponse(SiaResponseKind.Invalid, sequence, "cannot decrypt answer");
                }
            }

            var kind = match.Groups["id"].Value switch
            {
                "ACK" => SiaResponseKind.Ack,
                "NAK" => SiaResponseKind.Nak,
                "DUH" => SiaResponseKind.Duh,
                _ => SiaResponseKind.Invalid
            };
            return new SiaResponse(kind, sequence, detail);
        }

        /// <summary>
        /// The receiver side, for tests and a local test receiver: an ACK for the given sequence
        /// </summary>
        public static string BuildAck(SiaAccount account, int sequence, DateTimeOffset time, byte[] key)
        {
            var encrypted = key is { Length: > 0 };
            var header = $"\"{(encrypted ? "*" : "")}ACK\"{sequence:0000}" +
                         (string.IsNullOrEmpty(account.Receiver) ? "" : "R" + account.Receiver) +
                         "L" + (string.IsNullOrEmpty(account.Prefix) ? "0" : account.Prefix) + "#" + account.Account + "[";
            var body = encrypted ? Encrypt("]" + Timestamp(time), key) : "]";
            return Wrap(header + body);
        }

        /// <summary>
        /// "host:port" → (host, port); the default port is 12300 (none is standardised)
        /// </summary>
        public static (string Host, int Port)? ParseEndpoint(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0)
            {
                return null;
            }

            var colon = value.LastIndexOf(':');
            if (colon > 0 && int.TryParse(value.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is > 0 and < 65536)
            {
                return (value.Substring(0, colon), port);
            }
            return (value, 12300);
        }
    }
}
