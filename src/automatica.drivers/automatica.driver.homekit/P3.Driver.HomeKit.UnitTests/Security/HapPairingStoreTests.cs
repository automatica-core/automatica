using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.TlvData;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Security
{
    public class HapPairingStoreTests
    {
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private static readonly string KeyA = new string('a', 64);
        private static readonly string KeyB = new string('b', 64);
        private static readonly string KeyC = new string('c', 64);

        private static HapPairingStore Paired(string id = "admin", string key = null)
        {
            var store = new HapPairingStore(null, null);
            Assert.True(store.CompletePairSetup(Ltsk, id, key ?? KeyA));
            return store;
        }

        private static List<PairSetupCompleteEventArgs> Record(HapPairingStore store)
        {
            var events = new List<PairSetupCompleteEventArgs>();
            store.Changed += (s, e) => events.Add(e);
            return events;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ctor_WithoutPairings_IsUnpaired(string pairings)
        {
            var store = new HapPairingStore(Ltsk, pairings);

            Assert.False(store.IsPaired);
            Assert.Empty(store.Pairings);
            Assert.Equal(0, store.FailedSetupAttempts);
            Assert.False(store.IsLockedOut);
        }

        [Fact]
        public void IsPaired_WithPairingsButNoAccessoryKey_IsFalse()
        {
            var json = "{\"pairings\":[{\"id\":\"a\",\"ltpk\":\"" + KeyA + "\",\"admin\":true}]}";

            Assert.False(new HapPairingStore("", json).IsPaired);
            Assert.True(new HapPairingStore(Ltsk, json).IsPaired);
        }

        [Fact]
        public void Load_Json_SkipsPairingsWithoutPublicKey()
        {
            var json = "{\"pairings\":[{\"id\":\"a\",\"ltpk\":\"\",\"admin\":true},{\"id\":\"b\",\"ltpk\":\"" + KeyB + "\",\"admin\":false}]}";

            var store = new HapPairingStore(Ltsk, json);

            var pairing = Assert.Single(store.Pairings);
            Assert.Equal("b", pairing.Identifier);
            Assert.False(pairing.IsAdmin);
        }

        [Fact]
        public void Load_Json_IsCappedAtMaxPairings()
        {
            var pairings = Enumerable.Range(0, HapPairingStore.MaxPairings + 4)
                .Select(i => new JObject { ["id"] = $"c{i}", ["ltpk"] = i.ToString("x64"), ["admin"] = true });
            var json = new JObject { ["pairings"] = new JArray(pairings) }.ToString();

            Assert.Equal(HapPairingStore.MaxPairings, new HapPairingStore(Ltsk, json).Pairings.Count);
        }

        [Fact]
        public void Load_NegativeFailedAttempts_IsClampedToZero()
        {
            Assert.Equal(0, new HapPairingStore(null, "{\"pairings\":[],\"failedSetupAttempts\":-5}").FailedSetupAttempts);
        }

        [Fact]
        public void Load_PersistedLockout_IsLockedOut()
        {
            var store = new HapPairingStore(null, $"{{\"failedSetupAttempts\":{HapPairingStore.MaxFailedSetupAttempts}}}");

            Assert.True(store.IsLockedOut);
            Assert.Empty(store.Pairings);
        }

        [Fact]
        public void Load_LegacyHexKey_IsAnAdminWithoutIdentifier()
        {
            var store = new HapPairingStore(Ltsk, "  " + KeyA + "  ");

            var pairing = Assert.Single(store.Pairings);
            Assert.Null(pairing.Identifier);
            Assert.Equal(KeyA, pairing.PublicKey);
            Assert.True(pairing.IsAdmin);
            Assert.True(store.IsPaired);
        }

        [Fact]
        public void Serialize_RoundTrip_KeepsPairingsPermissionsAndFailedAttempts()
        {
            var store = Paired("admin");
            store.AddPairing("guest", KeyB, false);
            store.RegisterFailedSetupAttempt();

            var reloaded = new HapPairingStore(Ltsk, store.Serialize());

            Assert.Equal(new[] { "admin", "guest" }, reloaded.Pairings.Select(a => a.Identifier));
            Assert.Equal(new[] { true, false }, reloaded.Pairings.Select(a => a.IsAdmin));
            Assert.Equal(1, reloaded.FailedSetupAttempts);
            Assert.Equal(2, (int)JObject.Parse(store.Serialize())["v"]);
        }

        [Fact]
        public void Pairings_AndFind_ReturnCopies()
        {
            var store = Paired("admin");

            store.Pairings[0].IsAdmin = false;
            store.Find("admin").PublicKey = KeyC;

            Assert.True(store.Find("admin").IsAdmin);
            Assert.Equal(KeyA, store.Find("admin").PublicKey);
        }

        [Fact]
        public void Find_Unknown_ReturnsNull_WhenThereIsNoLegacyPairing()
        {
            Assert.Null(Paired("admin").Find("other"));
        }

        [Fact]
        public void Find_PrefersTheExactIdentifierOverALegacyPairing()
        {
            var store = new HapPairingStore(Ltsk, KeyA);
            store.AddPairing("b", KeyB, false);

            Assert.Equal(KeyB, store.Find("b").PublicKey);
            Assert.Equal(KeyA, store.Find("unknown").PublicKey); // legacy candidate
        }

        [Fact]
        public void BindIdentifier_WithAnotherKey_DoesNotBind()
        {
            var store = new HapPairingStore(Ltsk, KeyA);
            var events = Record(store);

            store.BindIdentifier(KeyB, "c1");

            Assert.Null(store.Pairings.Single().Identifier);
            Assert.Empty(events);
        }

        [Fact]
        public void BindIdentifier_WhenTheIdentifierIsTaken_DoesNotBind()
        {
            var store = new HapPairingStore(Ltsk, KeyA);
            store.AddPairing("c1", KeyB, false);

            store.BindIdentifier(KeyA, "c1");

            Assert.Contains(store.Pairings, a => a.Identifier == null && a.PublicKey == KeyA);
        }

        [Fact]
        public void BindIdentifier_RaisesChanged_WithTheBoundIdentifier()
        {
            var store = new HapPairingStore(Ltsk, KeyA);
            var events = Record(store);

            store.BindIdentifier(KeyA, "c1");

            var e = Assert.Single(events);
            Assert.Equal(Ltsk, e.Ltsk);
            Assert.Equal("c1", new HapPairingStore(e.Ltsk, e.Ltpk).Pairings.Single().Identifier);
        }

        [Fact]
        public void RegisterFailedSetupAttempt_ReportsTheLockoutOnlyAtTheLimit()
        {
            var store = new HapPairingStore(null, $"{{\"failedSetupAttempts\":{HapPairingStore.MaxFailedSetupAttempts - 2}}}");

            Assert.False(store.RegisterFailedSetupAttempt());
            Assert.False(store.IsLockedOut);
            Assert.True(store.RegisterFailedSetupAttempt());
            Assert.True(store.IsLockedOut);
            Assert.True(store.RegisterFailedSetupAttempt());
        }

        [Fact]
        public void RegisterFailedSetupAttempt_PersistsTheCounter()
        {
            var store = new HapPairingStore(null, null);
            var events = Record(store);

            store.RegisterFailedSetupAttempt();
            store.RegisterFailedSetupAttempt();

            Assert.Equal(2, events.Count);
            Assert.Equal(2, new HapPairingStore(null, events.Last().Ltpk).FailedSetupAttempts);
        }

        [Fact]
        public void CompletePairSetup_WhenAlreadyPaired_IsRefused_AndKeepsTheExistingPairing()
        {
            var store = Paired("admin");
            var events = Record(store);

            Assert.False(store.CompletePairSetup(new string('1', 128), "intruder", KeyB));

            Assert.Equal("admin", store.Pairings.Single().Identifier);
            Assert.Equal(Ltsk, store.AccessoryLtsk);
            Assert.Empty(events);
        }

        [Fact]
        public void CompletePairSetup_ResetsTheFailedAttempts_AndStoresAnAdmin()
        {
            var store = new HapPairingStore(null, "{\"failedSetupAttempts\":42}");
            var events = Record(store);

            Assert.True(store.CompletePairSetup(Ltsk, "c1", KeyA));

            Assert.Equal(0, store.FailedSetupAttempts);
            Assert.True(store.Pairings.Single().IsAdmin);
            Assert.True(store.IsPaired);
            Assert.Equal(Ltsk, Assert.Single(events).Ltsk);
        }

        [Fact]
        public void CompletePairSetup_WithStalePairingsButNoAccessoryKey_ReplacesThem()
        {
            var store = new HapPairingStore("", "{\"pairings\":[{\"id\":\"old\",\"ltpk\":\"" + KeyA + "\",\"admin\":true}]}");

            Assert.True(store.CompletePairSetup(Ltsk, "new", KeyB));

            Assert.Equal("new", store.Pairings.Single().Identifier);
        }

        [Theory]
        [InlineData(null, "key")]
        [InlineData("", "key")]
        [InlineData("id", null)]
        [InlineData("id", "")]
        public void AddPairing_WithoutIdentifierOrKey_IsUnknown(string id, string key)
        {
            var store = Paired();

            Assert.Equal(ErrorCodes.Unknown, store.AddPairing(id, key == "key" ? KeyB : key, false));
            Assert.Single(store.Pairings);
        }

        [Fact]
        public void AddPairing_KnownIdentifierWithTheSameKeyInOtherCase_UpdatesThePermissions()
        {
            var store = Paired("admin");
            store.AddPairing("guest", KeyB, false);

            Assert.Null(store.AddPairing("guest", KeyB.ToUpperInvariant(), true));

            Assert.True(store.Find("guest").IsAdmin);
            Assert.Equal(2, store.Pairings.Count);
        }

        [Fact]
        public void AddPairing_KnownIdentifierWithAnotherKey_IsRefused_AndKeepsThePermissions()
        {
            var store = Paired("admin");
            store.AddPairing("guest", KeyB, false);
            var events = Record(store);

            Assert.Equal(ErrorCodes.Unknown, store.AddPairing("guest", KeyC, true));

            Assert.False(store.Find("guest").IsAdmin);
            Assert.Equal(KeyB, store.Find("guest").PublicKey);
            Assert.Empty(events);
        }

        [Fact]
        public void AddPairing_UpdatingAnExistingPairing_IsAllowedAtCapacity()
        {
            var store = Paired("admin");
            for (var i = 1; i < HapPairingStore.MaxPairings; i++)
            {
                Assert.Null(store.AddPairing($"c{i}", i.ToString("x64"), false));
            }

            Assert.Equal(ErrorCodes.MaxPeers, store.AddPairing("new", KeyC, false));
            Assert.Null(store.AddPairing("c1", 1.ToString("x64"), true));
            Assert.True(store.Find("c1").IsAdmin);
        }

        [Fact]
        public void AddPairing_RaisesChanged()
        {
            var store = Paired("admin");
            var events = Record(store);

            store.AddPairing("guest", KeyB, false);

            Assert.Contains("guest", Assert.Single(events).Ltpk);
        }

        [Fact]
        public void AddPairing_Concurrently_NeverExceedsTheCap()
        {
            var store = Paired("admin");

            Parallel.For(0, 64, i => store.AddPairing($"c{i}", (i + 1).ToString("x64"), false));

            Assert.Equal(HapPairingStore.MaxPairings, store.Pairings.Count);
            Assert.Equal(HapPairingStore.MaxPairings, store.Pairings.Select(a => a.Identifier).Distinct().Count());
        }

        [Fact]
        public void RemovePairing_Unknown_ReturnsNothing_AndDoesNotRaiseChanged()
        {
            var store = Paired("admin");
            var events = Record(store);

            Assert.Empty(store.RemovePairing("other"));
            Assert.Empty(events);
            Assert.True(store.IsPaired);
        }

        [Fact]
        public void RemovePairing_NonAdmin_KeepsTheOthersAndTheAccessoryKey()
        {
            var store = Paired("admin");
            store.AddPairing("guest", KeyB, false);

            Assert.Equal(new[] { "guest" }, store.RemovePairing("guest"));

            Assert.Equal("admin", store.Pairings.Single().Identifier);
            Assert.Equal(Ltsk, store.AccessoryLtsk);
        }

        [Fact]
        public void RemovePairing_AnAdminWhileAnotherAdminRemains_KeepsThePairing()
        {
            var store = Paired("admin");
            store.AddPairing("admin2", KeyB, true);
            store.AddPairing("guest", KeyC, false);

            Assert.Equal(new[] { "admin" }, store.RemovePairing("admin"));

            Assert.True(store.IsPaired);
            Assert.Equal(new[] { "admin2", "guest" }, store.Pairings.Select(a => a.Identifier));
        }

        [Fact]
        public void RemovePairing_TheLastAdmin_RemovesEveryPairing_AndReturnsAllRemovedIds()
        {
            var store = Paired("admin");
            store.AddPairing("guest1", KeyB, false);
            store.AddPairing("guest2", KeyC, false);
            var events = Record(store);

            var removed = store.RemovePairing("admin");

            Assert.Equal(new[] { "admin", "guest1", "guest2" }, removed);
            Assert.Empty(store.Pairings);
            Assert.False(store.IsPaired);
            Assert.Equal(string.Empty, store.AccessoryLtsk);
            Assert.Equal(string.Empty, Assert.Single(events).Ltsk);
        }

        [Fact]
        public void Clear_ResetsPairingsKeyAndLockout()
        {
            var store = new HapPairingStore(Ltsk, $"{{\"pairings\":[{{\"id\":\"a\",\"ltpk\":\"{KeyA}\",\"admin\":true}}],\"failedSetupAttempts\":{HapPairingStore.MaxFailedSetupAttempts}}}");
            var events = Record(store);

            store.Clear();

            Assert.False(store.IsPaired);
            Assert.False(store.IsLockedOut);
            Assert.Empty(store.Pairings);
            var e = Assert.Single(events);
            Assert.Equal(string.Empty, e.Ltsk);
            var reloaded = new HapPairingStore(e.Ltsk, e.Ltpk);
            Assert.False(reloaded.IsPaired);
            Assert.False(reloaded.IsLockedOut);
        }

        [Fact]
        public void Clear_AllowsANewPairSetup()
        {
            var store = Paired("admin");
            store.Clear();

            Assert.True(store.CompletePairSetup(Ltsk, "new", KeyB));
            Assert.Equal("new", store.Pairings.Single().Identifier);
        }
    }
}
