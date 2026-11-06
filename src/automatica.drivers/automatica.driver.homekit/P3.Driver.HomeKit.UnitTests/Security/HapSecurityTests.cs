using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Security
{
    public class HapSecurityTests
    {
        private const string PairCode = "031-45-154";
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private static readonly string ControllerLtpk = new string('a', 64);

        private readonly TlvParser _tlvParser = new TlvParser(NullLogger.Instance);

        public HapSecurityTests()
        {
            HapControllerServer.HapControllerId = "FC:22:3D:E3:CE:F2";
        }

        private static HapMiddleware CreateMiddleware(HapPairingStore store)
        {
            return new HapMiddleware(NullLogger.Instance, PairCode, null, store);
        }

        private static HapPairingStore PairedStore(bool admin = true)
        {
            var store = new HapPairingStore(Ltsk, null);
            Assert.True(store.CompletePairSetup(Ltsk, "controller-1", ControllerLtpk));
            if (!admin)
            {
                store.AddPairing("controller-1", ControllerLtpk, false);
            }
            return store;
        }

        private Tlv ParseTlv(HapResponse response)
        {
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/pairing+tlv8", response.ContentType);
            return _tlvParser.Parse(response.Body);
        }

        private static byte[] M1()
        {
            return Automatica.Core.Driver.Utility.Utils.StringToByteArray("000100060101");
        }

        private byte[] M3WithWrongProof()
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 3);
            var publicKey = new byte[384];
            RandomNumberGenerator.Fill(publicKey);
            publicKey[0] = 0x01;
            var proof = new byte[64];
            RandomNumberGenerator.Fill(proof);
            tlv.AddType(Constants.PublicKey, publicKey);
            tlv.AddType(Constants.Proof, proof);
            return _tlvParser.Serialize(tlv);
        }

        private static byte[] PairingsRequest(int method, string identifier = null, string ltpk = null, byte? permissions = null)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.Method, method);
            if (identifier != null)
            {
                tlv.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(identifier));
            }
            if (ltpk != null)
            {
                tlv.AddType(Constants.PublicKey, Automatica.Core.Driver.Utility.Utils.StringToByteArray(ltpk));
            }
            if (permissions.HasValue)
            {
                tlv.AddType(Constants.Permissions, new[] { permissions.Value });
            }
            return new TlvParser(NullLogger.Instance).Serialize(tlv);
        }

        private static HapSession Verify(HapMiddleware middleware, string connectionId, string controllerId, bool admin)
        {
            var session = middleware.GetSession(connectionId);
            session.IsVerified = true;
            session.ControllerId = controllerId;
            session.IsAdmin = admin;
            return session;
        }

        [Theory]
        [InlineData("GET", "accessories", "")]
        [InlineData("GET", "characteristics?id=1.2", "")]
        [InlineData("PUT", "characteristics", "{\"characteristics\":[{\"aid\":2,\"iid\":10,\"value\":true}]}")]
        [InlineData("POST", "pairings", "0001050601")]
        [InlineData("PUT", "prepare", "{}")]
        public void UnverifiedRequestsAreRejectedWith470(string method, string url, string body)
        {
            var middleware = CreateMiddleware(PairedStore());
            var data = method == "POST" ? Automatica.Core.Driver.Utility.Utils.StringToByteArray(body) : Encoding.UTF8.GetBytes(body);

            var response = middleware.Invoke("con", url, method, data, new ConnectionSession());

            Assert.Equal(470, response.StatusCode);
            Assert.Contains("-70401", Encoding.UTF8.GetString(response.Body));
        }

        [Fact]
        public void HttpResponseCarriesTheStatusCode()
        {
            var response = HapMiddleware.ConnectionAuthorizationRequired();
            var raw = Encoding.UTF8.GetString(HttpServerConnection.GetHttpResponse("HTTP/1.1", response.ContentType, response.Body, DateTime.Now, response.StatusCode, response.Reason));

            Assert.StartsWith("HTTP/1.1 470 Connection Authorization Required\r\n", raw);
        }

        [Fact]
        public void PairVerifyIsRejectedWhileUnpaired()
        {
            var middleware = CreateMiddleware(new HapPairingStore(null, null));
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.PublicKey, new byte[32]);

            var response = ParseTlv(middleware.Invoke("con", "pair-verify", "POST", _tlvParser.Serialize(tlv), new ConnectionSession()));

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Authentication, response.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetupIsUnavailableWhenAlreadyPaired()
        {
            var middleware = CreateMiddleware(PairedStore());

            var response = ParseTlv(middleware.Invoke("con", "pair-setup", "POST", M1(), new ConnectionSession()));

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)ErrorCodes.Unavailable, response.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void PairSetupStartsWhenUnpaired()
        {
            var middleware = CreateMiddleware(new HapPairingStore(null, null));

            var response = ParseTlv(middleware.Invoke("con", "pair-setup", "POST", M1(), new ConnectionSession()));

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.False(response.HasType(Constants.Error));
            Assert.Equal(16, response.GetType(Constants.Salt).Length);
        }

        [Fact]
        public void PairSetupSaltIsRandom()
        {
            var a = ParseTlv(CreateMiddleware(new HapPairingStore(null, null)).Invoke("a", "pair-setup", "POST", M1(), new ConnectionSession()));
            var b = ParseTlv(CreateMiddleware(new HapPairingStore(null, null)).Invoke("b", "pair-setup", "POST", M1(), new ConnectionSession()));

            Assert.NotEqual(a.GetType(Constants.Salt), b.GetType(Constants.Salt));
        }

        [Fact]
        public void WrongSetupCodeIsCountedAndLocksOutAfter100Attempts()
        {
            var store = new HapPairingStore(null, "{\"pairings\":[],\"failedSetupAttempts\":98}");
            var middleware = CreateMiddleware(store);

            for (var i = 0; i < 2; i++)
            {
                var connection = new ConnectionSession();
                ParseTlv(middleware.Invoke("con", "pair-setup", "POST", M1(), connection));
                var m4 = ParseTlv(middleware.Invoke("con", "pair-setup", "POST", M3WithWrongProof(), connection));

                Assert.Equal(4, m4.GetTypeAsInt(Constants.State));
                Assert.Equal((int)ErrorCodes.Authentication, m4.GetTypeAsInt(Constants.Error));
            }

            Assert.Equal(100, store.FailedSetupAttempts);
            Assert.True(store.IsLockedOut);

            var locked = ParseTlv(middleware.Invoke("con2", "pair-setup", "POST", M1(), new ConnectionSession()));
            Assert.Equal((int)ErrorCodes.MaxTries, locked.GetTypeAsInt(Constants.Error));
        }

        [Fact]
        public void FailedAttemptsArePersisted()
        {
            var store = new HapPairingStore(null, null);
            string persisted = null;
            store.Changed += (s, e) => persisted = e.Ltpk;

            for (var i = 0; i < HapPairingStore.MaxFailedSetupAttempts; i++)
            {
                store.RegisterFailedSetupAttempt();
            }

            Assert.True(new HapPairingStore(null, persisted).IsLockedOut);

            store.Clear();
            Assert.False(new HapPairingStore(null, persisted).IsLockedOut);
        }

        [Fact]
        public void SecondControllerIsBusyUntilTheSetupTimesOut()
        {
            var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
            var middleware = CreateMiddleware(new HapPairingStore(null, null));
            middleware.UtcNow = () => now;

            ParseTlv(middleware.Invoke("a", "pair-setup", "POST", M1(), new ConnectionSession()));

            var busy = ParseTlv(middleware.Invoke("b", "pair-setup", "POST", M1(), new ConnectionSession()));
            Assert.Equal((int)ErrorCodes.Busy, busy.GetTypeAsInt(Constants.Error));

            // b may not continue a setup started by a
            var hijack = ParseTlv(middleware.Invoke("b", "pair-setup", "POST", M3WithWrongProof(), new ConnectionSession()));
            Assert.Equal((int)ErrorCodes.Unknown, hijack.GetTypeAsInt(Constants.Error));

            now = now.Add(HapMiddleware.PairSetupTimeout).AddSeconds(1);

            var ok = ParseTlv(middleware.Invoke("b", "pair-setup", "POST", M1(), new ConnectionSession()));
            Assert.False(ok.HasType(Constants.Error));
        }

        [Fact]
        public void ClosingTheConnectionReleasesThePendingSetup()
        {
            var middleware = CreateMiddleware(new HapPairingStore(null, null));

            ParseTlv(middleware.Invoke("a", "pair-setup", "POST", M1(), new ConnectionSession()));
            middleware.TerminateSession("a");

            var ok = ParseTlv(middleware.Invoke("b", "pair-setup", "POST", M1(), new ConnectionSession()));
            Assert.False(ok.HasType(Constants.Error));
        }

        [Fact]
        public void AdminCanAddListAndRemovePairings()
        {
            var store = PairedStore();
            var middleware = CreateMiddleware(store);
            Verify(middleware, "con", "controller-1", true);
            var secondLtpk = new string('b', 64);

            var add = ParseTlv(middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodAddPairing, "controller-2", secondLtpk, 0), new ConnectionSession()));
            Assert.False(add.HasType(Constants.Error));
            Assert.Equal(2, store.Pairings.Count);

            var listRaw = middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodListPairings), new ConnectionSession());
            Assert.Equal(1, listRaw.Body.Count(a => a == (byte)Constants.Separator));
            Assert.Contains("controller-2", Encoding.UTF8.GetString(listRaw.Body));

            // a different key for a known identifier is refused
            var conflict = ParseTlv(middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodAddPairing, "controller-2", new string('c', 64), 1), new ConnectionSession()));
            Assert.Equal((int)ErrorCodes.Unknown, conflict.GetTypeAsInt(Constants.Error));

            var remove = ParseTlv(middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodRemovePairing, "controller-2"), new ConnectionSession()));
            Assert.False(remove.HasType(Constants.Error));
            Assert.Single(store.Pairings);
            Assert.Null(store.Find("controller-2"));
        }

        [Fact]
        public void RemovingTheLastAdminUnpairsTheAccessory()
        {
            var store = PairedStore();
            store.AddPairing("controller-2", new string('b', 64), false);
            var middleware = CreateMiddleware(store);
            var session = Verify(middleware, "con", "controller-1", true);

            ParseTlv(middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodRemovePairing, "controller-1"), new ConnectionSession()));

            Assert.False(store.IsPaired);
            Assert.Empty(store.Pairings);
            Assert.True(session.CloseAfterResponse);
            Assert.Null(store.Find("controller-1"));
        }

        [Fact]
        public void NonAdminCannotManagePairings()
        {
            var store = PairedStore();
            var middleware = CreateMiddleware(store);
            Verify(middleware, "con", "controller-1", false);

            var response = ParseTlv(middleware.Invoke("con", "pairings", "POST", PairingsRequest(PairingController.MethodAddPairing, "evil", new string('d', 64), 1), new ConnectionSession()));

            Assert.Equal((int)ErrorCodes.Authentication, response.GetTypeAsInt(Constants.Error));
            Assert.Single(store.Pairings);
        }

        [Fact]
        public void PairingsAreCapped()
        {
            var store = PairedStore();
            for (var i = 1; i < HapPairingStore.MaxPairings; i++)
            {
                Assert.Null(store.AddPairing($"c{i}", i.ToString("x64"), false));
            }

            Assert.Equal(ErrorCodes.MaxPeers, store.AddPairing("one-too-many", new string('e', 64), false));
        }

        [Fact]
        public void LegacyLtpkIsLoadedAndBoundOnFirstVerify()
        {
            var store = new HapPairingStore(Ltsk, ControllerLtpk);

            Assert.True(store.IsPaired);
            var candidate = store.Find("any-controller");
            Assert.NotNull(candidate);
            Assert.Null(candidate.Identifier);

            store.BindIdentifier(ControllerLtpk, "controller-1");

            Assert.Equal("controller-1", store.Find("controller-1").Identifier);
            Assert.Null(store.Find("another-controller"));

            var reloaded = new HapPairingStore(Ltsk, store.Serialize());
            Assert.Equal("controller-1", reloaded.Pairings.Single().Identifier);
        }

        [Fact]
        public void SetupCodeIsWellFormedAndValid()
        {
            for (var i = 0; i < 200; i++)
            {
                var code = HomeKitSetup.GenerateSetupCode();
                Assert.Matches("^[0-9]{3}-[0-9]{2}-[0-9]{3}$", code);
                Assert.True(HomeKitSetup.IsSetupCodeValid(code));
            }
        }
    }
}
