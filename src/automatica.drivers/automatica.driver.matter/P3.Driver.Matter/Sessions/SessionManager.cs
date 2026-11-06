using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;

namespace P3.Driver.Matter.Sessions
{
    public sealed class SessionManager
    {
        public const int MaxSecureSessions = 32;

        private readonly ConcurrentDictionary<ushort, SecureSession> _secure = new();
        private readonly ConcurrentDictionary<ulong, UnsecuredSession> _unsecured = new();
        private uint _unsecuredCounter = (uint)RandomNumberGenerator.GetInt32(1, 1 << 28);

        public event Action<SecureSession> SessionRemoved;

        public IEnumerable<SecureSession> SecureSessions => _secure.Values;

        public SecureSession Get(ushort localSessionId) => _secure.TryGetValue(localSessionId, out var s) ? s : null;

        public UnsecuredSession GetOrCreateUnsecured(IPEndPoint peer, ulong ephemeralNodeId)
        {
            var session = _unsecured.GetOrAdd(ephemeralNodeId, id => new UnsecuredSession(peer, id));
            session.Peer = peer;
            session.LastActivity = DateTime.UtcNow;

            if (_unsecured.Count > 64)
            {
                foreach (var stale in _unsecured.Values.Where(s => DateTime.UtcNow - s.LastActivity > TimeSpan.FromMinutes(2)).ToList())
                {
                    _unsecured.TryRemove(stale.EphemeralNodeId, out _);
                }
            }

            return session;
        }

        public void RemoveUnsecured(UnsecuredSession session) => _unsecured.TryRemove(session.EphemeralNodeId, out _);

        public uint NextUnsecuredCounter() => Interlocked.Increment(ref _unsecuredCounter);

        public ushort AllocateSessionId()
        {
            while (true)
            {
                var id = (ushort)RandomNumberGenerator.GetInt32(1, 0x10000);
                if (!_secure.ContainsKey(id))
                {
                    return id;
                }
            }
        }

        public void Add(SecureSession session)
        {
            _secure[session.LocalSessionId] = session;
            while (_secure.Count > MaxSecureSessions)
            {
                var oldest = _secure.Values.Where(s => s != session).OrderBy(s => s.LastActivity).FirstOrDefault();
                if (oldest == null)
                {
                    break;
                }

                Remove(oldest);
            }
        }

        public void Remove(SecureSession session)
        {
            if (_secure.TryRemove(session.LocalSessionId, out _))
            {
                session.IsClosed = true;
                SessionRemoved?.Invoke(session);
            }
        }

        public void RemoveForFabric(byte fabricIndex)
        {
            foreach (var s in _secure.Values.Where(s => s.FabricIndex == fabricIndex).ToList())
            {
                Remove(s);
            }
        }

        public void RemovePaseSessions()
        {
            foreach (var s in _secure.Values.Where(s => s.IsPase).ToList())
            {
                Remove(s);
            }
        }
    }
}
