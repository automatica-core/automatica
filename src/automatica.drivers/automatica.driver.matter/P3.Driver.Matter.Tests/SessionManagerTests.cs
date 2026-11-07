using System.Net;
using P3.Driver.Matter.Sessions;

namespace P3.Driver.Matter.Tests
{
    public class SessionManagerTests
    {
        private static readonly IPEndPoint PeerA = new(IPAddress.Loopback, 40001);
        private static readonly IPEndPoint PeerB = new(IPAddress.Loopback, 40002);

        [Fact]
        public void GetOrCreateUnsecured_SameEphemeralId_ReturnsSameSessionAndUpdatesPeer()
        {
            var sessions = new SessionManager();

            var first = sessions.GetOrCreateUnsecured(PeerA, 7);
            var second = sessions.GetOrCreateUnsecured(PeerB, 7);

            Assert.Same(first, second);
            Assert.Equal(PeerB, second.Peer);
            Assert.Equal(7ul, second.EphemeralNodeId);
            Assert.False(second.IsSecure);
            Assert.Equal(1, sessions.UnsecuredSessionCount);
        }

        [Fact]
        public void GetOrCreateUnsecured_UpToTheCap_KeepsEverySession()
        {
            var sessions = new SessionManager();
            var created = new List<UnsecuredSession>();
            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions; id++)
            {
                created.Add(sessions.GetOrCreateUnsecured(PeerA, id));
            }

