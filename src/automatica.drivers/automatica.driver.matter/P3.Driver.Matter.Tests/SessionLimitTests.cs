using System.Net;
using P3.Driver.Matter.Sessions;
using Xunit;

namespace P3.Driver.Matter.Tests
{
    public class SessionLimitTests
    {
        [Fact]
        public void UnsecuredSessionTableIsCapped()
        {
            var sessions = new SessionManager();
            var peer = new IPEndPoint(IPAddress.Loopback, 5540);

            for (ulong id = 1; id <= 1000; id++)
            {
                sessions.GetOrCreateUnsecured(peer, id);
            }

            Assert.True(sessions.UnsecuredSessionCount <= SessionManager.MaxUnsecuredSessions);
        }

        [Fact]
        public void NewestUnsecuredSessionSurvivesTheCap()
        {
            var sessions = new SessionManager();
            var peer = new IPEndPoint(IPAddress.Loopback, 5540);

            for (ulong id = 1; id <= SessionManager.MaxUnsecuredSessions + 10; id++)
            {
                sessions.GetOrCreateUnsecured(peer, id);
            }

            var latest = sessions.GetOrCreateUnsecured(peer, 4242);
            Assert.Same(latest, sessions.GetOrCreateUnsecured(peer, 4242));
        }
    }
}
