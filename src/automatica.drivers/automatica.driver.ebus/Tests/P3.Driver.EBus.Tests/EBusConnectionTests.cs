using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static P3.Driver.EBus.Tests.FakeBus;

namespace P3.Driver.EBus.Tests
{
    public class EBusConnectionTests : IAsyncLifetime
    {
        private readonly FakeBus _bus = new();
        private readonly ConcurrentQueue<EBusTelegram> _seen = new();
        private EBusConnection _connection;

        public async Task InitializeAsync()
        {
            _bus.Registers[13] = 3225;
            _bus.Registers[0xAAAA] = 0xA9AA;
            _connection = new EBusConnection(_bus, 0xFF);
            _connection.TelegramReceived += t => _seen.Enqueue(t);
            await _connection.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private static async Task Eventually(Func<bool> condition, string what)
        {
            for (var i = 0; i < 200 && !condition(); i++)
            {
                await Task.Delay(20);
            }

            Assert.True(condition(), what);
        }

        [Fact]
        public async Task ReadsAWolfRegister()
        {
            var value = await _connection.ReadWolfRegisterAsync(0x08, 13);

            Assert.Equal((ushort)3225, value);
            var request = Assert.Single(_bus.Requests);
            Assert.Equal(Hex("ff 08 50 22 03"), request.Take(5).ToArray());
        }

        [Fact]
        public async Task EscapedRegisterAndValue_AreHandled()
        {
            Assert.Equal((ushort)0xA9AA, await _connection.ReadWolfRegisterAsync(0x08, 0xAAAA));
        }

        [Fact]
        public async Task MissingRegister_IsNull()
        {
            Assert.Null(await _connection.ReadWolfRegisterAsync(0x08, 554));
        }

        [Fact]
        public async Task AbsentDevice_IsNoAnswer()
        {
            var result = await _connection.RequestAsync(0x15, 0x07, 0x04, Array.Empty<byte>());

            Assert.Equal(EBusResultStatus.NoAnswer, result.Status);
            await Assert.ThrowsAsync<EBusException>(() => _connection.ReadWolfRegisterAsync(0x15, 13));
        }

        [Fact]
        public async Task LostArbitration_IsRetried_AndTheWinnersTelegramIsSeen()
        {
            _bus.LoseArbitration = 2;

            var value = await _connection.ReadWolfRegisterAsync(0x08, 13);

            Assert.Equal((ushort)3225, value);
            await Eventually(() => _seen.Count(t => t.Source == 0x10 && t.Command == 0x0507) == 2, "the winning master's telegrams are published");
        }

        [Fact]
        public async Task AlwaysLosingArbitration_IsBusy()
        {
            _bus.LoseArbitration = int.MaxValue;

            var result = await _connection.RequestAsync(0x08, 0x50, 0x22, Wolf.WolfRegister.ReadRequest(13));

            Assert.Equal(EBusResultStatus.Busy, result.Status);
        }

        [Fact]
        public async Task OtherTelegrams_ArePublished()
        {
            _bus.Inject(Hex("03 fe 05 03 08 01 00 48 ff 44 1e 34 02 d5"));
            _bus.Inject(Hex("f1 50 50 22 03 cc 0e 00 8d 00 02 0b 02 01 00"));
            _bus.Inject(Hex("03 fe 05 03 08 01 00 48 ff 44 1e 34 02 d6"));

            await Eventually(() => _seen.Count == 2, "both valid telegrams arrive");
            Assert.Equal(new ushort[] { 0x0503, 0x5022 }, _seen.Select(t => t.Command));
            Assert.Equal(Hex("0b 02"), _seen.Last().Response);
        }

        [Fact]
        public async Task ParallelRequests_AreSerialized()
        {
            _bus.Registers[14] = 504;
            _bus.Registers[22] = 2997;

            var results = await Task.WhenAll(
                _connection.ReadWolfRegisterAsync(0x08, 13),
                _connection.ReadWolfRegisterAsync(0x08, 14),
                _connection.ReadWolfRegisterAsync(0x08, 22));

            Assert.Equal(new ushort?[] { 3225, 504, 2997 }, results);
        }

        [Fact]
        public async Task ClosedAdapter_CompletesTheConnection_AndFailsRequests()
        {
            _bus.Close();

            await Assert.ThrowsAnyAsync<Exception>(() => _connection.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
            await Assert.ThrowsAsync<EBusException>(() => _connection.ReadWolfRegisterAsync(0x08, 13));
        }

        [Fact]
        public void OnlyMasterAddresses_CanBeUsed()
        {
            Assert.Throws<ArgumentException>(() => new EBusConnection(new FakeBus(), 0x08));
        }
    }
}
