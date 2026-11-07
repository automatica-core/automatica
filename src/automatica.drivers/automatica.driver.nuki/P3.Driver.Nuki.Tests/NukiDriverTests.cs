using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Newtonsoft.Json;
using P3.Driver.Nuki.Driver;
using Xunit;

namespace P3.Driver.Nuki.Tests
{
    /// <summary>
    /// NukiDriver against a fake bridge on 127.0.0.1: request URLs, hashed-token auth, response parsing and the poll timer lifecycle
    /// </summary>
    public class NukiDriverTests : DriverFactoryTestBase<NukiDriverFactory>, IAsyncLifetime
    {
        private const string Token = "test-token-1234";
        private const int LockId = 4711;
        private const int LongPoll = 3600; // the poll timer never fires during a test unless asked to

        private static readonly Guid SmartLockTemplate = new("58e1dddc-03d4-4e5a-8a4b-164e9d183cbe");
        private static readonly Guid StateTemplate = new("d4122ed5-f1c1-4ec0-baf0-2d1cbbe13c5d");
        private static readonly Guid StateNameTemplate = new("546a0769-5fbf-4054-bbdb-717ac184855e");
        private static readonly Guid BatteryCriticalTemplate = new("5195bf0b-eb1a-4cc0-ae3f-72e97be2e01e");
        private static readonly Guid BatteryChargeStateTemplate = new("26af095b-8eef-4299-a171-fdb962eb4c25");
        private static readonly Guid TimestampTemplate = new("14986357-750a-45ca-83ed-917413c19cd9");

        private static readonly FieldInfo TimerField = typeof(NukiDriver).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private FakeNukiBridge _bridge;

        public async Task InitializeAsync()
        {
            _bridge = await FakeNukiBridge.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _bridge.DisposeAsync();
        }

        private sealed class LockNodes
        {
            public NodeInstance State { get; init; }
            public NodeInstance StateName { get; init; }
            public NodeInstance BatteryCritical { get; init; }
            public NodeInstance BatteryChargeState { get; init; }
            public NodeInstance Timestamp { get; init; }
        }

        private NodeInstance CreateBridgeNode(bool? hashedToken = true, int poll = LongPoll, int? port = null)
        {
            var node = CreateNodeInstance(DriverFactory.DriverGuid);
            node.SetProperty("ip", "127.0.0.1");
            node.SetProperty("port", port ?? _bridge.Port);
            node.SetProperty("token", Token);
            node.SetProperty("poll", poll);
            if (hashedToken.HasValue)
            {
                node.SetProperty("hashed_token", hashedToken.Value);
            }
            return node;
        }

        private LockNodes AddLock(NodeInstance bridgeNode, int lockId = LockId)
        {
            var smartLock = CreateNodeInstance(SmartLockTemplate);
            smartLock.SetProperty("id", lockId);
            bridgeNode.InverseThis2ParentNodeInstanceNavigation.Add(smartLock);

            NodeInstance Add(Guid template)
            {
                var child = CreateNodeInstance(template);
                smartLock.InverseThis2ParentNodeInstanceNavigation.Add(child);
                return child;
            }

            return new LockNodes
            {
                State = Add(StateTemplate),
                StateName = Add(StateNameTemplate),
                BatteryCritical = Add(BatteryCriticalTemplate),
                BatteryChargeState = Add(BatteryChargeStateTemplate),
                Timestamp = Add(TimestampTemplate),
            };
        }

        private async Task<NukiDriver> CreateNukiDriverAsync(NodeInstance node)
        {
            var driver = (NukiDriver)await CreateDriver(node);
            await driver.Init();
            return driver;
        }

        private static string LockJson(int nukiId = LockId, int state = 1, string stateName = "locked", bool batteryCritical = false, int charge = 80,
            string timestamp = "2026-01-02T03:04:05+00:00")
        {
            return JsonConvert.SerializeObject(new object[]
            {
                new
                {
                    deviceType = 0,
                    nukiId,
                    name = "Front door",
                    firmwareVersion = "2.2.1",
                    lastKnownState = new { mode = 2, state, stateName, batteryCritical, batteryCharging = false, batteryChargeState = charge, timestamp }
                }
            });
        }

