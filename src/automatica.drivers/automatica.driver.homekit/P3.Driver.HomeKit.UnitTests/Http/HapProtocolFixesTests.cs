using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
    public class EncryptedFrameTests
    {
        private static HapSession CreateSession()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            // the same key in both directions so EncryptData produces frames DecryptData can read
            return new HapSession { AccessoryToControllerKey = key, ControllerToAccessoryKey = key };
        }

        private static HapSession CreateSessionWithKeyOf(HapSession session)
        {
            return new HapSession { AccessoryToControllerKey = session.AccessoryToControllerKey, ControllerToAccessoryKey = session.ControllerToAccessoryKey };
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

        [Fact]
        public void FrameLengthAbove1024_IsInvalid_EvenBeforeTheFrameIsComplete()
        {
            var session = CreateSession();
            var lengthOnly = BitConverter.GetBytes((ushort)(HttpServerConnection.MaxFrameLength + 1));

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(lengthOnly, session, out _));
            Assert.Equal(0, session.InboundBinaryMessageCount);
        }

        [Fact]
        public void TamperedFrame_IsInvalid()
        {
            var session = CreateSession();
            var encrypted = HttpServerConnection.EncryptData(Plain(10), session);
            encrypted[5] ^= 0xff;

            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(encrypted, CreateSessionWithKeyOf(session), out _));
        }

        [Fact]
        public void CompleteBufferOverload_RejectsATooShortFrame()
        {
            var session = CreateSession();
            var encrypted = HttpServerConnection.EncryptData(Plain(10), session);

            Assert.Equal(Plain(10), HttpServerConnection.DecryptData(encrypted, CreateSessionWithKeyOf(session)));
            Assert.Throws<InvalidDataException>(() => HttpServerConnection.DecryptData(encrypted.Take(encrypted.Length - 1).ToArray(), CreateSessionWithKeyOf(session)));
        }
    }

    public class PairSetupMiddlewareTests
    {
        private const string PairCode = "031-45-154";
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private Tlv InvokePairSetup(HapMiddleware middleware, string connectionId, int state)
        {
            var request = new Tlv();
            request.AddType(Constants.Method, 0);
            request.AddType(Constants.State, state);

            var response = middleware.Invoke(connectionId, "pair-setup", "POST", _parser.Serialize(request), new ConnectionSession());
            return _parser.Parse(response.Body);
        }

        [Fact]
        public void SecondPairSetup_IsBusyWithStateTwo_UntilTheFirstSessionTerminates()
        {
            var middleware = new HapMiddleware(NullLogger.Instance, PairCode, null, new HapPairingStore(null, null));

            var first = InvokePairSetup(middleware, "a", 1);
            Assert.False(first.HasType(Constants.Error));
            Assert.Equal(2, first.GetTypeAsInt(Constants.State));

            var busy = InvokePairSetup(middleware, "b", 1);
            Assert.Equal((int)ErrorCodes.Busy, busy.GetTypeAsInt(Constants.Error));
            Assert.Equal(2, busy.GetTypeAsInt(Constants.State));

            // the connection of the first pair-setup is gone, pairing must be possible again
            middleware.TerminateSession("a");

            var retry = InvokePairSetup(middleware, "b", 1);
            Assert.False(retry.HasType(Constants.Error));
            Assert.Equal(2, retry.GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void ErrorResponses_CarryTheStateOfTheResponse()
        {
            var paired = new HapPairingStore(null, null);
            Assert.True(paired.CompletePairSetup(Ltsk, "controller-1", new string('a', 64)));
            var pairedMiddleware = new HapMiddleware(NullLogger.Instance, PairCode, null, paired);

            // M1 => M2, M3 => M4
            var m2 = InvokePairSetup(pairedMiddleware, "a", 1);
            Assert.Equal((int)ErrorCodes.Unavailable, m2.GetTypeAsInt(Constants.Error));
            Assert.Equal(2, m2.GetTypeAsInt(Constants.State));

            var m4 = InvokePairSetup(pairedMiddleware, "a", 3);
            Assert.Equal((int)ErrorCodes.Unavailable, m4.GetTypeAsInt(Constants.Error));
            Assert.Equal(4, m4.GetTypeAsInt(Constants.State));

            // M5 without a pending setup on the connection => M6
            var unpairedMiddleware = new HapMiddleware(NullLogger.Instance, PairCode, null, new HapPairingStore(null, null));
            var m6 = InvokePairSetup(unpairedMiddleware, "a", 5);
            Assert.Equal((int)ErrorCodes.Unknown, m6.GetTypeAsInt(Constants.Error));
            Assert.Equal(6, m6.GetTypeAsInt(Constants.State));
        }
    }

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

        [Fact]
        public void HueAndBrightness_AreWritable()
        {
            var accessory = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);

            Assert.Contains("pw", accessory.CreateHue().Permissions);
            Assert.Contains("pw", accessory.CreateBrightness().Permissions);
        }
    }

    public class PairingsEndpointTests
    {
        private const string PairCode = "031-45-154";
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private const string ControllerId = "controller-1";
        private static readonly string ControllerLtpk = new string('b', 64);
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);

        private static HomeKitServer CreatePairedServer()
        {
            var store = new HapPairingStore(null, null);
            Assert.True(store.CompletePairSetup(Ltsk, ControllerId, ControllerLtpk));

            // loaded the way the driver persists it: accessory LTSK + serialized controller list
            return new HomeKitServer(NullLogger.Instance, 0, "test", Ltsk, store.Serialize(), "FC:22:3D:E3:CE:F2", PairCode, "m", "bridge", 1, "1");
        }

        private static HapMiddleware CreateMiddleware(HomeKitServer server)
        {
            return new HapMiddleware(NullLogger.Instance, PairCode, server, server.PairingStore);
        }

        private static HapSession Verify(HapMiddleware middleware, string connectionId)
        {
            var session = middleware.GetSession(connectionId);
            session.IsVerified = true;
            session.ControllerId = ControllerId;
            session.ClientUsername = "636f6e74726f6c6c65722d31";
            session.IsAdmin = true;
            return session;
        }

        private HapResponse InvokeRaw(HapMiddleware middleware, string connectionId, string url, int method, string identifier = null)
        {
            var request = new Tlv();
            request.AddType(Constants.State, 1);
            request.AddType(Constants.Method, method);
            if (identifier != null)
            {
                request.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(identifier));
            }

            return middleware.Invoke(connectionId, url, "POST", _parser.Serialize(request), new ConnectionSession());
        }

        private Tlv Invoke(HapMiddleware middleware, string connectionId, string url, int method, string identifier = null)
        {
            return _parser.Parse(InvokeRaw(middleware, connectionId, url, method, identifier).Body);
        }

        [Fact]
        public void RemoveOwnPairing_Unpairs_PersistsTheClearedKeys_AndAllowsANewPairSetup()
        {
            var server = CreatePairedServer();
            var persisted = new List<PairSetupCompleteEventArgs>();
            server.PairingCompleted += (s, e) => persisted.Add(e); // the driver saves every change to the database
            var middleware = CreateMiddleware(server);
            var session = Verify(middleware, "a");
            Assert.True(server.AdvertisedAsPaired);

            var response = Invoke(middleware, "a", "pairings", PairingController.MethodRemovePairing, ControllerId);

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.False(response.HasType(Constants.Error));

            var saved = Assert.Single(persisted);
            Assert.True(string.IsNullOrEmpty(saved.Ltsk));
            Assert.False(new HapPairingStore(saved.Ltsk, saved.Ltpk).IsPaired); // a restart comes up unpaired
            Assert.True(string.IsNullOrEmpty(server.PairingStore.AccessoryLtsk));
            Assert.False(server.AdvertisedAsPaired); // => sf=1
            Assert.True(session.CloseAfterResponse);

            var m2 = Invoke(middleware, "b", "pair-setup", 0);
            Assert.False(m2.HasType(Constants.Error));
            Assert.Equal(2, m2.GetTypeAsInt(Constants.State));
        }

        [Fact]
        public void RemovePairing_ClosesTheControllersOtherConnections()
        {
            var server = CreatePairedServer();
            var middleware = CreateMiddleware(server);
            var current = Verify(middleware, "a");
            var other = Verify(middleware, "b");
            other.Client = new TcpClient();
            var unrelated = middleware.GetSession("c");
            unrelated.Client = new TcpClient();

            Invoke(middleware, "a", "pairings", PairingController.MethodRemovePairing, ControllerId);

            // the current connection still has to send its response, it closes itself afterwards
            Assert.True(current.CloseAfterResponse);
            Assert.False(other.CloseAfterResponse);
            Assert.Throws<ObjectDisposedException>(() => other.Client.GetStream());
            Assert.Throws<InvalidOperationException>(() => unrelated.Client.GetStream()); // not connected, but not closed
        }

        [Fact]
        public void RemoveUnknownPairing_IsSuccess_AndKeepsThePairing()
        {
            var server = CreatePairedServer();
            var middleware = CreateMiddleware(server);
            var session = Verify(middleware, "a");

            var response = Invoke(middleware, "a", "pairings", PairingController.MethodRemovePairing, "other");

            Assert.False(response.HasType(Constants.Error));
            Assert.Equal(ControllerLtpk, server.PairingStore.Find(ControllerId).PublicKey);
            Assert.True(server.AdvertisedAsPaired);
            Assert.False(session.CloseAfterResponse);
        }

        [Fact]
        public void ListPairings_ReturnsTheAdminController()
        {
            var server = CreatePairedServer();
            var middleware = CreateMiddleware(server);
            Verify(middleware, "a");

            var response = Invoke(middleware, "a", "pairings", PairingController.MethodListPairings);

            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal(Encoding.UTF8.GetBytes(ControllerId), response.GetType(Constants.Identifier));
            Assert.Equal(Enumerable.Repeat((byte)0xbb, 32).ToArray(), response.GetType(Constants.PublicKey));
            Assert.Equal(1, response.GetTypeAsInt(Constants.Permissions));
        }

        [Fact]
        public void Pairings_FromAnUnverifiedSession_AreRejected()
        {
            var server = CreatePairedServer();
            var middleware = CreateMiddleware(server);

            var response = InvokeRaw(middleware, "a", "pairings", PairingController.MethodRemovePairing, ControllerId);

            Assert.Equal(470, response.StatusCode);
            Assert.NotNull(server.PairingStore.Find(ControllerId));
            Assert.True(server.AdvertisedAsPaired);
        }
    }
}
