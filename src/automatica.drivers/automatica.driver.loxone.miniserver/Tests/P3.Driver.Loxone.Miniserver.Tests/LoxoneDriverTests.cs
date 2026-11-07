using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.DriverFactory;
using Xunit;
using static P3.Driver.Loxone.Miniserver.Tests.LoxoneFrames;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    /// <summary>
    /// LoxoneDriver / LoxoneDriverNode against the loopback miniserver.
    /// </summary>
    public class LoxoneDriverTests : DriverFactoryTestBase<LoxoneDriverFactory>, IAsyncLifetime
    {
        private static readonly Guid ConnectedNodeTemplate = new Guid("3887cc34-6103-4dc7-aaec-ba30e6e42710");
        private const string ControlUuid = "1a2b3c4d-0001-0002-ffff000000000001";

        private FakeMiniserver _server;
        private LoxoneDriver _driver;

        public async Task InitializeAsync()
        {
            _server = await FakeMiniserver.StartAsync();
            // the switch control whose "active" state is UuidA
            _server.LoxAppJson = "{\"msInfo\":{},\"rooms\":{},\"cats\":{},\"controls\":{\"" + ControlUuid + "\":{\"name\":\"Light\",\"type\":\"Switch\",\"states\":{\"active\":\"" + UuidAString + "\"}}}}";
        }

        public async Task DisposeAsync()
        {
            if (_driver != null)
            {
                try
                {
                    await _driver.Stop();
                }
                catch
                {
                    // already stopped
                }
            }
            await _server.DisposeAsync();
        }

        private static void SetProperty(NodeInstance node, string key, object value)
        {
            node.PropertyInstance.Single(a => a.This2PropertyTemplateNavigation.Key == key).Value = value;
        }

        private async Task<(NodeInstance Connected, NodeInstance Switch)> StartDriver()
        {
            var driverNode = CreateNodeInstance(LoxoneDriverFactory.BusId);
            SetProperty(driverNode, "ip-address", "127.0.0.1");
            SetProperty(driverNode, "port", _server.Port);
            SetProperty(driverNode, "user", _server.User);
            SetProperty(driverNode, "password", _server.Password);
            SetProperty(driverNode, "loxone-use-https", false);
            SetProperty(driverNode, "loxone-use-tunnel", false);

            var connected = CreateNodeInstance(ConnectedNodeTemplate);
            var switchNode = CreateNodeInstance(LoxoneDriverFactory.SwitchNodeGuid);
            SetProperty(switchNode, "uuid", ControlUuid);
            SetProperty(switchNode, "state", "active");

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(connected);
            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(switchNode);

            var context = new DriverContextMock(driverNode, DriverFactory, Factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            _driver = (LoxoneDriver)DriverFactory.CreateDriver(context);

            Assert.True(await _driver.Init());
            Assert.True(await _driver.Configure());
            Assert.True(await _driver.Start());
            // the miniserver connection is private to the driver, read it to wait for the token authentication
            var connection = (LoxoneMiniserverConnection)typeof(LoxoneDriver)
                .GetField("_miniserver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(_driver);
            Assert.True(await FakeMiniserver.WaitUntil(() => connection.IsAuthenticated), "no token handshake: " + _server.Diagnostics());

            return (connected, switchNode);
        }

        private object CurrentValue(NodeInstance node)
        {
            return Dispatcher.GetValue(DispatchableType.NodeInstance, node.ObjId)?.Value;
        }

        private static DispatchValue Write(object value)
        {
            return new DispatchValue(DispatchableMock.Instance.Id, DispatchableType.NodeInstance, value, new DateTime(2026, 1, 1, 12, 0, 0), DispatchValueSource.Write);
        }

        [Fact]
        public async Task Start_ConnectedNode_IsTrue()
        {
            var (connected, _) = await StartDriver();

            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(connected), true)));
        }

        [Fact]
        public async Task WebSocketClosedByMiniserver_ConnectedNode_BecomesFalse()
        {
            var (connected, _) = await StartDriver();
            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(connected), true)));

            await _server.CloseWebSocketAsync();

            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(connected), false)), "the connected node still reports true after the websocket was closed");
        }

        [Fact]
        public async Task ValueStateFromMiniserver_IsDispatchedOnTheMatchingNode()
        {
            var (_, switchNode) = await StartDriver();

            await _server.SendBinaryAsync(ValueMessage((UuidA, 1), (UuidB, 5)));

            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(switchNode), 1d)));
        }

        [Fact]
        public async Task Write_Authenticated_IsSentOverTheWebSocketAndDispatched()
        {
            var (_, switchNode) = await StartDriver();
            var driverNode = _driver.Children.OfType<LoxoneDriverNode>().Single();

            await driverNode.WriteValue(DispatchableMock.Instance, Write(true));

            Assert.True(await FakeMiniserver.WaitUntil(() => _server.DecryptedCommands.Contains($"jdev/sps/io/{ControlUuid}/on")));
            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(switchNode), true)));
            Assert.DoesNotContain(_server.HttpRequests, a => a.RawTarget.StartsWith("/jdev/sps/io/"));
        }

        [Fact]
        public async Task Write_RejectedByMiniserver_IsNotDispatched()
        {
            var (connected, switchNode) = await StartDriver();
            var driverNode = _driver.Children.OfType<LoxoneDriverNode>().Single();
            var dispatched = new ConcurrentQueue<object>();
            await Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, switchNode.ObjId, (s, v) => dispatched.Enqueue(v.Value));
            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(connected), true)));

            // without the websocket the write goes over http, where the miniserver answers with an error code;
            // wait until the driver saw the close, before that a write would still go to the closing websocket
            await _server.CloseWebSocketAsync();
            Assert.True(await FakeMiniserver.WaitUntil(() => Equals(CurrentValue(connected), false)));
            _server.WriteResponseCode = 500;

            await driverNode.WriteValue(DispatchableMock.Instance, Write(12));
            Assert.True(await FakeMiniserver.WaitUntil(() => _server.HttpRequests.Any(a => a.RawTarget == $"/jdev/sps/io/{ControlUuid}/12")));

            // a later accepted write proves the rejected one was processed (the write queue is sequential)
            _server.WriteResponseCode = 200;
            await driverNode.WriteValue(DispatchableMock.Instance, Write(13));

            Assert.True(await FakeMiniserver.WaitUntil(() => dispatched.Contains(13)));
            Assert.DoesNotContain(12, dispatched);
        }
    }
}