            Assert.Equal(SessionManager.MaxUnsecuredSessions, sessions.UnsecuredSessionCount);
            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions; id++)
            {
                Assert.Same(created[(int)id - 1], sessions.GetOrCreateUnsecured(PeerA, id));
            }
        }

        [Fact]
        public void GetOrCreateUnsecured_FloodOfFreshIds_NeverExceedsTheCap()
        {
            var sessions = new SessionManager();
            for (ulong id = 1; id <= 5000; id++)
            {
                sessions.GetOrCreateUnsecured(PeerA, id);
                Assert.True(sessions.UnsecuredSessionCount <= SessionManager.MaxUnsecuredSessions);
            }

            Assert.Equal(SessionManager.MaxUnsecuredSessions, sessions.UnsecuredSessionCount);
        }

        [Fact]
        public void GetOrCreateUnsecured_OverTheCap_EvictsTheOldestNotTheRecentlyActive()
        {
            var sessions = new SessionManager();
            var baseTime = DateTime.UtcNow.AddSeconds(-60);
            var all = new List<UnsecuredSession>();
            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions; id++)
            {
                var s = sessions.GetOrCreateUnsecured(PeerA, id);
                // deterministic ordering: id 1 is the oldest, all are younger than the 2 minute idle timeout
                s.LastActivity = baseTime.AddMilliseconds(id);
                all.Add(s);
            }

            // the commissioner keeps talking on id 1, so it is now the most recent one
            all[0].LastActivity = DateTime.UtcNow;

            var newcomer = sessions.GetOrCreateUnsecured(PeerA, 10_000);

            Assert.Equal(SessionManager.MaxUnsecuredSessions, sessions.UnsecuredSessionCount);
            Assert.Same(newcomer, sessions.GetOrCreateUnsecured(PeerA, 10_000));
            Assert.Same(all[0], sessions.GetOrCreateUnsecured(PeerA, 1));

            // id 2 was the oldest one and must be gone: asking for it again creates a fresh session
            Assert.NotSame(all[1], sessions.GetOrCreateUnsecured(PeerA, 2));
        }

        [Fact]
        public void GetOrCreateUnsecured_OverTheCap_DropsAllIdleSessionsFirst()
        {
            var sessions = new SessionManager();
            var idle = new List<UnsecuredSession>();
            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions; id++)
            {
                var s = sessions.GetOrCreateUnsecured(PeerA, id);
                if (id <= 10)
                {
                    s.LastActivity = DateTime.UtcNow.AddMinutes(-10);
                    idle.Add(s);
                }
            }

            sessions.GetOrCreateUnsecured(PeerA, 9999);

            // all 10 idle sessions go in one sweep, not just the single one the cap needs
            Assert.Equal(SessionManager.MaxUnsecuredSessions + 1 - 10, sessions.UnsecuredSessionCount);
            Assert.NotSame(idle[0], sessions.GetOrCreateUnsecured(PeerA, 1));
        }

        [Fact]
        public void GetOrCreateUnsecured_NewSessionIsNeverItsOwnEvictionVictim()
        {
            var sessions = new SessionManager();
            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions; id++)
            {
                // all existing sessions look newer than the one about to be created
                sessions.GetOrCreateUnsecured(PeerA, id).LastActivity = DateTime.UtcNow.AddMinutes(1);
            }

            var newcomer = sessions.GetOrCreateUnsecured(PeerA, 4242);

            Assert.Same(newcomer, sessions.GetOrCreateUnsecured(PeerA, 4242));
            Assert.True(sessions.UnsecuredSessionCount <= SessionManager.MaxUnsecuredSessions);
        }

        [Fact]
        public void RemoveUnsecured_RemovesOnlyThatSession()
        {
            var sessions = new SessionManager();
            var a = sessions.GetOrCreateUnsecured(PeerA, 1);
            var b = sessions.GetOrCreateUnsecured(PeerA, 2);

            sessions.RemoveUnsecured(a);

            Assert.Equal(1, sessions.UnsecuredSessionCount);
            Assert.Same(b, sessions.GetOrCreateUnsecured(PeerA, 2));
            Assert.NotSame(a, sessions.GetOrCreateUnsecured(PeerA, 1));
        }

        [Fact]
        public void GetOrCreateUnsecured_ConcurrentFlood_StaysBounded()
        {
            var sessions = new SessionManager();
            Parallel.For(0, 4000, i => sessions.GetOrCreateUnsecured(PeerA, (ulong)i + 1));

            // racing inserts may overshoot by a few, but the table must not grow with the flood
            Assert.True(sessions.UnsecuredSessionCount <= SessionManager.MaxUnsecuredSessions + Environment.ProcessorCount + 1,
                $"count {sessions.UnsecuredSessionCount}");
        }

        private static SecureSession Secure(ushort id, bool pase = false, DateTime? lastActivity = null)
        {
            var s = new SecureSession(PeerA) { LocalSessionId = id, PeerSessionId = (ushort)(id + 1000), IsPase = pase };
            if (lastActivity.HasValue)
            {
                s.LastActivity = lastActivity.Value;
            }

            return s;
        }

        [Fact]
        public void Add_SecureSession_IsFoundById()
        {
            var sessions = new SessionManager();
            var s = Secure(10);

            sessions.Add(s);

            Assert.Same(s, sessions.Get(10));
            Assert.Null(sessions.Get(11));
            Assert.True(s.IsSecure);
        }

        [Fact]
        public void Add_OverTheSecureCap_EvictsTheOldestAndRaisesSessionRemoved()
        {
            var sessions = new SessionManager();
            var removed = new List<SecureSession>();
            sessions.SessionRemoved += removed.Add;
            var baseTime = DateTime.UtcNow.AddMinutes(-5);
            for (ushort id = 1; id <= SessionManager.MaxSecureSessions; id++)
            {
                sessions.Add(Secure(id, lastActivity: baseTime.AddSeconds(id)));
            }

            // older than everything else, but the session just added is never evicted
            var newest = Secure(500, lastActivity: baseTime);
            sessions.Add(newest);

            Assert.Equal(SessionManager.MaxSecureSessions, sessions.SecureSessions.Count());
            Assert.Same(newest, sessions.Get(500));
            var evicted = Assert.Single(removed);
            Assert.Equal((ushort)1, evicted.LocalSessionId);
            Assert.True(evicted.IsClosed);
            Assert.Null(sessions.Get(1));
        }

        [Fact]
        public void Remove_Twice_RaisesSessionRemovedOnce()
        {
            var sessions = new SessionManager();
            var count = 0;
            sessions.SessionRemoved += _ => count++;
            var s = Secure(3);
            sessions.Add(s);

            sessions.Remove(s);
            sessions.Remove(s);

            Assert.Equal(1, count);
            Assert.True(s.IsClosed);
            Assert.Null(sessions.Get(3));
        }

        [Fact]
        public void RemovePaseSessions_KeepsCaseSessions()
        {
            var sessions = new SessionManager();
            sessions.Add(Secure(1, pase: true));
            sessions.Add(Secure(2, pase: false));
            sessions.Add(Secure(3, pase: true));

            sessions.RemovePaseSessions();

            Assert.Equal(new ushort[] { 2 }, sessions.SecureSessions.Select(s => s.LocalSessionId).ToArray());
        }

        [Fact]
        public void RemoveForFabric_SessionsWithoutFabric_OnlyMatchFabricIndexZero()
        {
            var sessions = new SessionManager();
            sessions.Add(Secure(1));
            sessions.Add(Secure(2));

            sessions.RemoveForFabric(1);
            Assert.Equal(2, sessions.SecureSessions.Count());

            sessions.RemoveForFabric(0);
            Assert.Empty(sessions.SecureSessions);
        }

        [Fact]
        public void AllocateSessionId_NeverReturnsZeroOrAnIdInUse()
        {
            var sessions = new SessionManager();
            var used = new HashSet<ushort>();
            while (sessions.SecureSessions.Count() < SessionManager.MaxSecureSessions)
            {
                var id = sessions.AllocateSessionId();
                Assert.NotEqual((ushort)0, id);
                Assert.True(used.Add(id), $"id {id} handed out twice");
                sessions.Add(Secure(id));
            }
        }

        [Fact]
        public void NextUnsecuredCounter_IsMonotonic()
        {
            var sessions = new SessionManager();
            var a = sessions.NextUnsecuredCounter();
            Assert.Equal(a + 1, sessions.NextUnsecuredCounter());
        }

        [Fact]
        public void SecureSession_NextSendCounter_Increments()
        {
            var s = Secure(1);
            var a = s.NextSendCounter();
            Assert.Equal(a + 1, s.NextSendCounter());
        }
    }
}
