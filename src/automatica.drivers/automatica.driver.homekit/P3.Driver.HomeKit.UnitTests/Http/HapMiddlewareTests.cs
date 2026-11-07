using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;
using P3.Driver.HomeKit.UnitTests.Integration;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Http
{
    public class HapMiddlewareTests
    {
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private static (HomeKitServer server, HapMiddleware middleware, Characteristic on) Create(bool paired = true)
        {
            var pairings = "";
            if (paired)
            {
                var store = new HapPairingStore(null, null);
                store.CompletePairSetup(Ltsk, "admin", new string('a', 64));
                pairings = store.Serialize();
            }

            var server = LoopbackHapHost.CreateServer(paired ? Ltsk : "", pairings);
            var lamp = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", true, 40);
            server.AddAccessory(lamp);
            var on = lamp.Services[1].Characteristics.First(a => a.Type == CharacteristicBase.OnType);
            return (server, new HapMiddleware(NullLogger.Instance, LoopbackHapHost.PairCode, server, server.PairingStore), on);
        }

        private static HapSession Verify(HapMiddleware middleware, string connectionId)
        {
            var session = middleware.GetSession(connectionId);
            session.IsVerified = true;
            session.IsAdmin = true;
            session.ControllerId = "admin";
            session.ClientUsername = "61646d696e";
            return session;
        }

        private byte[] Tlv(params (Constants type, int value)[] items)
        {
            var tlv = new Tlv();
            foreach (var (type, value) in items)
            {
                tlv.AddType(type, value);
            }
            return _parser.Serialize(tlv);
        }

        [Fact]
        public void GetSession_SameConnection_ReturnsTheSameSession_UntilTerminated()
        {
            var (_, middleware, _) = Create();

            var first = middleware.GetSession("a");
            Assert.Same(first, middleware.GetSession("a"));
            Assert.NotSame(first, middleware.GetSession("b"));

            middleware.TerminateSession("a");

            Assert.NotSame(first, middleware.GetSession("a"));
        }

        [Fact]
        public void TerminateSession_UnknownConnection_DoesNotThrow()
        {
            var (_, middleware, _) = Create();

            middleware.TerminateSession("never-seen");
        }

        [Fact]
        public void GetSessionByClientUsername_FindsTheVerifiedSession()
        {
            var (_, middleware, _) = Create();
            var session = Verify(middleware, "a");
            middleware.GetSession("b");

            Assert.Same(session, middleware.GetSessionByClientUsername("61646d696e"));
            Assert.Null(middleware.GetSessionByClientUsername("other"));
        }

        [Fact]
        public void CloseAllSessions_ClosesEveryClient()
        {
            var (_, middleware, _) = Create();
            var a = middleware.GetSession("a");
            a.Client = new TcpClient();
            var b = Verify(middleware, "b");
            b.Client = new TcpClient();
            middleware.GetSession("c"); // without client

            middleware.CloseAllSessions();

            Assert.Throws<ObjectDisposedException>(() => a.Client.GetStream());
            Assert.Throws<ObjectDisposedException>(() => b.Client.GetStream());
        }

        [Fact]
        public void GetAccessories_Verified_ReturnsTheBridgeAndTheAccessories()
        {
            var (_, middleware, _) = Create();
            Verify(middleware, "a");

            var response = middleware.Invoke("a", "/accessories/", "GET", Array.Empty<byte>(), new ConnectionSession());

            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/hap+json", response.ContentType);
            var json = JObject.Parse(Encoding.UTF8.GetString(response.Body));
            Assert.Equal(new ulong[] { 1, 2 }, json["accessories"].Select(a => (ulong)a["aid"]).OrderBy(a => a));
        }

        [Fact]
        public void GetCharacteristics_SeveralIds_ReturnsEachKnownCharacteristic()
        {
            var (_, middleware, on) = Create();
            Verify(middleware, "a");

            var response = middleware.Invoke("a", $"characteristics?id=2.{on.Id},2.9,99.1,2.999", "GET", Array.Empty<byte>(), new ConnectionSession());

            Assert.Equal(200, response.StatusCode);
            var list = JObject.Parse(Encoding.UTF8.GetString(response.Body))["characteristics"].ToList();
            Assert.Equal(new[] { on.Id, 9 }, list.Select(a => (int)a["iid"]));
            Assert.True((bool)list[0]["value"]);
            Assert.Equal(40, (int)list[1]["value"]);
        }

        [Fact]
        public void GetCharacteristics_EmptyId_Is400()
        {
            var (_, middleware, _) = Create();
            Verify(middleware, "a");

            var response = middleware.Invoke("a", "characteristics?id=", "GET", Array.Empty<byte>(), new ConnectionSession());

            Assert.Equal(400, response.StatusCode);
            Assert.Contains("-70410", Encoding.UTF8.GetString(response.Body));
        }

        [Fact]
        public void PutCharacteristics_UnknownAccessoryOrCharacteristic_IsIgnored()
        {
            var (server, middleware, on) = Create();
            Verify(middleware, "a");
            var changes = 0;
            server.ValueChanged += (s, e) => changes++;

            var body = Encoding.UTF8.GetBytes("{\"characteristics\":[{\"aid\":99,\"iid\":8,\"value\":false},{\"aid\":2,\"iid\":999,\"value\":false}]}");
            var response = middleware.Invoke("a", "characteristics", "PUT", body, new ConnectionSession());

            Assert.Equal(200, response.StatusCode);
            Assert.Empty(response.Body); // => 204
            Assert.Equal(true, on.Value);
            Assert.Equal(0, changes);
        }

        [Fact]
        public void UnknownEndpoint_OnAVerifiedSession_Throws()
        {
            var (_, middleware, _) = Create();
            Verify(middleware, "a");

            Assert.Throws<InvalidOperationException>(() => middleware.Invoke("a", "nope", "GET", Array.Empty<byte>(), new ConnectionSession()));
        }

        [Fact]
        public void GetPairSetup_IsNotTreatedAsPairSetup()
        {
            var (_, middleware, _) = Create(paired: false);

            var response = middleware.Invoke("a", "pair-setup", "GET", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession());

            Assert.Equal(470, response.StatusCode);
        }

        [Fact]
        public void Identify_WhileUnpaired_Succeeds_AndWhilePaired_Is400()
        {
            var (_, unpaired, _) = Create(paired: false);
            var ok = unpaired.Invoke("a", "identify", "POST", Array.Empty<byte>(), new ConnectionSession());
            Assert.Equal(200, ok.StatusCode);
            Assert.Empty(ok.Body);

            var (_, paired, _) = Create();
            var denied = paired.Invoke("a", "identify", "POST", Array.Empty<byte>(), new ConnectionSession());
            Assert.Equal(400, denied.StatusCode);
        }

        [Fact]
        public void PairSetup_M3WithoutM1_IsUnknownWithState4()
        {
            var (_, middleware, _) = Create(paired: false);

            var response = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.State, 3)), new ConnectionSession()).Body);

            Assert.Equal(4, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unknown, response.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetup_UnexpectedState_ReleasesThePendingSetup()
        {
            var (_, middleware, _) = Create(paired: false);
            var connection = new ConnectionSession();
            middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), connection);

            var odd = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.State, 7)), connection).Body);
            Assert.Equal(8, odd.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unknown, odd.GetTypeAsInt(Constants.Error));

            var other = _parser.Parse(middleware.Invoke("b", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession()).Body);
            Assert.False(other.HasType(Constants.Error));
        }

        [Fact]
        public void PairSetup_M5BeforeM3_IsUnknownWithState6_AndReleasesTheSetup()
        {
            var (_, middleware, _) = Create(paired: false);
            var connection = new ConnectionSession();
            middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), connection);

            var m6 = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.State, 5)), connection).Body);

            Assert.Equal(6, m6.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unknown, m6.GetTypeAsInt(Constants.Error));
            Assert.False(_parser.Parse(middleware.Invoke("b", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession()).Body).HasType(Constants.Error));
        }

        [Fact]
        public void PairSetup_M1Again_OnTheSameConnection_RestartsTheSetup()
        {
            var (_, middleware, _) = Create(paired: false);
            var connection = new ConnectionSession();

            var first = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), connection).Body);
            var second = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), connection).Body);

            Assert.False(second.HasType(Constants.Error));
            Assert.NotEqual(first.GetType(Constants.Salt), second.GetType(Constants.Salt));
        }

        [Fact]
        public void PairSetup_ExactlyAtTheTimeout_IsStillBusy()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var (_, middleware, _) = Create(paired: false);
            middleware.UtcNow = () => now;
            middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession());

            now = now.Add(HapMiddleware.PairSetupTimeout);

            var busy = _parser.Parse(middleware.Invoke("b", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession()).Body);
            Assert.Equal((int)ErrorCodes.Busy, busy.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetup_TimedOutSetup_CannotBeContinuedByItsConnection()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var (_, middleware, _) = Create(paired: false);
            middleware.UtcNow = () => now;
            var connection = new ConnectionSession();
            middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), connection);

            now = now.Add(HapMiddleware.PairSetupTimeout).AddSeconds(1);

            var m4 = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.State, 3)), connection).Body);
            Assert.Equal(4, m4.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unknown, m4.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetup_OnAVerifiedSession_IsUnavailable()
        {
            var (_, middleware, _) = Create(paired: false);
            Verify(middleware, "a");

            var response = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession()).Body);

            Assert.Equal((int)ErrorCodes.Unavailable, response.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetup_WhileLockedOut_IsMaxTries_AndNotCountedAgain()
        {
            var store = new HapPairingStore(null, $"{{\"failedSetupAttempts\":{HapPairingStore.MaxFailedSetupAttempts}}}");
            var middleware = new HapMiddleware(NullLogger.Instance, LoopbackHapHost.PairCode, null, store);

            var response = _parser.Parse(middleware.Invoke("a", "pair-setup", "POST", Tlv((Constants.Method, 0), (Constants.State, 1)), new ConnectionSession()).Body);

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.MaxTries, response.GetTypeAsInt(Constants.Error));
            Assert.Equal(HapPairingStore.MaxFailedSetupAttempts, store.FailedSetupAttempts);
        }

        [Theory]
        [InlineData(31)]
        [InlineData(33)]
        public void PairVerify_M1WithAWrongKeyLength_IsAuthentication(int length)
        {
            var (_, middleware, _) = Create();
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.PublicKey, new byte[length]);

            var response = _parser.Parse(middleware.Invoke("a", "pair-verify", "POST", _parser.Serialize(tlv), new ConnectionSession()).Body);

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Authentication, response.GetTypeAsInt(Constants.Error));
            Assert.False(middleware.GetSession("a").IsVerified);
        }

        [Fact]
        public void PairVerify_M3WithoutM1_IsAuthenticationWithState4()
        {
            var (_, middleware, _) = Create();
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 3);
            tlv.AddType(Constants.EncryptedData, new byte[48]);

            var response = _parser.Parse(middleware.Invoke("a", "pair-verify", "POST", _parser.Serialize(tlv), new ConnectionSession()).Body);

            Assert.Equal(4, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Authentication, response.GetTypeAsInt(Constants.Error));
            Assert.False(middleware.GetSession("a").IsVerified);
        }

        [Fact]
        public void PairVerify_UnknownState_IsUnknown()
        {
            var (_, middleware, _) = Create();

            var response = _parser.Parse(middleware.Invoke("a", "pair-verify", "POST", Tlv((Constants.State, 5)), new ConnectionSession()).Body);

            Assert.Equal(6, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unknown, response.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairVerify_M1_PreparesTheSessionKeys_ButDoesNotVerify()
        {
            var (_, middleware, _) = Create();
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            var clientKey = new byte[32];
            clientKey[0] = 9;
            tlv.AddType(Constants.PublicKey, clientKey);

            var response = _parser.Parse(middleware.Invoke("a", "pair-verify", "POST", _parser.Serialize(tlv), new ConnectionSession()).Body);

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal(32, response.GetType(Constants.PublicKey).Length);
            Assert.Equal(2 + 17 + 2 + 64 + 16, response.GetType(Constants.EncryptedData).Length); // TLV id "FC:22:3D:E3:CE:F2" + TLV signature + auth tag
            var session = middleware.GetSession("a");
            Assert.False(session.IsVerified);
            Assert.NotNull(session.ControllerToAccessoryKey);
            Assert.NotNull(session.AccessoryToControllerKey);
            Assert.NotEqual(session.ControllerToAccessoryKey, session.AccessoryToControllerKey);
        }

        [Fact]
        public void PairVerify_M1_UsesAFreshEphemeralKeyEachTime()
        {
            var (_, middleware, _) = Create();
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            var clientKey = new byte[32];
            clientKey[0] = 9;
            tlv.AddType(Constants.PublicKey, clientKey);
            var request = _parser.Serialize(tlv);

            var a = _parser.Parse(middleware.Invoke("a", "pair-verify", "POST", request, new ConnectionSession()).Body);
            var b = _parser.Parse(middleware.Invoke("b", "pair-verify", "POST", request, new ConnectionSession()).Body);

            Assert.NotEqual(a.GetType(Constants.PublicKey), b.GetType(Constants.PublicKey));
        }
    }
}
