using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Bonjour;
using P3.Driver.HomeKit.Bonjour.Abstraction;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.UnitTests.Integration;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Bonjour
{
    /// <summary>
    /// The "sf" TXT record tells controllers whether the bridge can be paired (1) or is paired (0).
    /// </summary>
    public class BonjourStatusFlagTests
    {
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";

        private static BonjourService Service() => new BonjourService(NullLogger.Instance, 51826, "Automatica", LoopbackHapHost.DeviceId, 3);

        private static Message Response(BonjourService service)
        {
            var method = typeof(BonjourService).GetMethod("GenerateQueryResponseMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Message)method!.Invoke(service, null);
        }

        private static string StatusFlag(BonjourService service)
        {
            return (string)typeof(BonjourService).GetProperty("StatusFlag", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service);
        }

        private static TXTRecord Txt(BonjourService service) => Response(service).Answers.OfType<TXTRecord>().Single();

        [Fact]
        public void NewService_IsAdvertisedAsPairable()
        {
            var service = Service();

            Assert.False(service.AlreadyPaired);
            Assert.Equal("1", StatusFlag(service));
            Assert.Contains("sf=1", Txt(service).Strings);
        }

        [Fact]
        public void AlreadyPaired_SwitchesTheTxtRecordTo0_AndBack()
        {
            var service = Service();

            service.AlreadyPaired = true;
            Assert.Contains("sf=0", Txt(service).Strings);
            Assert.DoesNotContain("sf=1", Txt(service).Strings);

            service.AlreadyPaired = false;
            Assert.Contains("sf=1", Txt(service).Strings);
        }

        [Fact]
        public void TxtRecord_ContainsTheHapKeys()
        {
            var txt = Txt(Service());

            Assert.Contains($"id={LoopbackHapHost.DeviceId}", txt.Strings);
            Assert.Contains("c#=3", txt.Strings);
            Assert.Contains("s#=1", txt.Strings);
            Assert.Contains("md=Automatica", txt.Strings);
            Assert.Contains("ci=2", txt.Strings);
            Assert.Contains("pv=1.1", txt.Strings);
            Assert.Contains("ff=0", txt.Strings);
            Assert.Equal("Automatica._hap._tcp.local", txt.Name.ToString());
        }

        [Fact]
        public void SrvRecord_PointsToTheHapPort()
        {
            var srv = Response(Service()).Answers.OfType<SRVRecord>().Single();

            Assert.Equal(51826, srv.Port);
            Assert.Equal("FC_22_3D_E3_CE_F2.local", srv.Target.ToString());
        }

        [Fact]
        public void Server_LoadedUnpaired_IsAdvertisedAsPairable()
        {
            Assert.False(LoopbackHapHost.CreateServer().AdvertisedAsPaired);
        }

        [Fact]
        public void Server_LoadedPaired_IsAdvertisedAsPaired()
        {
            var store = new HapPairingStore(null, null);
            store.CompletePairSetup(Ltsk, "admin", new string('a', 64));

            Assert.True(LoopbackHapHost.CreateServer(Ltsk, store.Serialize()).AdvertisedAsPaired);
        }

        [Fact]
        public void Server_FollowsThePairingStore()
        {
            var server = LoopbackHapHost.CreateServer();

            server.PairingStore.CompletePairSetup(Ltsk, "admin", new string('a', 64));
            Assert.True(server.AdvertisedAsPaired);

            server.ClearPairings();
            Assert.False(server.AdvertisedAsPaired);
        }

        [Fact]
        public void Server_FailedSetupAttempts_KeepItPairable()
        {
            var server = LoopbackHapHost.CreateServer();

            server.PairingStore.RegisterFailedSetupAttempt();

            Assert.False(server.AdvertisedAsPaired);
        }

        [Fact]
        public void Server_RemovingTheLastAdmin_MakesItPairableAgain()
        {
            var store = new HapPairingStore(null, null);
            store.CompletePairSetup(Ltsk, "admin", new string('a', 64));
            var server = LoopbackHapHost.CreateServer(Ltsk, store.Serialize());

            server.PairingStore.RemovePairing("admin");

            Assert.False(server.AdvertisedAsPaired);
            Assert.False(server.IsPaired);
        }
    }
}
