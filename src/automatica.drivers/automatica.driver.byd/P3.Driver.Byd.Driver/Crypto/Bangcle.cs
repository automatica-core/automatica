using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace P3.Driver.Byd.Driver.Crypto
{
    /// <summary>
    /// The outer "Bangcle" envelope of the BYD app API: white-box AES-CBC (zero IV, PKCS#7) with lookup tables extracted from
    /// the app's libencrypt.so, "F" + base64. Port of pyBYD's _crypto/_bangcle_block.py and bangcle.py (MIT, Joel Kåberg).
    /// </summary>
    internal sealed class Bangcle
    {
        private const int HeaderSize = 8;
        private static readonly int[] TableSizes = { 0x28000, 0x3C000, 0x1000, 0x28000, 0x3C000, 0x1000, 8, 8 };
        private static readonly Lazy<Bangcle> Embedded = new(LoadEmbedded);

        private readonly byte[] _invRound;
        private readonly byte[] _invXor;
        private readonly byte[] _invFirst;
        private readonly byte[] _round;
        private readonly byte[] _xor;
        private readonly byte[] _final;
        private readonly byte[] _permDecrypt;
        private readonly byte[] _permEncrypt;

        private Bangcle(byte[][] tables)
        {
            _invRound = tables[0];
            _invXor = tables[1];
            _invFirst = tables[2];
            _round = tables[3];
            _xor = tables[4];
            _final = tables[5];
            _permDecrypt = tables[6];
            _permEncrypt = tables[7];
        }

        /// <summary>
        /// The tables shipped with the driver (bangcle_tables.bin from pyBYD)
        /// </summary>
        public static Bangcle Default => Embedded.Value;

        private static Bangcle LoadEmbedded()
        {
            using var stream = typeof(Bangcle).Assembly.GetManifestResourceStream("P3.Driver.Byd.Driver.bangcle_tables.bin")
                               ?? throw new InvalidOperationException("bangcle_tables.bin is not embedded");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Load(ms.ToArray());
        }

        /// <summary>
        /// "BGTB", version 1 (u16), count 8 (u16), 8 × (offset u32, length u32), data
        /// </summary>
        internal static Bangcle Load(byte[] data)
        {
            if (data.Length < HeaderSize + TableSizes.Length * 8 || Encoding.ASCII.GetString(data, 0, 4) != "BGTB")
            {
                throw new InvalidDataException("Not a Bangcle table file");
            }
            if (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(4)) != 1 || BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6)) != TableSizes.Length)
            {
                throw new InvalidDataException("Unsupported Bangcle table file version");
            }
            var tables = new byte[TableSizes.Length][];
            for (var i = 0; i < TableSizes.Length; i++)
            {
                var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(HeaderSize + i * 8));
                var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(HeaderSize + i * 8 + 4));
                if (length != TableSizes[i] || offset < 0 || offset + length > data.Length)
                {
                    throw new InvalidDataException($"Bangcle table {i} is broken");
                }
                tables[i] = data.AsSpan(offset, length).ToArray();
            }
            return new Bangcle(tables);
        }

        #region envelope

        public string Encode(string plaintext)
        {
            var plain = Encoding.UTF8.GetBytes(plaintext);
            var padLength = 16 - plain.Length % 16;
            var padded = new byte[plain.Length + padLength];
            plain.CopyTo(padded, 0);
            padded.AsSpan(plain.Length).Fill((byte)padLength);
            return "F" + Convert.ToBase64String(EncryptCbc(padded));
        }

        public string Decode(string envelope)
        {
            var cleaned = (envelope ?? "").Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Trim()
                .Replace('-', '+').Replace('_', '/');
            if (!cleaned.StartsWith('F'))
            {
                throw new InvalidDataException("A Bangcle envelope starts with F");
            }
            cleaned = cleaned[1..];
            cleaned = cleaned.PadRight(cleaned.Length + (4 - cleaned.Length % 4) % 4, '=');
            var cipher = Convert.FromBase64String(cleaned);
            if (cipher.Length == 0 || cipher.Length % 16 != 0)
            {
                throw new InvalidDataException("Bangcle ciphertext length is not a multiple of 16");
            }
            var plain = DecryptCbc(cipher);
            return Encoding.UTF8.GetString(plain, 0, plain.Length - PadLength(plain));
        }

        /// <summary>
        /// PKCS#7, the data is kept as is when the padding is invalid (like pyBYD)
        /// </summary>
        private static int PadLength(byte[] data)
        {
            var pad = data[^1];
            if (pad == 0 || pad > 16 || data.Length < pad)
            {
                return 0;
            }
            for (var i = data.Length - pad; i < data.Length; i++)
            {
                if (data[i] != pad)
                {
                    return 0;
                }
            }
            return pad;
        }

        internal byte[] EncryptCbc(byte[] data)
        {
            var result = new byte[data.Length];
            Span<byte> prev = stackalloc byte[16];
            Span<byte> block = stackalloc byte[16];
            for (var offset = 0; offset < data.Length; offset += 16)
            {
                for (var i = 0; i < 16; i++)
                {
                    block[i] = (byte)(data[offset + i] ^ prev[i]);
                }
                EncryptBlock(block, result.AsSpan(offset, 16));
                result.AsSpan(offset, 16).CopyTo(prev);
            }
            return result;
        }

        internal byte[] DecryptCbc(byte[] data)
        {
            var result = new byte[data.Length];
            Span<byte> prev = stackalloc byte[16];
            for (var offset = 0; offset < data.Length; offset += 16)
            {
                var output = result.AsSpan(offset, 16);
                DecryptBlock(data.AsSpan(offset, 16), output);
                for (var i = 0; i < 16; i++)
                {
                    output[i] ^= prev[i];
                }
                data.AsSpan(offset, 16).CopyTo(prev);
            }
            return result;
        }

        #endregion

        #region block

        private static void ToState(ReadOnlySpan<byte> block, Span<byte> state)
        {
            for (var col = 0; col < 4; col++)
            {
                for (var row = 0; row < 4; row++)
                {
                    state[col * 8 + row] = block[col + row * 4];
                }
            }
        }

        private static void FromState(ReadOnlySpan<byte> state, Span<byte> output)
        {
            for (var col = 0; col < 4; col++)
            {
                for (var row = 0; row < 4; row++)
                {
                    output[col + row * 4] = state[col * 8 + row];
                }
            }
        }

        /// <summary>
        /// One table round: 4 × 4 32-bit lookups, then the nibble-wise xor network
        /// </summary>
        private static void Round(int rnd, Span<byte> state, Span<byte> temp64, byte[] roundTable, byte[] xorTable, byte[] perm)
        {
            var lVar21 = rnd * 4;
            var permPtr = 0;
            for (var i = 0; i < 4; i++)
            {
                var start = perm[permPtr];
                var lVar16 = i * 8;
                var baseOffset = i * 16;
                for (var j = 0; j < 4; j++)
                {
                    var u = (start + j) & 3;
                    var idx = state[lVar16 + u] + (i + (lVar21 + u) * 4) * 256;
                    var value = BinaryPrimitives.ReadUInt32LittleEndian(roundTable.AsSpan(idx * 4, 4));
                    BinaryPrimitives.WriteUInt32LittleEndian(temp64.Slice(baseOffset + j * 4, 4), value);
                }
                permPtr += 2;
            }

            var iVar15 = 1;
            for (var outer = 0; outer < 4; outer++)
            {
                var p = outer;
                for (var inner = 0; inner < 4; inner++)
                {
                    int local10 = temp64[p];
                    var low = local10 & 0xF;
                    var high = local10 & 0xF0;
                    var lVar2 = inner * 0x18 + rnd * 0x60;
                    var iVar25 = iVar15;
                    for (var k = 0; k < 3; k++)
                    {
                        int b = temp64[p + 0x10 * (k + 1)];
                        var uVar27 = low | ((b << 4) & 0xFF);
                        high = ((high >> 4) | ((b >> 4) << 4)) & 0xFF;
                        low = xorTable[(lVar2 + (iVar25 - 1)) * 0x100 + uVar27] & 0xF;
                        high = (xorTable[(lVar2 + iVar25) * 0x100 + high] & 0xF) << 4;
                        iVar25 += 2;
                    }
                    state[inner + outer * 8] = (byte)((high | low) & 0xFF);
                    p += 4;
                }
                iVar15 += 6;
            }
        }

        internal void EncryptBlock(ReadOnlySpan<byte> block, Span<byte> output)
        {
            Span<byte> state = stackalloc byte[32];
            Span<byte> temp64 = stackalloc byte[64];
            Span<byte> tmp32 = stackalloc byte[32];
            ToState(block, state);

            for (var rnd = 0; rnd < 9; rnd++)
            {
                Round(rnd, state, temp64, _round, _xor, _permEncrypt);
            }

            state.CopyTo(tmp32);
            for (var row = 0; row < 4; row++)
            {
                var row0 = row & 3;
                state[row] = _final[tmp32[row0] + row0 * 0x400];
                var row1 = (1 + row) & 3;
                state[8 + row] = _final[tmp32[8 + row1] + row1 * 0x400 + 0x100];
                var row2 = (2 + row) & 3;
                state[0x10 + row] = _final[tmp32[0x10 + row2] + row2 * 0x400 + 0x200];
                var row3 = (3 + row) & 3;
                state[0x18 + row] = _final[tmp32[0x18 + row3] + row3 * 0x400 + 0x300];
            }
            FromState(state, output);
        }

        internal void DecryptBlock(ReadOnlySpan<byte> block, Span<byte> output)
        {
            Span<byte> state = stackalloc byte[32];
            Span<byte> temp64 = stackalloc byte[64];
            Span<byte> tmp32 = stackalloc byte[32];
            ToState(block, state);

            for (var rnd = 9; rnd > 0; rnd--)
            {
                Round(rnd, state, temp64, _invRound, _invXor, _permDecrypt);
            }

            state.CopyTo(tmp32);
            int uVar8 = 1, uVar10 = 3, uVar12 = 2;
            for (var row = 0; row < 4; row++)
            {
                state[row] = _invFirst[tmp32[row] + row * 0x400];
                var row1 = uVar10 & 3;
                state[8 + row] = _invFirst[tmp32[8 + row1] + row1 * 0x400 + 0x100];
                var row2 = uVar12 & 3;
                state[0x10 + row] = _invFirst[tmp32[0x10 + row2] + row2 * 0x400 + 0x200];
                var row3 = uVar8 & 3;
                state[0x18 + row] = _invFirst[tmp32[0x18 + row3] + row3 * 0x400 + 0x300];
                uVar8++;
                uVar10++;
                uVar12++;
            }
            FromState(state, output);
        }

        #endregion
    }
}
