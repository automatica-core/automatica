using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKit.Hap.TlvData;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Integration
{
    /// <summary>
    /// End-to-end HAP over a real loopback TCP connection: SRP pair-setup, pair-verify (X25519 + Ed25519), then
    /// ChaCha20-Poly1305 framed requests, with a test controller built from NSec / SecureRemotePassword.
    /// </summary>
    public class HapEndToEndTests
    {
        private const string WrongCode = "111-22-333";

        private sealed class Bridge : IAsyncDisposable
        {
            public LoopbackHapHost Host { get; }
            public HomeKitServer Server => Host.Server;
            public Accessory Lamp { get; }
            public Characteristic On { get; }
            public List<PairSetupCompleteEventArgs> Persisted { get; } = new List<PairSetupCompleteEventArgs>();
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            public Bridge(string ltsk = "", string pairings = "")
            {
                var server = LoopbackHapHost.CreateServer(ltsk, pairings);
                Lamp = AccessoryFactory.CreateLightBulbAccessory(2, "lamp", "AutomaticaCore", "1", false, 0);
                server.AddAccessory(Lamp);
                On = Lamp.Services[1].Characteristics.First(a => a.Type == CharacteristicBase.OnType);
                server.PairingCompleted += (s, e) =>
                {
                    lock (Persisted)
                    {
                        Persisted.Add(e);
                    }
                };
                Host = new LoopbackHapHost(server);
            }

            public T Track<T>(T disposable) where T : IDisposable
            {
                _disposables.Add(disposable);
                return disposable;
            }

            public async Task<HapTestConnection> ConnectAsync()
            {
                return Track(await HapTestConnection.ConnectAsync(Host.Port));
            }

            public HapControllerIdentity NewController(string id)
            {
                return Track(new HapControllerIdentity(id));
            }

            /// <summary>
            /// Pair-setup with <paramref name="id"/> on its own connection.
            /// </summary>
            public async Task<HapControllerIdentity> PairAsync(string id = "controller-A")
            {
                var identity = NewController(id);
                var connection = await ConnectAsync();
                var result = await connection.PairSetupAsync(identity, LoopbackHapHost.PairCode);
                Assert.True(result.Success, $"pair-setup failed in M{result.FailedState}: {result.Error}");
                return identity;
            }

            public async Task<HapTestConnection> VerifiedConnectionAsync(HapControllerIdentity identity)
            {
                var connection = await ConnectAsync();
                Assert.Null(await connection.PairVerifyAsync(identity));
                return connection;
            }

            public async ValueTask DisposeAsync()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }

                await Host.DisposeAsync();
            }
        }

        private static async Task<bool> Eventually(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        private static string Characteristics(int iid, string extra)
        {
            return $"{{\"characteristics\":[{{\"aid\":2,\"iid\":{iid}{extra}}}]}}";
        }

        [Fact]
        public async Task PairSetup_PairVerify_EncryptedGetAccessories_ReturnsTheAccessoryDatabase()
        {
            await using var bridge = new Bridge();
            var controller = await bridge.PairAsync();

            var connection = await bridge.VerifiedConnectionAsync(controller);
            var response = await connection.SendAsync("GET", "accessories");

            Assert.Equal(200, response.StatusCode);
            Assert.Equal("application/hap+json", response.ContentType);
            var json = JObject.Parse(response.BodyText);
            var aids = json["accessories"].Select(a => (ulong)a["aid"]).ToList();
            Assert.Equal(new ulong[] { 1, 2 }, aids.OrderBy(a => a));
            var lamp = json["accessories"].Single(a => (ulong)a["aid"] == 2);
            Assert.Contains(lamp["services"], s => (string)s["type"] == AccessoryFactory.LightBulbType);
        }

        [Fact]
        public async Task PairSetup_Completes_StoresTheControllerAsAdmin_AndAdvertisesPaired()
        {
            await using var bridge = new Bridge();
            Assert.False(bridge.Server.AdvertisedAsPaired);

            var controller = await bridge.PairAsync("controller-A");

            Assert.True(bridge.Server.IsPaired);
            Assert.True(bridge.Server.AdvertisedAsPaired); // sf=0
            var pairing = Assert.Single(bridge.Server.PairingStore.Pairings);
            Assert.Equal("controller-A", pairing.Identifier);
            Assert.Equal(controller.PublicKeyHex, pairing.PublicKey, ignoreCase: true);
            Assert.True(pairing.IsAdmin);
            Assert.Equal(LoopbackHapHost.DeviceId, controller.AccessoryPairingId);
            Assert.Equal(32, controller.AccessoryLtpk.Length);
        }

        [Fact]
        public async Task PairSetup_Completes_PersistsKeysThatSurviveARestart()
        {
            PairSetupCompleteEventArgs saved;
            using var controller = new HapControllerIdentity("controller-A");
            await using (var bridge = new Bridge())
            {
                var setup = await bridge.ConnectAsync();
                Assert.True((await setup.PairSetupAsync(controller, LoopbackHapHost.PairCode)).Success);
                Assert.NotEmpty(bridge.Persisted);
                saved = bridge.Persisted.Last();
                Assert.False(string.IsNullOrEmpty(saved.Ltsk));
            }

            await using var restarted = new Bridge(saved.Ltsk, saved.Ltpk);
            Assert.True(restarted.Server.IsPaired);

            var connection = await restarted.ConnectAsync();
            Assert.Null(await connection.PairVerifyAsync(controller));
            Assert.Equal(200, (await connection.SendAsync("GET", "accessories")).StatusCode);
        }

        [Fact]
        public async Task PairVerify_OnTheSetupConnection_Works()
        {
            await using var bridge = new Bridge();
            var controller = bridge.NewController("controller-A");
            var connection = await bridge.ConnectAsync();

            Assert.True((await connection.PairSetupAsync(controller, LoopbackHapHost.PairCode)).Success);
            Assert.Null(await connection.PairVerifyAsync(controller));

            Assert.Equal(200, (await connection.SendAsync("GET", "accessories")).StatusCode);
        }

        [Fact]
        public async Task GetAccessories_OnAnUnverifiedConnectionOfAPairedBridge_Returns470()
        {
            await using var bridge = new Bridge();
            await bridge.PairAsync();
            var connection = await bridge.ConnectAsync();

            var response = await connection.SendAsync("GET", "accessories");

            Assert.Equal(470, response.StatusCode);
            Assert.Equal("HTTP/1.1 470 Connection Authorization Required", response.StartLine);
            Assert.Equal(-70401, (int)JObject.Parse(response.BodyText)["status"]);
        }

        [Theory]
        [InlineData("GET", "accessories", null)]
        [InlineData("GET", "characteristics?id=2.10", null)]
        [InlineData("PUT", "characteristics", "{\"characteristics\":[{\"aid\":2,\"iid\":10,\"value\":true}]}")]
        public async Task Requests_OnAnUnpairedBridge_Return470_AndDoNotChangeValues(string method, string path, string body)
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();

            var response = await connection.SendAsync(method, path, body == null ? null : Encoding.UTF8.GetBytes(body), "application/hap+json");

            Assert.Equal(470, response.StatusCode);
            Assert.Equal(false, bridge.On.Value);
        }

        [Fact]
        public async Task Pairings_OnAnUnverifiedConnection_Returns470_AndKeepsThePairing()
        {
            await using var bridge = new Bridge();
            var controller = await bridge.PairAsync();
            var connection = await bridge.ConnectAsync();

            var response = await connection.SendAsync("POST", "pairings", new TlvParser(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Serialize(RemoveRequest(controller.Id)), "application/pairing+tlv8");

            Assert.Equal(470, response.StatusCode);
            Assert.True(bridge.Server.IsPaired);
        }

        private static Tlv RemoveRequest(string id)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, 1);
            tlv.AddType(Constants.Method, 4);
            tlv.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(id));
            return tlv;
        }

        [Fact]
        public async Task PairSetup_OnAPairedBridge_IsUnavailable()
        {
            await using var bridge = new Bridge();
            await bridge.PairAsync();
            var connection = await bridge.ConnectAsync();

            var result = await connection.PairSetupAsync(bridge.NewController("intruder"), LoopbackHapHost.PairCode);

            Assert.False(result.Success);
            Assert.Equal(2, result.FailedState);
            Assert.Equal(ErrorCodes.Unavailable, result.Error);
            Assert.Null(bridge.Server.PairingStore.Find("intruder"));
            Assert.Single(bridge.Server.PairingStore.Pairings);
        }

        [Fact]
        public async Task PairSetup_WithAWrongSetupCode_FailsInM4_AndCountsTheAttempt()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();

            var result = await connection.PairSetupAsync(bridge.NewController("controller-A"), WrongCode);

            Assert.False(result.Success);
            Assert.Equal(4, result.FailedState);
            Assert.Equal(ErrorCodes.Authentication, result.Error);
            Assert.False(bridge.Server.IsPaired);
            Assert.Equal(1, bridge.Server.PairingStore.FailedSetupAttempts);
            Assert.Contains(bridge.Persisted, e => e.Ltpk.Contains("\"failedSetupAttempts\":1"));
        }

        [Fact]
        public async Task PairSetup_AfterAWrongCode_CanBeRetriedWithTheRightCodeOnTheSameConnection()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();
            var controller = bridge.NewController("controller-A");

            Assert.False((await connection.PairSetupAsync(controller, WrongCode)).Success);
            var result = await connection.PairSetupAsync(controller, LoopbackHapHost.PairCode);

            Assert.True(result.Success);
            Assert.Equal(0, bridge.Server.PairingStore.FailedSetupAttempts);
        }

        [Fact]
        public async Task PairSetup_AfterMaxFailedAttempts_IsLockedOut_EvenWithTheRightCode()
        {
            const int alreadyFailed = HapPairingStore.MaxFailedSetupAttempts - 3;
            await using var bridge = new Bridge("", $"{{\"pairings\":[],\"failedSetupAttempts\":{alreadyFailed}}}");
            var controller = bridge.NewController("controller-A");

            for (var i = 0; i < 3; i++)
            {
                var connection = await bridge.ConnectAsync();
                var failed = await connection.PairSetupAsync(controller, WrongCode);
                Assert.Equal(ErrorCodes.Authentication, failed.Error);
            }

            Assert.True(bridge.Server.PairingStore.IsLockedOut);

            var last = await bridge.ConnectAsync();
            var locked = await last.PairSetupAsync(controller, LoopbackHapHost.PairCode);

            Assert.False(locked.Success);
            Assert.Equal(2, locked.FailedState);
            Assert.Equal(ErrorCodes.MaxTries, locked.Error);
            Assert.False(bridge.Server.IsPaired);

            // the lockout is persisted, a restart does not reset it
            var persisted = bridge.Persisted.Last();
            Assert.True(new HapPairingStore(persisted.Ltsk, persisted.Ltpk).IsLockedOut);
        }

        [Fact]
        public async Task PairSetup_WhileAnotherConnectionIsPairing_IsBusy_UntilThatConnectionCloses()
        {
            await using var bridge = new Bridge();
            var first = await bridge.ConnectAsync();
            Assert.False((await first.PairSetupM1Async()).HasType(Constants.Error));

            var second = await bridge.ConnectAsync();
            var busy = await second.PairSetupM1Async();
            Assert.Equal((int)ErrorCodes.Busy, busy.GetTypeAsInt(Constants.Error));
            Assert.Equal(2, busy.GetTypeAsInt(Constants.State));

            first.Dispose();

            var deadline = DateTime.UtcNow.AddSeconds(10);
            Tlv retry;
            do
            {
                retry = await second.PairSetupM1Async();
                if (!retry.HasType(Constants.Error))
                {
                    break;
                }

                await Task.Delay(20);
            } while (DateTime.UtcNow < deadline);

            Assert.False(retry.HasType(Constants.Error));
            Assert.True((await second.PairSetupAsync(bridge.NewController("controller-B"), LoopbackHapHost.PairCode)).Success);
        }

        [Fact]
        public async Task PairVerify_OnAnUnpairedBridge_FailsWithAuthentication()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();

            Assert.Equal(ErrorCodes.Authentication, await connection.PairVerifyAsync(bridge.NewController("controller-A")));
            Assert.False(connection.IsEncrypted);
        }

        [Fact]
        public async Task PairVerify_WithAnUnknownController_FailsInM4_AndTheConnectionStaysUnverified()
        {
            await using var bridge = new Bridge();
            var paired = await bridge.PairAsync("controller-A");
            var stranger = bridge.NewController("stranger");
            stranger.AccessoryLtpk = paired.AccessoryLtpk;
            var connection = await bridge.ConnectAsync();

            Assert.Equal(ErrorCodes.Authentication, await connection.PairVerifyAsync(stranger));

            Assert.Equal(470, (await connection.SendAsync("GET", "accessories")).StatusCode);
        }

        [Fact]
        public async Task PairVerify_WithTheRightIdButAnotherKey_FailsWithAuthentication()
        {
            await using var bridge = new Bridge();
            var paired = await bridge.PairAsync("controller-A");
            var impostor = bridge.NewController("controller-A"); // same id, different Ed25519 key
            impostor.AccessoryLtpk = paired.AccessoryLtpk;
            var connection = await bridge.ConnectAsync();

            Assert.Equal(ErrorCodes.Authentication, await connection.PairVerifyAsync(impostor));
        }

        [Fact]
        public async Task GetCharacteristics_Encrypted_ReturnsTheCurrentValue()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            bridge.On.Value = true;

            var response = await connection.SendAsync("GET", $"characteristics?id=2.{bridge.On.Id}");

            Assert.Equal(200, response.StatusCode);
            var characteristic = JObject.Parse(response.BodyText)["characteristics"].Single();
            Assert.Equal(2, (int)characteristic["aid"]);
            Assert.Equal(bridge.On.Id, (int)characteristic["iid"]);
            Assert.True((bool)characteristic["value"]);
        }

        [Fact]
        public async Task GetCharacteristics_WithoutId_Returns400InvalidValue()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            var response = await connection.SendAsync("GET", "characteristics");

            Assert.Equal(400, response.StatusCode);
            Assert.Equal(-70410, (int)JObject.Parse(response.BodyText)["status"]);
        }

        [Fact]
        public async Task PutCharacteristics_Encrypted_WritesTheValue_RaisesValueChanged_AndAnswers204()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            var changes = new List<object>();
            bridge.Server.ValueChanged += (s, e) => changes.Add(e.Value);

            var response = await connection.SendJsonAsync("PUT", "characteristics", Characteristics(bridge.On.Id, ",\"value\":true"));

            Assert.Equal(204, response.StatusCode);
            Assert.Equal(true, bridge.On.Value);
            Assert.Equal(new object[] { true }, changes);
        }

        [Fact]
        public async Task Subscribe_ThenServerValueChange_SendsAnEncryptedEvent_UnsubscribeStopsIt()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            Assert.Equal(204, (await connection.SendJsonAsync("PUT", "characteristics", Characteristics(bridge.On.Id, ",\"ev\":true"))).StatusCode);

            bridge.Server.SetCharacteristicValue(bridge.On, true);

            var evt = await connection.ReadMessageAsync();
            Assert.True(evt.IsEvent, evt.StartLine);
            var characteristic = JObject.Parse(evt.BodyText)["characteristics"].Single();
            Assert.Equal(bridge.On.Id, (int)characteristic["iid"]);
            Assert.Equal(2, (int)characteristic["aid"]);
            Assert.True((bool)characteristic["value"]);

            Assert.Equal(204, (await connection.SendJsonAsync("PUT", "characteristics", Characteristics(bridge.On.Id, ",\"ev\":false"))).StatusCode);

            // an event would be written synchronously before the GET response
            bridge.Server.SetCharacteristicValue(bridge.On, false);
            var next = await connection.SendAsync("GET", "accessories");
            Assert.False(next.IsEvent);
            Assert.Equal(200, next.StatusCode);
        }

        [Fact]
        public async Task ValueWrite_WithoutEv_DoesNotSubscribe()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            Assert.Equal(204, (await connection.SendJsonAsync("PUT", "characteristics", Characteristics(bridge.On.Id, ",\"value\":true"))).StatusCode);
            bridge.Server.SetCharacteristicValue(bridge.On, false);

            var next = await connection.SendAsync("GET", "accessories");
            Assert.False(next.IsEvent);
        }

        [Fact]
        public async Task EncryptedRequest_SplitInsideAFrame_IsBufferedAndAnswered()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            var request = connection.Encrypt(HapTestConnection.BuildRequest("GET", "accessories", null, null));

            await connection.WriteRawAsync(request.Take(1).ToArray()); // only half of the length prefix
            await Task.Delay(50);
            await connection.WriteRawAsync(request.Skip(1).Take(20).ToArray());
            await Task.Delay(50);
            await connection.WriteRawAsync(request.Skip(21).ToArray());

            var response = await connection.ReadMessageAsync();
            Assert.Equal(200, response.StatusCode);
            Assert.Contains("\"aid\":2", response.BodyText);
        }

        [Fact]
        public async Task EncryptedRequest_LargerThanOneFrame_InOneWrite_IsAnswered()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            var json = Characteristics(bridge.On.Id, ",\"value\":true" + new string(' ', 1500).Insert(0, ",\"pad\":\"") + "\"");
            var request = HapTestConnection.BuildRequest("PUT", "characteristics", Encoding.UTF8.GetBytes(json), "application/hap+json");
            Assert.True(request.Length > 1024);

            await connection.WriteRawAsync(connection.Encrypt(request));

            Assert.Equal(204, (await connection.ReadMessageAsync()).StatusCode);
            Assert.Equal(true, bridge.On.Value);
        }

        [Fact]
        public async Task SeveralRequests_KeepTheNonceCountersInSync()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            for (var i = 0; i < 5; i++)
            {
                Assert.Equal(200, (await connection.SendAsync("GET", "accessories")).StatusCode); // > 1024 bytes => several response frames
            }
        }

        [Fact(Skip = "BUG: HttpServerConnection.HandleClient (HttpServerConnection.cs:83-133) parses the HTTP request as soon as one frame decrypted; a request whose second frame arrives in a later read fails the content-length check and the connection is closed")]
        public async Task EncryptedRequest_SecondFrameInALaterRead_IsAnswered()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            var json = Characteristics(bridge.On.Id, ",\"value\":true" + new string(' ', 1500).Insert(0, ",\"pad\":\"") + "\"");
            var frames = connection.Encrypt(HapTestConnection.BuildRequest("PUT", "characteristics", Encoding.UTF8.GetBytes(json), "application/hap+json"));
            const int firstFrame = 2 + 1024 + 16;

            await connection.WriteRawAsync(frames.Take(firstFrame).ToArray());
            await Task.Delay(100);
            await connection.WriteRawAsync(frames.Skip(firstFrame).ToArray());

            Assert.Equal(204, (await connection.ReadMessageAsync()).StatusCode);
        }

        [Fact(Skip = "BUG: HttpServerConnection.HandleClient (HttpServerConnection.cs:135-142) treats every read as a complete plain-text request; a pair-setup body that arrives in a second TCP read is taken from the header bytes instead")]
        public async Task PlainRequest_BodyInALaterRead_IsAnswered()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();
            var tlv = new Tlv();
            tlv.AddType(Constants.Method, 0);
            tlv.AddType(Constants.State, 1);
            var request = HapTestConnection.BuildRequest("POST", "pair-setup", new TlvParser(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Serialize(tlv), "application/pairing+tlv8");

            await connection.WriteRawAsync(request.Take(request.Length - 6).ToArray());
            await Task.Delay(100);
            await connection.WriteRawAsync(request.Skip(request.Length - 6).ToArray());

            var response = await connection.ReadMessageAsync();
            var m2 = new TlvParser(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Parse(response.Body);
            Assert.Equal(2, m2.GetTypeAsInt(Constants.State));
            Assert.False(m2.HasType(Constants.Error));
        }

        [Fact]
        public async Task EncryptedFrame_WithALengthAbove1024_ClosesTheConnection()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            await connection.WriteRawAsync(connection.EncryptSingleFrame(new byte[10], 1025));

            Assert.True(await connection.WaitForCloseAsync());
        }

        [Fact]
        public async Task EncryptedFrame_Tampered_ClosesTheConnection()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());
            var request = connection.Encrypt(HapTestConnection.BuildRequest("GET", "accessories", null, null));
            request[10] ^= 0x01;

            await connection.WriteRawAsync(request);

            Assert.True(await connection.WaitForCloseAsync());
        }

        [Fact]
        public async Task PlainRequest_AfterPairVerify_IsRejected_AndClosesTheConnection()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            await connection.WriteRawAsync(HapTestConnection.BuildRequest("GET", "accessories", null, null));

            Assert.True(await connection.WaitForCloseAsync());
        }

        [Fact]
        public async Task Identify_WhileUnpaired_Answers204()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();

            Assert.Equal(204, (await connection.SendAsync("POST", "identify")).StatusCode);
        }

        [Fact]
        public async Task Identify_WhilePaired_Answers400()
        {
            await using var bridge = new Bridge();
            await bridge.PairAsync();
            var connection = await bridge.ConnectAsync();

            var response = await connection.SendAsync("POST", "identify");

            Assert.Equal(400, response.StatusCode);
            Assert.Equal(-70401, (int)JObject.Parse(response.BodyText)["status"]);
        }

        [Fact]
        public async Task Admin_AddsANonAdminController_WhichCanVerifyAndRead_ButNotManagePairings()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var adminConnection = await bridge.VerifiedConnectionAsync(admin);
            var guest = bridge.NewController("controller-B");
            guest.AccessoryLtpk = admin.AccessoryLtpk;

            var added = await adminConnection.AddPairingAsync(guest.Id, guest.PublicKey, admin: false);
            Assert.Equal(2, added.GetTypeAsInt(Constants.State));
            Assert.False(added.HasType(Constants.Error));

            var guestConnection = await bridge.VerifiedConnectionAsync(guest);
            Assert.Equal(200, (await guestConnection.SendAsync("GET", "accessories")).StatusCode);

            var third = bridge.NewController("controller-C");
            var addByGuest = await guestConnection.AddPairingAsync(third.Id, third.PublicKey, admin: true);
            Assert.Equal((int)ErrorCodes.Authentication, addByGuest.GetTypeAsInt(Constants.Error));
            Assert.Equal(2, addByGuest.GetTypeAsInt(Constants.State));

            var removeByGuest = await guestConnection.RemovePairingAsync(admin.Id);
            Assert.Equal((int)ErrorCodes.Authentication, removeByGuest.GetTypeAsInt(Constants.Error));

            var listByGuest = await guestConnection.ListPairingsRawAsync();
            Assert.Equal((int)ErrorCodes.Authentication, new TlvParser(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance).Parse(listByGuest.Body).GetTypeAsInt(Constants.Error));

            Assert.Equal(2, bridge.Server.PairingStore.Pairings.Count);
            Assert.Null(bridge.Server.PairingStore.Find("controller-C"));
            Assert.True(bridge.Server.PairingStore.Find("controller-A").IsAdmin);
        }

        [Fact]
        public async Task Admin_ListPairings_ReturnsAllControllersWithPermissions()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var connection = await bridge.VerifiedConnectionAsync(admin);
            var guest = bridge.NewController("controller-B");
            await connection.AddPairingAsync(guest.Id, guest.PublicKey, admin: false);

            var list = await connection.ListPairingsRawAsync();

            Assert.Equal(200, list.StatusCode);
            // walk the TLV items, the random keys may contain 0xff bytes
            var items = new List<(int type, byte[] value)>();
            for (var i = 0; i < list.Body.Length; i += 2 + list.Body[i + 1])
            {
                items.Add((list.Body[i], list.Body.Skip(i + 2).Take(list.Body[i + 1]).ToArray()));
            }

            Assert.Equal((int)Constants.State, items[0].type);
            Assert.Single(items, a => a.type == (int)Constants.Separator);
            var ids = items.Where(a => a.type == (int)Constants.Identifier).Select(a => Encoding.UTF8.GetString(a.value)).ToList();
            var keys = items.Where(a => a.type == (int)Constants.PublicKey).Select(a => a.value).ToList();
            var permissions = items.Where(a => a.type == (int)Constants.Permissions).Select(a => a.value.Single()).ToList();
            Assert.Equal(new[] { "controller-A", "controller-B" }, ids);
            Assert.Equal(admin.PublicKey, keys[0]);
            Assert.Equal(guest.PublicKey, keys[1]);
            Assert.Equal(new byte[] { 1, 0 }, permissions);
        }

        [Fact]
        public async Task Admin_PromotesAController_WhichCanThenAddPairings()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var adminConnection = await bridge.VerifiedConnectionAsync(admin);
            var second = bridge.NewController("controller-B");
            second.AccessoryLtpk = admin.AccessoryLtpk;
            await adminConnection.AddPairingAsync(second.Id, second.PublicKey, admin: false);
            await adminConnection.AddPairingAsync(second.Id, second.PublicKey, admin: true);

            var secondConnection = await bridge.VerifiedConnectionAsync(second);
            var third = bridge.NewController("controller-C");
            var added = await secondConnection.AddPairingAsync(third.Id, third.PublicKey, admin: false);

            Assert.False(added.HasType(Constants.Error));
            Assert.NotNull(bridge.Server.PairingStore.Find("controller-C"));
        }

        [Fact]
        public async Task RemovePairing_ClosesTheRemovedControllersConnections_AndRevokesItsKey()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var adminConnection = await bridge.VerifiedConnectionAsync(admin);
            var guest = bridge.NewController("controller-B");
            guest.AccessoryLtpk = admin.AccessoryLtpk;
            await adminConnection.AddPairingAsync(guest.Id, guest.PublicKey, admin: false);
            var guestConnection1 = await bridge.VerifiedConnectionAsync(guest);
            var guestConnection2 = await bridge.VerifiedConnectionAsync(guest);

            var removed = await adminConnection.RemovePairingAsync(guest.Id);

            Assert.False(removed.HasType(Constants.Error));
            Assert.True(await guestConnection1.WaitForCloseAsync());
            Assert.True(await guestConnection2.WaitForCloseAsync());

            // the admin connection is untouched
            Assert.Equal(200, (await adminConnection.SendAsync("GET", "accessories")).StatusCode);

            var again = await bridge.ConnectAsync();
            Assert.Equal(ErrorCodes.Authentication, await again.PairVerifyAsync(guest));
            Assert.True(bridge.Server.IsPaired);
        }

        [Fact]
        public async Task RemoveOwnPairing_AnswersOnTheSession_ThenClosesIt_AndTheBridgeIsPairableAgain()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var connection = await bridge.VerifiedConnectionAsync(admin);

            var removed = await connection.RemovePairingAsync(admin.Id);

            Assert.Equal(2, removed.GetTypeAsInt(Constants.State));
            Assert.False(removed.HasType(Constants.Error));
            Assert.True(await connection.WaitForCloseAsync());
            Assert.False(bridge.Server.IsPaired);
            Assert.False(bridge.Server.AdvertisedAsPaired); // sf=1
            Assert.True(string.IsNullOrEmpty(bridge.Persisted.Last().Ltsk));

            var fresh = await bridge.ConnectAsync();
            Assert.True((await fresh.PairSetupAsync(bridge.NewController("controller-new"), LoopbackHapHost.PairCode)).Success);
        }

        [Fact]
        public async Task ClearPairings_ClosesAllConnections_AndUnpairs()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var verified = await bridge.VerifiedConnectionAsync(admin);
            var plain = await bridge.ConnectAsync();
            Assert.Equal(470, (await plain.SendAsync("GET", "accessories")).StatusCode);

            bridge.Server.ClearPairings();

            Assert.True(await verified.WaitForCloseAsync());
            Assert.True(await plain.WaitForCloseAsync());
            Assert.False(bridge.Server.IsPaired);
            Assert.False(bridge.Server.AdvertisedAsPaired);
        }

        [Fact]
        public async Task TwoControllers_HaveIndependentSessions()
        {
            await using var bridge = new Bridge();
            var admin = await bridge.PairAsync("controller-A");
            var adminConnection = await bridge.VerifiedConnectionAsync(admin);
            var second = bridge.NewController("controller-B");
            second.AccessoryLtpk = admin.AccessoryLtpk;
            await adminConnection.AddPairingAsync(second.Id, second.PublicKey, admin: true);
            var secondConnection = await bridge.VerifiedConnectionAsync(second);

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(200, (await adminConnection.SendAsync("GET", "accessories")).StatusCode);
                Assert.Equal(200, (await secondConnection.SendAsync("GET", "accessories")).StatusCode);
            }
        }

        [Fact]
        public async Task ClosingAConnection_RemovesItFromTheServer()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.ConnectAsync();
            Assert.Equal(204, (await connection.SendAsync("POST", "identify")).StatusCode);
            Assert.True(await Eventually(() => bridge.Host.OpenConnectionCount == 1));

            connection.Dispose();

            Assert.True(await Eventually(() => bridge.Host.OpenConnectionCount == 0));
        }

        [Fact]
        public async Task UnknownEndpoint_ClosesTheConnection()
        {
            await using var bridge = new Bridge();
            var connection = await bridge.VerifiedConnectionAsync(await bridge.PairAsync());

            await connection.WriteRequestAsync(HapTestConnection.BuildRequest("GET", "does-not-exist", null, null));

            Assert.True(await connection.WaitForCloseAsync());
        }
    }
}