        private object DispatchedValue(NodeInstance node)
        {
            return Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;
        }

        private async Task<object> WaitForDispatchAsync(NodeInstance node)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                var value = DispatchedValue(node);
                if (value != null)
                {
                    return value;
                }
                await Task.Delay(10);
            }
            return DispatchedValue(node);
        }

        private static Timer GetTimer(NukiDriver driver) => (Timer)TimerField.GetValue(driver);

        private static bool IsDisposed(Timer timer)
        {
            // Change returns false (or throws on older runtimes) once the timer is disposed; on a live timer it pauses it
            try
            {
                return !timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        // ---------- request URL and authentication ----------

        [Fact]
        public async Task Start_HashedToken_RequestsListWithTsRnrAndHash()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode(hashedToken: true));

            await driver.Start();
            await driver.Stop();

            var request = Assert.Single(_bridge.Requests);
            Assert.Equal("GET", request.Method);
            Assert.Equal("/list", request.Path);
            Assert.Equal(new[] { "hash", "rnr", "ts" }, request.Query.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Matches("^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}Z$", request.Query["ts"]);
            Assert.InRange(int.Parse(request.Query["rnr"]), 0, 65535);
            Assert.Equal(NukiTokenAuthTests.Sha256Hex($"{request.Query["ts"]},{request.Query["rnr"]},{Token}"), request.Query["hash"]);
        }

        [Fact]
        public async Task Start_HashedToken_NeverSendsTheTokenItself()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode(hashedToken: true));

            await driver.Start();
            await driver.Stop();

            var request = Assert.Single(_bridge.Requests);
            Assert.DoesNotContain(Token, request.RawQuery);
            Assert.False(request.Query.ContainsKey("token"));
        }

        [Fact]
        public async Task Start_HashedTokenSettingNotChanged_DefaultsToHashed()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode(hashedToken: null));

            await driver.Start();
            await driver.Stop();

            var request = Assert.Single(_bridge.Requests);
            Assert.True(request.Query.ContainsKey("hash"));
            Assert.False(request.Query.ContainsKey("token"));
        }

        [Fact]
        public async Task Start_PlainToken_SendsTokenParameterOnly()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode(hashedToken: false));

            await driver.Start();
            await driver.Stop();

            var request = Assert.Single(_bridge.Requests);
            Assert.Equal("/list", request.Path);
            Assert.Equal(Token, request.Query["token"]);
            Assert.Equal(new[] { "token" }, request.Query.Keys);
        }

        [Fact]
        public async Task Read_RequestsListAgainWithAFreshRandomNumber()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await driver.Start();

            for (var i = 0; i < 5; i++)
            {
                await driver.Read();
            }
            await driver.Stop();

            Assert.Equal(6, _bridge.Requests.Count);
            Assert.All(_bridge.Requests, r => Assert.Equal("/list", r.Path));
            Assert.True(_bridge.Requests.Select(r => r.Query["rnr"]).Distinct().Count() > 1);
        }

        // ---------- response parsing ----------

        [Fact]
        public async Task Start_LockInResponse_DispatchesAllAttributes()
        {
            _bridge.RespondWith(200, LockJson(state: 3, stateName: "unlocked", batteryCritical: true, charge: 15));
            var node = CreateBridgeNode();
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            await driver.Start();
            await driver.Stop();

            Assert.Equal(3, await WaitForDispatchAsync(nodes.State));
            Assert.Equal("unlocked", await WaitForDispatchAsync(nodes.StateName));
            Assert.Equal(true, await WaitForDispatchAsync(nodes.BatteryCritical));
            Assert.Equal(15, await WaitForDispatchAsync(nodes.BatteryChargeState));
            var timestamp = Assert.IsType<DateTime>(await WaitForDispatchAsync(nodes.Timestamp));
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), timestamp.ToUniversalTime());
        }

        [Fact]
        public async Task Read_StateChanged_DispatchesNewState()
        {
            _bridge.RespondWith(200, LockJson(state: 1, stateName: "locked"));
            var node = CreateBridgeNode();
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);
            await driver.Start();
            Assert.Equal("locked", await WaitForDispatchAsync(nodes.StateName));

            _bridge.RespondWith(200, LockJson(state: 3, stateName: "unlocked"));
            await driver.Read();
            await driver.Stop();

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!Equals(DispatchedValue(nodes.StateName), "unlocked") && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }
            Assert.Equal("unlocked", DispatchedValue(nodes.StateName));
            Assert.Equal(3, DispatchedValue(nodes.State));
        }

        [Fact]
        public async Task Start_ResponseForAnotherLock_DispatchesNothing()
        {
            _bridge.RespondWith(200, LockJson(nukiId: 1));
            var node = CreateBridgeNode();
            var nodes = AddLock(node, lockId: 2);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(DispatchedValue(nodes.State));
            Assert.Null(DispatchedValue(nodes.StateName));
        }

        [Fact]
        public async Task Start_TwoLocks_EachGetsItsOwnState()
        {
            _bridge.RespondWith(200, "[" +
                JsonConvert.SerializeObject(new { nukiId = 1, lastKnownState = new { state = 1, stateName = "locked" } }) + "," +
                JsonConvert.SerializeObject(new { nukiId = 2, lastKnownState = new { state = 3, stateName = "unlocked" } }) + "]");
            var node = CreateBridgeNode();
            var first = AddLock(node, 1);
            var second = AddLock(node, 2);
            var driver = await CreateNukiDriverAsync(node);

            await driver.Start();
            await driver.Stop();

            Assert.Equal("locked", await WaitForDispatchAsync(first.StateName));
            Assert.Equal("unlocked", await WaitForDispatchAsync(second.StateName));
        }

        [Fact]
        public async Task Start_LockWithoutLastKnownState_StartsAndDispatchesNothing()
        {
            _bridge.RespondWith(200, JsonConvert.SerializeObject(new[] { new { deviceType = 0, nukiId = LockId, name = "Front door" } }));
            var node = CreateBridgeNode();
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(DispatchedValue(nodes.State));
            Assert.Null(DispatchedValue(nodes.Timestamp));
        }

        [Fact]
        public async Task Start_LockWithNullLastKnownState_StartsAndDispatchesNothing()
        {
            _bridge.RespondWith(200, $"[{{\"nukiId\":{LockId},\"lastKnownState\":null}}]");
            var node = CreateBridgeNode();
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());
            Assert.NotNull(GetTimer(driver));
            await driver.Stop();

            Assert.Null(DispatchedValue(nodes.StateName));
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("")]
        public async Task Start_EmptyOrNullList_StartsAndCreatesTheTimer(string body)
        {
            _bridge.RespondWith(200, body);
            var node = CreateBridgeNode();
            AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());

            Assert.NotNull(GetTimer(driver));
            await driver.Stop();
        }

        [Theory]
        [InlineData(401)]
        [InlineData(403)]
        [InlineData(500)]
        [InlineData(503)]
        public async Task Start_ErrorStatus_StartsWithoutDispatching(int status)
        {
            _bridge.RespondWith(status, LockJson());
            var node = CreateBridgeNode();
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(DispatchedValue(nodes.State));
        }

        [Fact]
        public async Task Start_UnauthorizedWithPlainToken_StartsWithoutDispatching()
        {
            _bridge.RespondWith(401, LockJson());
            var node = CreateBridgeNode(hashedToken: false);
            var nodes = AddLock(node);
            var driver = await CreateNukiDriverAsync(node);

            Assert.True(await driver.Start());
            await driver.Stop();

            Assert.Null(DispatchedValue(nodes.State));
        }

        // ---------- poll timer lifecycle ----------

        [Fact]
        public async Task Start_InvalidJson_ThrowsAndLeavesNoTimer()
        {
            _bridge.RespondWith(200, "{not json");
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());

            await Assert.ThrowsAnyAsync<JsonException>(() => driver.Start());

            Assert.Null(GetTimer(driver));
        }

        [Fact]
        public async Task Start_BridgeDropsTheConnection_ThrowsAndLeavesNoTimer()
        {
            _bridge.Handler = ctx =>
            {
                ctx.Abort();
                return Task.CompletedTask;
            };
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());

            await Assert.ThrowsAsync<HttpRequestException>(() => driver.Start());

            Assert.Null(GetTimer(driver));
        }

        [Fact]
        public async Task Start_RetriedAfterAFailure_CreatesExactlyOneTimer()
        {
            _bridge.RespondWith(200, "{not json");
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await Assert.ThrowsAnyAsync<JsonException>(() => driver.Start());

            _bridge.RespondWith(200, "[]");
            Assert.True(await driver.Start());

            var timer = GetTimer(driver);
            Assert.NotNull(timer);
            Assert.False(IsDisposed(timer));
            await driver.Stop();
        }

        [Fact]
        public async Task Start_CalledTwice_DisposesTheFirstTimer()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await driver.Start();
            var first = GetTimer(driver);

            await driver.Start();
            var second = GetTimer(driver);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
            Assert.True(IsDisposed(first));
            Assert.False(IsDisposed(second));
            await driver.Stop();
        }

        [Fact]
        public async Task Start_RetryThatFails_DisposesTheTimerOfThePreviousStart()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await driver.Start();
            var first = GetTimer(driver);

            _bridge.RespondWith(200, "{not json");
            await Assert.ThrowsAnyAsync<JsonException>(() => driver.Start());

            Assert.True(IsDisposed(first));
            Assert.Null(GetTimer(driver));
        }

        [Fact]
        public async Task Start_ShortPollInterval_PollsTheBridgeAgain()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode(poll: 1));

            await driver.Start();
            var polled = await _bridge.WaitForRequestsAsync(2, TimeSpan.FromSeconds(10));
            await driver.Stop();

            Assert.True(polled);
            Assert.All(_bridge.Requests, r => Assert.Equal("/list", r.Path));
        }

        [Fact]
        public async Task Stop_AfterStart_DisposesTheTimer()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await driver.Start();
            var timer = GetTimer(driver);

            Assert.True(await driver.Stop());

            Assert.True(IsDisposed(timer));
            Assert.Null(GetTimer(driver));
        }

        [Fact]
        public async Task Stop_NeverStarted_DoesNotThrow()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());

            Assert.True(await driver.Stop());
            Assert.Null(GetTimer(driver));
        }

        [Fact]
        public async Task Stop_AfterFailedStart_DoesNotThrow()
        {
            _bridge.RespondWith(200, "{not json");
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await Assert.ThrowsAnyAsync<JsonException>(() => driver.Start());

            Assert.True(await driver.Stop());
        }

        [Fact(Skip = "BUG: DriverBase.Stop in the consumed Automatica.Core.Driver 10.0.0.2049 package disposes its CancellationTokenSource, a second Stop throws ObjectDisposedException (fixed in source by aaea5026, package not republished)")]
        public async Task Stop_CalledTwice_DoesNotThrow()
        {
            var driver = await CreateNukiDriverAsync(CreateBridgeNode());
            await driver.Start();

            await driver.Stop();
            await driver.Stop();

            Assert.Null(GetTimer(driver));
        }

        // ---------- configuration ----------

        [Fact]
        public async Task Configure_SmartLockWithAttributes_CreatesAllAttributeNodes()
        {
            var node = CreateBridgeNode();
            AddLock(node);

            var driver = await CreateNukiDriverAsync(node);

            var smartLock = Assert.IsType<NukiSmartLock>(Assert.Single(driver.Children));
            Assert.Equal(LockId, smartLock.NukiId);
            Assert.Equal(5, smartLock.Children.Count);
            Assert.All(smartLock.Children, c => Assert.IsType<NukiSmartLockAttribute>(c));
        }
    }
}
