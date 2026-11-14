using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace P3.Driver.Tuya.Driver
{
    /// <summary>
    /// One frame of the Tuya local protocol: 000055AA | seq | cmd | length | payload | CRC32 (3.3) or HMAC-SHA256 (3.4) | 0000AA55
    /// </summary>
    internal sealed record TuyaFrame(uint Seq, uint Cmd, uint? RetCode, byte[] Payload);

    internal sealed class TuyaProtocolException : Exception
    {
        public TuyaProtocolException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Framing and encryption of the Tuya local protocol, versions 3.3 and 3.4 (the protocol of tinytuya / localtuya).
    /// 3.3: AES-128-ECB with the local key, CRC32, the version header "3.3" + 12 zero bytes in front of the encrypted payload.
    /// 3.4: AES-128-ECB with a session key negotiated from the local key, HMAC-SHA256, the version header inside the encrypted payload.
    /// </summary>
    internal static class TuyaProtocol
    {
        public const uint Prefix = 0x000055AA;
        public const uint Suffix = 0x0000AA55;
        public const int HeaderLength = 16;

        public const uint SessKeyNegStart = 3;
        public const uint SessKeyNegResp = 4;
        public const uint SessKeyNegFinish = 5;
        public const uint Control = 7;
        public const uint Status = 8;
        public const uint HeartBeat = 9;
        public const uint DpQuery = 10;
        public const uint ControlNew = 13;
        public const uint DpQueryNew = 16;

        /// <summary>
        /// Key of the UDP discovery broadcasts (port 6667) of all devices: md5("yGAdlopoPVldABfn")
        /// </summary>
        public static readonly byte[] DiscoveryKey = MD5.HashData(Encoding.ASCII.GetBytes("yGAdlopoPVldABfn"));

        /// <summary>
        /// Commands sent without the version header
        /// </summary>
        private static bool WithoutHeader(uint cmd) => cmd is DpQuery or DpQueryNew or HeartBeat or SessKeyNegStart or SessKeyNegResp or SessKeyNegFinish or Status;

        public static bool IsSupported(string version) => version is "3.3" or "3.4";

        private static byte[] VersionHeader(string version) => Encoding.ASCII.GetBytes(version).Concat(new byte[12]).ToArray();

        public static byte[] Encrypt(byte[] key, byte[] data, bool pad = true)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            return aes.EncryptEcb(data, pad ? PaddingMode.PKCS7 : PaddingMode.None);
        }

        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            return aes.DecryptEcb(data, PaddingMode.PKCS7);
        }

        /// <summary>
        /// A frame to send: payload encrypted with <paramref name="key"/>, with CRC32 (3.3) or HMAC (3.4)
        /// </summary>
        public static byte[] Encode(string version, byte[] key, uint seq, uint cmd, byte[] payload)
        {
            byte[] body;
            if (version == "3.4")
            {
                body = Encrypt(key, WithoutHeader(cmd) ? payload : VersionHeader(version).Concat(payload).ToArray());
            }
            else
            {
                body = Encrypt(key, payload);
                if (!WithoutHeader(cmd))
                {
                    body = VersionHeader(version).Concat(body).ToArray();
                }
            }

            var trailer = version == "3.4" ? 32 : 4;
            var frame = new byte[HeaderLength + body.Length + trailer + 4];
            BinaryPrimitives.WriteUInt32BigEndian(frame, Prefix);
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(4), seq);
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(8), cmd);
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(12), (uint)(body.Length + trailer + 4));
            body.CopyTo(frame, HeaderLength);
            var signed = frame.AsSpan(0, HeaderLength + body.Length);
            if (version == "3.4")
            {
                HMACSHA256.HashData(key, signed).CopyTo(frame, HeaderLength + body.Length);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(HeaderLength + body.Length), Crc32(signed));
            }
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(frame.Length - 4), Suffix);
            return frame;
        }

        /// <summary>
        /// Length of the rest of the frame after the 16 header bytes
        /// </summary>
        public static int RestLength(ReadOnlySpan<byte> header)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(header) != Prefix)
            {
                throw new TuyaProtocolException($"no Tuya frame (prefix {Convert.ToHexString(header[..4])})");
            }
            var length = BinaryPrimitives.ReadUInt32BigEndian(header[12..]);
            if (length is < 8 or > 64 * 1024)
            {
                throw new TuyaProtocolException($"bad frame length {length}");
            }
            return (int)length;
        }

        /// <summary>
        /// Checks and decrypts a received frame (header + rest). Frames of the device start with a 4 byte return code.
        /// </summary>
        public static TuyaFrame Decode(string version, byte[] key, byte[] header, byte[] rest)
        {
            var seq = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
            var cmd = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8));
            var trailer = version == "3.4" ? 32 : 4;
            if (rest.Length < trailer + 4 || BinaryPrimitives.ReadUInt32BigEndian(rest.AsSpan(rest.Length - 4)) != Suffix)
            {
                throw new TuyaProtocolException("bad frame end");
            }

            var data = rest.AsSpan(0, rest.Length - trailer - 4).ToArray();
            var signed = header.Concat(data).ToArray();
            var check = rest.AsSpan(data.Length, trailer);
            if (version == "3.4")
            {
                if (!CryptographicOperations.FixedTimeEquals(check, HMACSHA256.HashData(key, signed)))
                {
                    throw new TuyaProtocolException("HMAC mismatch (wrong local key?)");
                }
            }
            else if (BinaryPrimitives.ReadUInt32BigEndian(check) != Crc32(signed))
            {
                throw new TuyaProtocolException("CRC mismatch");
            }

            uint? retCode = null;
            // a return code is there when what follows it lines up with the cipher blocks (with or without the 3.3 header)
            if (data.Length >= 4 && (data.Length % 16 == 4 || HasVersionHeader(data.AsSpan(4)) && (data.Length - 19) % 16 == 0))
            {
                retCode = BinaryPrimitives.ReadUInt32BigEndian(data);
                data = data[4..];
            }

            if (version != "3.4" && HasVersionHeader(data))
            {
                data = data[15..];
            }
            var plain = data.Length == 0 ? data : Decrypt(key, data);
            if (version == "3.4" && HasVersionHeader(plain))
            {
                plain = plain[15..];
            }
            return new TuyaFrame(seq, cmd, retCode, plain);
        }

        private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
        {
            var c = (uint)n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            return c;
        }).ToArray();

        /// <summary>
        /// CRC-32 (IEEE, as zlib) of the 3.3 frames
        /// </summary>
        internal static uint Crc32(ReadOnlySpan<byte> data)
        {
            var crc = 0xFFFFFFFF;
            foreach (var b in data)
            {
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return ~crc;
        }

        private static bool HasVersionHeader(ReadOnlySpan<byte> data) => data.Length >= 15 && data[0] == (byte)'3' && data[1] == (byte)'.' && data[2] is >= (byte)'0' and <= (byte)'9';

        /// <summary>
        /// Session key of 3.4: AES-ECB(local key, local nonce XOR remote nonce), no padding
        /// </summary>
        public static byte[] SessionKey(byte[] localKey, byte[] localNonce, byte[] remoteNonce) =>
            Encrypt(localKey, localNonce.Zip(remoteNonce, (a, b) => (byte)(a ^ b)).ToArray(), false);

        /// <summary>
        /// The JSON of a discovery broadcast (UDP 6666 plain, 6667 encrypted with <see cref="DiscoveryKey"/>), null when it is none
        /// </summary>
        public static JsonElement? DecodeDiscovery(byte[] packet)
        {
            if (packet.Length < 28 || BinaryPrimitives.ReadUInt32BigEndian(packet) != Prefix)
            {
                return null;
            }
            var payload = packet.AsSpan(20, packet.Length - 28).ToArray();
            try
            {
                var json = payload.Length > 0 && payload[0] == (byte)'{' ? payload : Decrypt(DiscoveryKey, payload);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException)
            {
                return null;
            }
        }
    }
}
