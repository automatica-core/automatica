using System.Buffers.Binary;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace P3.Driver.Matter.Crypto
{
    /// <summary>
    /// SPAKE2+ over P-256 as used by PASE (spec 3.10): w0/w1 from PBKDF2 of the passcode, Ka||Ke = Hash(TT),
    /// KcA||KcB = KDF(Ka, "ConfirmationKeys"), cA = HMAC(KcA, Y), cB = HMAC(KcB, X).
    /// </summary>
    public static class Spake2Plus
    {
        public const int WsLength = 40;

        internal static readonly X9ECParameters Curve = ECNamedCurveTable.GetByName("P-256");
        private static readonly SecureRandom Random = new();

        // M and N from RFC 9383 / the Matter spec (compressed SEC1 encodings).
        internal static readonly ECPoint M = Curve.Curve.DecodePoint(Convert.FromHexString("02886e2f97ace46e55ba9dd7242579f2993b64e16ef3dcab95afd497333d8fa12f"));
        internal static readonly ECPoint N = Curve.Curve.DecodePoint(Convert.FromHexString("03d8bbd6c639c62937b04d997f38c3770719c629d7014d49a24b4f98baa1292b49"));

        private static readonly byte[] ConfirmationKeysInfo = "ConfirmationKeys"u8.ToArray();

        /// <summary>Computes w0s/w1s with PBKDF2 and reduces them modulo the group order.</summary>
        public static (byte[] W0, byte[] W1) ComputeW0W1(uint passcode, byte[] salt, int iterations)
        {
            var pw = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(pw, passcode);
            var ws = MatterCrypto.Pbkdf2(pw, salt, iterations, 2 * WsLength);
            var w0 = new BigInteger(1, ws, 0, WsLength).Mod(Curve.N);
            var w1 = new BigInteger(1, ws, WsLength, WsLength).Mod(Curve.N);
            return (ToFixed(w0), ToFixed(w1));
        }

        /// <summary>The verifier a device stores: w0 and L = w1·G (uncompressed).</summary>
        public static (byte[] W0, byte[] L) ComputeVerifier(uint passcode, byte[] salt, int iterations)
        {
            var (w0, w1) = ComputeW0W1(passcode, salt, iterations);
            var l = Curve.G.Multiply(new BigInteger(1, w1)).Normalize().GetEncoded(false);
            return (w0, l);
        }

        internal static byte[] ToFixed(BigInteger v)
        {
            var bytes = v.ToByteArrayUnsigned();
            if (bytes.Length == 32)
            {
                return bytes;
            }

            var r = new byte[32];
            bytes.CopyTo(r, 32 - bytes.Length);
            return r;
        }

        internal static BigInteger RandomScalar()
        {
            BigInteger k;
            do
            {
                k = new BigInteger(256, Random);
            } while (k.SignValue == 0 || k.CompareTo(Curve.N) >= 0);

            return k;
        }

        internal static ECPoint Decode(byte[] encoded)
        {
            ECPoint p;
            try
            {
                p = Curve.Curve.DecodePoint(encoded).Normalize();
            }
            catch (Exception e)
            {
                throw new MatterProtocolException("Invalid SPAKE2+ point", e);
            }

            if (p.IsInfinity || !p.IsValid())
            {
                throw new MatterProtocolException("Invalid SPAKE2+ point");
            }

            return p;
        }

        internal static (byte[] Ke, byte[] KcA, byte[] KcB) DeriveKeys(byte[] context, byte[] x, byte[] y, byte[] z, byte[] v, byte[] w0)
        {
            using var tt = new MemoryStream();
            void Add(byte[] data)
            {
                Span<byte> len = stackalloc byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(len, (ulong)data.Length);
                tt.Write(len);
                tt.Write(data);
            }

            Add(context);
            Add(Array.Empty<byte>()); // idProver
            Add(Array.Empty<byte>()); // idVerifier
            Add(M.GetEncoded(false));
            Add(N.GetEncoded(false));
            Add(x);
            Add(y);
            Add(z);
            Add(v);
            Add(w0);

            var hash = MatterCrypto.Sha256(tt.ToArray());
            var ka = hash[..16];
            var ke = hash[16..];
            var kc = MatterCrypto.Kdf(ka, null, ConfirmationKeysInfo, 32);
            return (ke, kc[..16], kc[16..]);
        }
    }

    /// <summary>The device (verifier) side of a PASE exchange.</summary>
    public sealed class Spake2PlusVerifier
    {
        private readonly byte[] _context;
        private readonly byte[] _w0;
        private readonly ECPoint _l;
        private readonly BigInteger _y;

        public byte[] Y { get; }
        public byte[] Ke { get; private set; }
        public byte[] CB { get; private set; }
        private byte[] _expectedCA;

        public Spake2PlusVerifier(byte[] context, byte[] w0, byte[] l)
        {
            _context = context;
            _w0 = w0;
            _l = Spake2Plus.Decode(l);
            _y = Spake2Plus.RandomScalar();
            var w0Int = new BigInteger(1, w0);
            Y = Spake2Plus.Curve.G.Multiply(_y).Add(Spake2Plus.N.Multiply(w0Int)).Normalize().GetEncoded(false);
        }

        /// <summary>Processes pA (X) from Pake1 and produces cB for Pake2.</summary>
        public void ProcessX(byte[] x)
        {
            var xPoint = Spake2Plus.Decode(x);
            var w0Int = new BigInteger(1, _w0);
            var zPoint = xPoint.Subtract(Spake2Plus.M.Multiply(w0Int)).Multiply(_y).Normalize();
            var vPoint = _l.Multiply(_y).Normalize();
            if (zPoint.IsInfinity || vPoint.IsInfinity)
            {
                throw new MatterProtocolException("SPAKE2+ produced the point at infinity");
            }

            var (ke, kcA, kcB) = Spake2Plus.DeriveKeys(_context, x, Y, zPoint.GetEncoded(false), vPoint.GetEncoded(false), _w0);
            Ke = ke;
            CB = MatterCrypto.HmacSha256(kcB, x);
            _expectedCA = MatterCrypto.HmacSha256(kcA, Y);
        }

        public bool VerifyCA(byte[] cA) => _expectedCA != null && MatterCrypto.FixedTimeEquals(cA, _expectedCA);
    }

    /// <summary>The commissioner (prover) side; used by the tests and the self-check tool.</summary>
    public sealed class Spake2PlusProver
    {
        private readonly byte[] _context;
        private readonly byte[] _w0;
        private readonly byte[] _w1;
        private readonly BigInteger _x;

        public byte[] X { get; }
        public byte[] Ke { get; private set; }
        public byte[] CA { get; private set; }
        private byte[] _expectedCB;

        public Spake2PlusProver(byte[] context, byte[] w0, byte[] w1)
        {
            _context = context;
            _w0 = w0;
            _w1 = w1;
            _x = Spake2Plus.RandomScalar();
            X = Spake2Plus.Curve.G.Multiply(_x).Add(Spake2Plus.M.Multiply(new BigInteger(1, w0))).Normalize().GetEncoded(false);
        }

        public void ProcessY(byte[] y)
        {
            var yPoint = Spake2Plus.Decode(y);
            var t = yPoint.Subtract(Spake2Plus.N.Multiply(new BigInteger(1, _w0)));
            var z = t.Multiply(_x).Normalize().GetEncoded(false);
            var v = t.Multiply(new BigInteger(1, _w1)).Normalize().GetEncoded(false);
            var (ke, kcA, kcB) = Spake2Plus.DeriveKeys(_context, X, y, z, v, _w0);
            Ke = ke;
            CA = MatterCrypto.HmacSha256(kcA, y);
            _expectedCB = MatterCrypto.HmacSha256(kcB, X);
        }

        public bool VerifyCB(byte[] cB) => _expectedCB != null && MatterCrypto.FixedTimeEquals(cB, _expectedCB);
    }
}
