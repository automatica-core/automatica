using System.Net;
using System.Security.Cryptography;
using P3.Driver.Matter.Fabrics;

namespace P3.Driver.Matter.Sessions
{
    /// <summary>MRP parameters a peer announced in its session parameters (spec 4.12.8).</summary>
    public sealed class SessionParameters
    {
        public uint IdleIntervalMs { get; set; } = 500;
        public uint ActiveIntervalMs { get; set; } = 300;
        public ushort ActiveThresholdMs { get; set; } = 4000;
    }

    /// <summary>Tracks received message counters for duplicate detection (spec 4.6.5, 32-entry window).</summary>
    public sealed class ReceptionState
    {
        private bool _initialized;
        private uint _max;
        private uint _bitmap;

        /// <summary>Returns false if the counter is a duplicate (or too old to tell).</summary>
        public bool Accept(uint counter)
        {
            if (!_initialized)
            {
                _initialized = true;
                _max = counter;
                _bitmap = 0;
                return true;
            }

            if (counter == _max)
            {
                return false;
            }

            var ahead = unchecked((int)(counter - _max));
            if (ahead > 0)
            {
                _bitmap = ahead >= 32 ? 0 : (_bitmap << ahead) | (1u << (ahead - 1));
                _max = counter;
                return true;
            }

            var behind = -ahead;
            if (behind > 32)
            {
                return false;
            }

            var bit = 1u << (behind - 1);
            if ((_bitmap & bit) != 0)
            {
                return false;
            }

            _bitmap |= bit;
            return true;
        }
    }

    public abstract class Session
    {
        private static int _nextCounterSeed = RandomNumberGenerator.GetInt32(1, 1 << 28);

        protected Session(IPEndPoint peer)
        {
            Peer = peer;
            LastActivity = DateTime.UtcNow;
        }

        public IPEndPoint Peer { get; set; }
        public DateTime LastActivity { get; set; }
        public SessionParameters PeerParameters { get; set; } = new();
        public ReceptionState Reception { get; } = new();

        public abstract bool IsSecure { get; }

        protected static uint InitialCounter() => (uint)Interlocked.Increment(ref _nextCounterSeed) & 0x0FFFFFFF;
    }

    /// <summary>The unsecured session a commissioner uses for PASE / CASE establishment.</summary>
    public sealed class UnsecuredSession : Session
    {
        public UnsecuredSession(IPEndPoint peer, ulong ephemeralNodeId) : base(peer)
        {
            EphemeralNodeId = ephemeralNodeId;
        }

        /// <summary>The random source node id the initiator used (we address replies to it).</summary>
        public ulong EphemeralNodeId { get; }

        public override bool IsSecure => false;
    }

    /// <summary>A PASE or CASE session; the device is always the responder.</summary>
    public sealed class SecureSession : Session
    {
        private uint _sendCounter = InitialCounter();

        public SecureSession(IPEndPoint peer) : base(peer)
        {
        }

        public override bool IsSecure => true;

        public bool IsPase { get; init; }
        public ushort LocalSessionId { get; init; }
        public ushort PeerSessionId { get; init; }

        /// <summary>R2I key: encrypts what we send.</summary>
        public byte[] EncryptKey { get; init; }

        /// <summary>I2R key: decrypts what we receive.</summary>
        public byte[] DecryptKey { get; init; }

        public byte[] AttestationChallenge { get; init; }

        /// <summary>Our node id in this session (0 for PASE).</summary>
        public ulong LocalNodeId { get; init; }

        /// <summary>The peer's node id (0 for PASE).</summary>
        public ulong PeerNodeId { get; init; }

        /// <summary>The fabric of a CASE session; for PASE the fabric added during commissioning (if any).</summary>
        public Fabric Fabric { get; set; }

        public uint[] CaseAuthenticatedTags { get; init; } = Array.Empty<uint>();

        public byte FabricIndex => Fabric?.FabricIndex ?? 0;

        public bool IsClosed { get; set; }

        public uint NextSendCounter() => Interlocked.Increment(ref _sendCounter);

        public override string ToString() =>
            $"{(IsPase ? "PASE" : "CASE")} session {LocalSessionId}->{PeerSessionId} peer {PeerNodeId:X16} fabric {FabricIndex} @ {Peer}";
    }
}
