using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.HomeKit.Hap;
using P3.Driver.HomeKit.Hap.Controllers;
using P3.Driver.HomeKit.Hap.TlvData;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Security
{
    public class PairingControllerTests
    {
        private const string Ltsk = "3ae86e1ed4720fe49137f19d301d06f0c49e3436770a87a08beeb413dda556ff83e6d70e7064effd0c6df6d19451b1ded53f7e2d0dd86e33d4aa1993f5e9cded";
        private static readonly byte[] KeyB = Enumerable.Repeat((byte)0xbb, 32).ToArray();
        private readonly TlvParser _parser = new TlvParser(NullLogger.Instance);
        private readonly PairingController _controller = new PairingController(NullLogger.Instance);

        private static HapPairingStore Store()
        {
            var store = new HapPairingStore(null, null);
            Assert.True(store.CompletePairSetup(Ltsk, "admin", new string('a', 64)));
            return store;
        }

        private static HapSession Admin(bool admin = true, bool verified = true)
        {
            return new HapSession { IsVerified = verified, IsAdmin = admin, ControllerId = "admin" };
        }

        private static Tlv Request(int method, string id = null, byte[] key = null, byte[] permissions = null, int state = 1)
        {
            var tlv = new Tlv();
            tlv.AddType(Constants.State, state);
            tlv.AddType(Constants.Method, method);
            if (id != null)
            {
                tlv.AddType(Constants.Identifier, Encoding.UTF8.GetBytes(id));
            }
            if (key != null)
            {
                tlv.AddType(Constants.PublicKey, key);
            }
            if (permissions != null)
            {
                tlv.AddType(Constants.Permissions, permissions);
            }
            return tlv;
        }

        private Tlv Post(Tlv request, HapSession session, HapPairingStore store)
        {
            var result = _controller.Post(request, session, store);
            Assert.Equal("application/pairing+tlv8", result.ContentType);
            return _parser.Parse(result.Data);
        }

        private static void AssertError(Tlv response, ErrorCodes error)
        {
            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal((int)error, response.GetTypeAsInt(Constants.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(3)]
        public void Post_WithAStateOtherThan1_IsUnknown(int state)
        {
            var store = Store();

            AssertError(Post(Request(PairingController.MethodAddPairing, "x", KeyB, new byte[] { 1 }, state), Admin(), store), ErrorCodes.Unknown);
            Assert.Single(store.Pairings);
        }

        [Fact]
        public void Post_WithoutSession_IsAuthentication()
        {
            var store = Store();

            AssertError(Post(Request(PairingController.MethodListPairings), null, store), ErrorCodes.Authentication);
        }

        [Fact]
        public void Post_FromAnUnverifiedAdminSession_IsAuthentication()
        {
            var store = Store();

            AssertError(Post(Request(PairingController.MethodAddPairing, "x", KeyB, new byte[] { 1 }), Admin(verified: false), store), ErrorCodes.Authentication);
            Assert.Single(store.Pairings);
        }

        [Theory]
        [InlineData(PairingController.MethodAddPairing)]
        [InlineData(PairingController.MethodRemovePairing)]
        [InlineData(PairingController.MethodListPairings)]
        public void Post_FromANonAdmin_IsAuthentication_ForEveryMethod(int method)
        {
            var store = Store();

            AssertError(Post(Request(method, "admin", KeyB, new byte[] { 1 }), Admin(admin: false), store), ErrorCodes.Authentication);
            Assert.NotNull(store.Find("admin"));
        }

        [Fact]
        public void Add_WithoutPermissions_IsUnknown()
        {
            var store = Store();

            AssertError(Post(Request(PairingController.MethodAddPairing, "x", KeyB), Admin(), store), ErrorCodes.Unknown);
            Assert.Null(store.Find("x"));
        }

        [Fact]
        public void Add_WithoutPublicKey_IsUnknown()
        {
            AssertError(Post(Request(PairingController.MethodAddPairing, "x", null, new byte[] { 1 }), Admin(), Store()), ErrorCodes.Unknown);
        }

        [Theory]
        [InlineData(31)]
        [InlineData(33)]
        public void Add_WithAPublicKeyThatIsNot32Bytes_IsUnknown(int length)
        {
            var store = Store();

            AssertError(Post(Request(PairingController.MethodAddPairing, "x", new byte[length], new byte[] { 1 }), Admin(), store), ErrorCodes.Unknown);
            Assert.Null(store.Find("x"));
        }

        [Fact]
        public void Add_WithTwoPermissionBytes_IsUnknown()
        {
            AssertError(Post(Request(PairingController.MethodAddPairing, "x", KeyB, new byte[] { 1, 0 }), Admin(), Store()), ErrorCodes.Unknown);
        }

        [Theory]
        [InlineData(0x00, false)]
        [InlineData(0x01, true)]
        [InlineData(0x02, false)]
        [InlineData(0x03, true)]
        public void Add_UsesBit0OfThePermissionsAsAdminFlag(byte permissions, bool admin)
        {
            var store = Store();

            var response = Post(Request(PairingController.MethodAddPairing, "x", KeyB, new[] { permissions }), Admin(), store);

            Assert.False(response.HasType(Constants.Error));
            Assert.Equal(2, response.GetTypeAsInt(Constants.State));
            Assert.Equal(admin, store.Find("x").IsAdmin);
            Assert.Equal(new string('b', 64), store.Find("x").PublicKey, ignoreCase: true);
        }

        [Fact]
        public void Add_WhenFull_IsMaxPeers()
        {
            var store = Store();
            for (var i = 1; i < HapPairingStore.MaxPairings; i++)
            {
                store.AddPairing($"c{i}", i.ToString("x64"), false);
            }

            AssertError(Post(Request(PairingController.MethodAddPairing, "x", KeyB, new byte[] { 0 }), Admin(), store), ErrorCodes.MaxPeers);
        }

        [Fact]
        public void Remove_WithoutIdentifier_IsUnknown_AndRemovesNothing()
        {
            var store = Store();

            var result = _controller.Post(Request(PairingController.MethodRemovePairing), Admin(), store);

            AssertError(_parser.Parse(result.Data), ErrorCodes.Unknown);
            Assert.Empty(result.RemovedControllers);
            Assert.True(store.IsPaired);
        }

        [Fact]
        public void Remove_ReportsTheRemovedControllers()
        {
            var store = Store();
            store.AddPairing("guest", new string('b', 64), false);

            var result = _controller.Post(Request(PairingController.MethodRemovePairing, "guest"), Admin(), store);

            Assert.Equal(new[] { "guest" }, result.RemovedControllers);
            Assert.False(_parser.Parse(result.Data).HasType(Constants.Error));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(6)]
        public void UnknownMethod_IsUnknown(int method)
        {
            AssertError(Post(Request(method), Admin(), Store()), ErrorCodes.Unknown);
        }

        [Fact]
        public void SerializeList_Empty_ContainsOnlyTheState()
        {
            Assert.Equal(new byte[] { (byte)Constants.State, 1, 2 }, PairingController.SerializeList(Array.Empty<HapPairing>()));
        }

        [Fact]
        public void SerializeList_SeparatesEntries_AndEncodesPermissions()
        {
            var list = new[]
            {
                new HapPairing { Identifier = "a", PublicKey = new string('a', 64), IsAdmin = true },
                new HapPairing { Identifier = "b", PublicKey = new string('b', 64), IsAdmin = false },
                new HapPairing { Identifier = null, PublicKey = new string('c', 64), IsAdmin = false }
            };

            var data = PairingController.SerializeList(list);

            // state, id a, ltpk, perms, sep, id b, ltpk, perms, sep, empty id, ltpk, perms
            var expected = new byte[] { 6, 1, 2 }
                .Concat(new byte[] { 1, 1, (byte)'a', 3, 32 }).Concat(Enumerable.Repeat((byte)0xaa, 32)).Concat(new byte[] { 11, 1, 1 })
                .Concat(new byte[] { 255, 0 })
                .Concat(new byte[] { 1, 1, (byte)'b', 3, 32 }).Concat(Enumerable.Repeat((byte)0xbb, 32)).Concat(new byte[] { 11, 1, 0 })
                .Concat(new byte[] { 255, 0 })
                .Concat(new byte[] { 1, 0, 3, 32 }).Concat(Enumerable.Repeat((byte)0xcc, 32)).Concat(new byte[] { 11, 1, 0 })
                .ToArray();
            Assert.Equal(expected, data);
        }
    }
}
