using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKit.Hap.TlvData;
using P3.Driver.HomeKit.Http;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Http
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class HapStaticsCollection
    {
        // HapControllerServer keeps the pairing in static fields
        public const string Name = "HapStatics";
    }

    public class EncryptedFrameTests
    {
        private static HapSession CreateSession()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            // the same key in both directions so EncryptData produces frames DecryptData can read
            return new HapSession { AccessoryToControllerKey = key, ControllerToAccessoryKey = key };
        }

        private static byte[] Plain(int length)
        {
            return Enumerable.Range(0, length).Select(a => (byte)(a % 251)).ToArray();
        }

        [Fact]
        public void TwoFramesInOneBuffer_AreBothDecrypted()
        {
            var session = CreateSession();
            var plain = Plain(1500); // 1024 + 476 => two frames
            var encrypted = HttpServerConnection.EncryptData(plain, session);

            Assert.Equal(2 + 1024 + 16 + 2 + 476 + 16, encrypted.Length);

            var decrypted = HttpServerConnection.DecryptData(encrypted, session, out var remainder);

            Assert.Equal(plain, decrypted);
            Assert.Empty(remainder);
            Assert.Equal(2, session.InboundBinaryMessageCount);
        }

        [Fact]
        public void PartialFrame_IsKeptForTheNextRead()
        {
            var session = CreateSession();
            var plain = Plain(1500);
            var encrypted = HttpServerConnection.EncryptData(plain, session);

            const int firstRead = 2 + 1024 + 16 + 100; // first frame + a part of the second one
            var first = HttpServerConnection.DecryptData(encrypted.Take(firstRead).ToArray(), session, out var remainder);

            Assert.Equal(plain.Take(1024), first);
            Assert.Equal(100, remainder.Length);
            Assert.Equal(1, session.InboundBinaryMessageCount);

            var second = HttpServerConnection.DecryptData(remainder.Concat(encrypted.Skip(firstRead)).ToArray(), session, out remainder);

            Assert.Equal(plain.Skip(1024), second);
            Assert.Empty(remainder);
        }

        [Fact]
        public void LengthPrefixOnly_IsKeptForTheNextRead()
        {
            var session = CreateSession();
            var encrypted = HttpServerConnection.EncryptData(Plain(10), session);

            var result = HttpServerConnection.DecryptData(encrypted.Take(1).ToArray(), session, out var remainder);

            Assert.Empty(result);
            Assert.Single(remainder);
            Assert.Equal(0, session.InboundBinaryMessageCount);
        }
    }

    [Collection(HapStaticsCollection.Name)]
    public class PairSetupMiddlewareTests
    {
        private const string PairCode = "031-45-154";
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private Tlv InvokeM1(HapMiddleware middleware, string connectionId)
        {
            var request = new Tlv();
            request.AddType(Constants.Method, 0);
            request.AddType(Constants.State, 1);

            var response = middleware.Invoke(connectionId, "pair-setup", "POST", _parser.Serialize(request), new ConnectionSession());
            return _parser.Parse(response.Item2);
        }

        [Fact]
        public void SecondPairSetup_IsBusyWithStateTwo_UntilTheFirstSessionTerminates()
        {
            HapControllerServer.ClearPairing();
            var middleware = new HapMiddleware(NullLogger.Instance, PairCode, null);

            var first = InvokeM1(middleware, "a");
            Assert.False(first.HasType(Constants.Error));
            Assert.Equal(2, first.GetTypeAsInt(Constants.State));

            var busy = InvokeM1(middleware, "b");
            Assert.Equal((int)ErrorCodes.Busy, busy.GetTypeAsInt(Constants.Error));
            Assert.Equal(2, busy.GetTypeAsInt(Constants.State));

            // the connection of the first pair-setup is gone, pairing must be possible again
            middleware.TerminateSession("a");

            var retry = InvokeM1(middleware, "b");
            Assert.False(retry.HasType(Constants.Error));
            Assert.Equal(2, retry.GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void PairSetup_WhenAlreadyPaired_IsUnavailable()
        {
            var server = new HapControllerServer(NullLogger.Instance, null, 0, "aa", "bb", "FC:22:3D:E3:CE:F2", PairCode);
            try
            {
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, null);

                var response = InvokeM1(middleware, "a");

                Assert.Equal((int)ErrorCodes.Unavailable, response.GetTypeAsInt(Constants.Error));
                Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            }
            finally
            {
                server.Stop().GetAwaiter().GetResult();
                HapControllerServer.ClearPairing();
            }
        }
    }

    [Collection(HapStaticsCollection.Name)]
    public class CharacteristicSubscriptionTests
    {
        private static Dictionary<string, List<Characteristic>> Subscriptions(HomeKitServer server)
        {
            var field = typeof(HomeKitServer).GetField("_eventBasedNotifications", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Dictionary<string, List<Characteristic>>)field!.GetValue(server);
        }

        private static void Put(HomeKitServer server, HapSession session, ulong aid, int iid, string extra)
        {
            var json = $"{{\"characteristics\":[{{\"aid\":{aid},\"iid\":{iid}{extra}}}]}}";
            new CharacteristicsController(NullLogger.Instance).Put(Encoding.UTF8.GetBytes(json), session, server);
        }

        [Fact]
        public void EvFalse_Unsubscribes_AndValueWrites_DoNotSubscribe()
        {
            var server = new HomeKitServer(NullLogger.Instance, 0, "test", "", "", "FC:22:3D:E3:CE:F2", "031-45-154", "m", "bridge", 1, "1");
            try
            {
                var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);
                server.AddAccessory(accessory);
                var on = accessory.Services[1].Characteristics.First(a => a.Type == CharacteristicBase.OnType);
                var session = new HapSession { ClientUsername = "controller" };

                Put(server, session, 2, on.Id, ",\"value\":true");
                Assert.False(Subscriptions(server).TryGetValue("controller", out var afterWrite) && afterWrite.Contains(on));

                Put(server, session, 2, on.Id, ",\"ev\":true");
                Assert.Contains(on, Subscriptions(server)["controller"]);

                Put(server, session, 2, on.Id, ",\"ev\":false");
                Assert.DoesNotContain(on, Subscriptions(server)["controller"]);
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }

        [Fact]
        public void HueAndBrightness_AreWritable()
        {
            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);

            Assert.Contains("pw", accessory.CreateHue().Permissions);
            Assert.Contains("pw", accessory.CreateBrightness().Permissions);
        }
    }

    [Collection(HapStaticsCollection.Name)]
    public class PairingsEndpointTests
    {
        private const string PairCode = "031-45-154";
        private const string ControllerLtpk = "bb";
        private static readonly byte[] ControllerId = Encoding.UTF8.GetBytes("controller-1");
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private static HomeKitServer CreatePairedServer()
        {
            return new HomeKitServer(NullLogger.Instance, 0, "test", "aa", ControllerLtpk, "FC:22:3D:E3:CE:F2", PairCode, "m", "bridge", 1, "1");
        }

        private static HapSession Verify(HapMiddleware middleware, string connectionId)
        {
            var session = middleware.GetSession(connectionId);
            session.IsVerified = true;
            session.ClientIdentifier = ControllerId;
            session.ClientUsername = "controller-1";
            return session;
        }

        private Tlv Invoke(HapMiddleware middleware, string connectionId, string url, int method, byte[] identifier = null)
        {
            var request = new Tlv();
            request.AddType(Constants.State, 1);
            request.AddType(Constants.Method, method);
            if (identifier != null)
            {
                request.AddType(Constants.Identifier, identifier);
            }

            var response = middleware.Invoke(connectionId, url, "POST", _parser.Serialize(request), new ConnectionSession());
            return _parser.Parse(response.Item2);
        }

        [Fact]
        public void RemoveOwnPairing_Unpairs_AndAllowsANewPairSetup()
        {
            var server = CreatePairedServer();
            try
            {
                var removedEvents = 0;
                server.PairingRemoved += (s, e) => removedEvents++;
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, server);
                var session = Verify(middleware, "a");
                Assert.True(server.AdvertisedAsPaired);

                var response = Invoke(middleware, "a", "pairings", 4, ControllerId);

                Assert.Equal(2, response.GetTypeAsInt(Constants.State));
                Assert.False(response.HasType(Constants.Error));
                Assert.Equal(1, removedEvents); // the driver clears the persisted keys on this event
                Assert.True(string.IsNullOrEmpty(HapControllerServer.HapControllerLtpk));
                Assert.True(string.IsNullOrEmpty(HapControllerServer.HapControllerLtsk));
                Assert.False(server.AdvertisedAsPaired); // => sf=1
                Assert.True(session.CloseAfterResponse);

                var m2 = Invoke(middleware, "b", "pair-setup", 0);
                Assert.False(m2.HasType(Constants.Error));
                Assert.Equal(2, m2.GetTypeAsInt(Constants.State));
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }

        [Fact]
        public void RemoveUnknownPairing_IsSuccess_AndKeepsThePairing()
        {
            var server = CreatePairedServer();
            try
            {
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, server);
                var session = Verify(middleware, "a");

                var response = Invoke(middleware, "a", "pairings", 4, Encoding.UTF8.GetBytes("other"));

                Assert.False(response.HasType(Constants.Error));
                Assert.Equal(ControllerLtpk, HapControllerServer.HapControllerLtpk);
                Assert.True(server.AdvertisedAsPaired);
                Assert.False(session.CloseAfterResponse);
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }

        [Fact]
        public void ListPairings_ReturnsTheAdminController()
        {
            var server = CreatePairedServer();
            try
            {
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, server);
                Verify(middleware, "a");

                var response = Invoke(middleware, "a", "pairings", 5);

                Assert.Equal(2, response.GetTypeAsInt(Constants.State));
                Assert.Equal(ControllerId, response.GetType(Constants.Identifier));
                Assert.Equal(new byte[] { 0xbb }, response.GetType(Constants.PublicKey));
                Assert.Equal(1, response.GetTypeAsInt(Constants.Permissions));
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }

        [Fact]
        public void AddPairing_OnlyForTheSameController()
        {
            var server = CreatePairedServer();
            try
            {
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, server);
                Verify(middleware, "a");

                var other = Invoke(middleware, "a", "pairings", 3, Encoding.UTF8.GetBytes("other"));
                Assert.Equal((int)ErrorCodes.Unavailable, other.GetTypeAsInt(Constants.Error));
                Assert.Equal(2, other.GetTypeAsInt(Constants.State));

                var same = Invoke(middleware, "a", "pairings", 3, ControllerId);
                Assert.False(same.HasType(Constants.Error));
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }

        [Fact]
        public void Pairings_FromAnUnverifiedSession_AreRejected()
        {
            var server = CreatePairedServer();
            try
            {
                var middleware = new HapMiddleware(NullLogger.Instance, PairCode, server);

                var response = Invoke(middleware, "a", "pairings", 4, ControllerId);

                Assert.Equal((int)ErrorCodes.Authentication, response.GetTypeAsInt(Constants.Error));
                Assert.Equal(ControllerLtpk, HapControllerServer.HapControllerLtpk);
            }
            finally
            {
                HapControllerServer.ClearPairing();
            }
        }
    }
}
