using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using CoreDispatcher = Automatica.Core.Base.IO.Dispatcher;

namespace Automatica.Core.Tests.Dispatcher
{
    /// <summary>
    /// The value pipeline of the real dispatcher: value store, remanent values, remote clients, broadcast and rule notifications
    /// </summary>
    public class DispatcherValuePipelineTests
    {
        private readonly Mock<IDataBroadcast> _broadcast = new();
        private readonly Mock<IRemoteSender> _remote = new();
        private readonly Mock<IRemanentHandler> _remanent = new();
        private readonly Mock<IRuleInstanceVisuNotify> _ruleNotify = new();
        private readonly IDispatcher _dispatcher;

        public DispatcherValuePipelineTests()
        {
            _remanent.Setup(a => a.SaveValueAsync(It.IsAny<Guid>(), It.IsAny<DispatchValue>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _dispatcher = new CoreDispatcher(NullLogger<CoreDispatcher>.Instance, _broadcast.Object, _remote.Object, _remanent.Object, _ruleNotify.Object);
        }

        private sealed class TestSource : IDispatchable
        {
            public DispatchableSource Source { get; init; } = DispatchableSource.NodeInstance;
            public DispatchableType Type { get; init; } = DispatchableType.NodeInstance;
            public string Name => "source";
            public Guid Id { get; init; } = Guid.NewGuid();
            public bool IsRemanent { get; init; }
        }

        private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var start = Environment.TickCount64;
            while (!condition())
            {
                if (Environment.TickCount64 - start > timeoutMs)
                {
                    throw new TimeoutException("condition not reached");
                }
                await Task.Delay(10);
            }
        }

        private static readonly DateTime Fixed = new(2026, 5, 6, 7, 8, 9);

        [Fact]
        public async Task DispatchedValue_IsStoredPerTypeAndId()
        {
            var node = new TestSource();
            var rule = new TestSource { Type = DispatchableType.RuleInstance, Source = DispatchableSource.RuleInstance };

            await _dispatcher.DispatchValue(node, 1);
            await _dispatcher.DispatchValue(rule, "r");

            Assert.Equal(1, _dispatcher.GetValue(DispatchableType.NodeInstance, node.Id).Value);
            Assert.Null(_dispatcher.GetValue(DispatchableType.RuleInstance, node.Id));
            Assert.Equal("r", _dispatcher.GetValue(rule.Id).Value);
            Assert.True(_dispatcher.GetValues(DispatchableType.NodeInstance).ContainsKey(node.Id));
            Assert.Empty(_dispatcher.GetValues(DispatchableType.Visualization));
        }

        [Fact]
        public async Task LatestValue_WinsAcrossTypes()
        {
            var id = Guid.NewGuid();
            await _dispatcher.DispatchValue(new TestSource { Id = id }, 1);
            await _dispatcher.DispatchValue(new TestSource { Id = id, Type = DispatchableType.Visualization, Source = DispatchableSource.Visualization }, 2);

            Assert.Equal(2, _dispatcher.GetValue(id).Value);
            Assert.Equal(1, _dispatcher.GetValue(DispatchableType.NodeInstance, id).Value);
        }

        [Fact]
        public async Task Init_RestoresRemanentValues_WithoutDispatchingThem()
        {
            var id = Guid.NewGuid();
            _remanent.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, DispatchValue>
            {
                { id, new DispatchValue(id, DispatchableType.NodeInstance, 21.5, Fixed, DispatchValueSource.Read) },
                { Guid.NewGuid(), null }
            });
            var delivered = 0;
            await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, (_, _) =>
            {
                Interlocked.Increment(ref delivered);
                return Task.CompletedTask;
            });

            await _dispatcher.Init();
            await Task.Delay(50);

            Assert.Equal(21.5, _dispatcher.GetValue(id).Value);
            Assert.Equal(0, Volatile.Read(ref delivered));
            _remote.Verify(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>()), Times.Never);
            _remanent.Verify(a => a.SaveValueAsync(It.IsAny<Guid>(), It.IsAny<DispatchValue>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Init_NeverOverwritesALiveValue()
        {
            var node = new TestSource();
            await _dispatcher.DispatchValue(node, "live");
            _remanent.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, DispatchValue>
            {
                { node.Id, new DispatchValue(node.Id, DispatchableType.NodeInstance, "stale", Fixed, DispatchValueSource.Read) }
            });

            await _dispatcher.Init();

            Assert.Equal("live", _dispatcher.GetValue(node.Id).Value);
        }

        [Fact]
        public async Task Init_WithoutRemanentValues_DoesNothing()
        {
            _remanent.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IDictionary<Guid, DispatchValue>)null);

            await _dispatcher.Init();

            Assert.Empty(_dispatcher.GetValues());
        }

        [Fact]
        public async Task RemanentSource_IsSaved_OtherSourcesAreNot()
        {
            var remanent = new TestSource { IsRemanent = true };
            var plain = new TestSource();

            await _dispatcher.DispatchValue(remanent, 5);
            await _dispatcher.DispatchValue(plain, 6);

            _remanent.Verify(a => a.SaveValueAsync(remanent.Id, It.Is<DispatchValue>(v => Equals(v.Value, 5)), It.IsAny<CancellationToken>()), Times.Once);
            _remanent.Verify(a => a.SaveValueAsync(plain.Id, It.IsAny<DispatchValue>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LocalValues_AreSentToTheSatellites_VisualizationAndRemoteValuesAreNot()
        {
            var local = new TestSource();
            var visu = new TestSource { Type = DispatchableType.Visualization, Source = DispatchableSource.Visualization };
            var remote = new TestSource { Source = DispatchableSource.Remote };

            await _dispatcher.DispatchValue(local, 1);
            await _dispatcher.DispatchValue(visu, 2);
            await _dispatcher.DispatchValue(remote, 3);

            _remote.Verify(a => a.DispatchValue(It.Is<IDispatchable>(d => d.Id == local.Id), It.IsAny<object>()), Times.Once);
            _remote.Verify(a => a.DispatchValue(It.Is<IDispatchable>(d => d.Id == visu.Id), It.IsAny<object>()), Times.Never);
            _remote.Verify(a => a.DispatchValue(It.Is<IDispatchable>(d => d.Id == remote.Id), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task EveryValue_IsBroadcast()
        {
            var local = new TestSource();
            var remote = new TestSource { Source = DispatchableSource.Remote };

            await _dispatcher.DispatchValue(local, 1);
            await _dispatcher.DispatchValue(remote, 2);

            _broadcast.Verify(a => a.DispatchValue(DispatchableType.NodeInstance, local.Id, It.Is<object>(v => v is DispatchValue && Equals(((DispatchValue)v).Value, 1))), Times.Once);
            _broadcast.Verify(a => a.DispatchValue(DispatchableType.NodeInstance, remote.Id, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task FailingRemoteSenderOrBroadcast_DoesNotStopTheSubscribers()
        {
            _remote.Setup(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>())).ThrowsAsync(new InvalidOperationException("satellite down"));
            _broadcast.Setup(a => a.DispatchValue(It.IsAny<DispatchableType>(), It.IsAny<Guid>(), It.IsAny<object>())).ThrowsAsync(new InvalidOperationException("hub down"));
            var node = new TestSource { IsRemanent = true };
            _remanent.Setup(a => a.SaveValueAsync(It.IsAny<Guid>(), It.IsAny<DispatchValue>(), It.IsAny<CancellationToken>())).ThrowsAsync(new System.IO.IOException("disk full"));
            var delivered = 0;
            await _dispatcher.Subscribe(DispatchableType.NodeInstance, node.Id, (_, _) =>
            {
                Interlocked.Increment(ref delivered);
                return Task.CompletedTask;
            });

            await _dispatcher.DispatchValue(node, 1);

            await WaitUntil(() => Volatile.Read(ref delivered) == 1);
            Assert.Equal(1, _dispatcher.GetValue(node.Id).Value);
        }

        [Fact]
        public async Task RuleValues_NotifyTheRuleVisualization()
        {
            var rule = new TestSource { Type = DispatchableType.RuleInstance, Source = DispatchableSource.RuleInstance };

            await _dispatcher.DispatchValue(rule, true);

            _ruleNotify.Verify(a => a.NotifyValueChanged(It.Is<IDispatchable>(d => d.Id == rule.Id), It.Is<DispatchValue>(v => Equals(v.Value, true))), Times.Once);
        }

        [Fact]
        public async Task VisualizationValue_WithoutAVisualizationSubscription_ReachesSubscriptionsOfOtherTypes()
        {
            var id = Guid.NewGuid();
            object received = null;
            await _dispatcher.Subscribe(DispatchableType.NodeInstance, id, (_, v) =>
            {
                received = v.Value;
                return Task.CompletedTask;
            });

            await _dispatcher.DispatchValue(new TestSource { Id = id, Type = DispatchableType.Visualization, Source = DispatchableSource.Visualization }, "from visu");

            await WaitUntil(() => received != null);
            Assert.Equal("from visu", received);
        }

        [Fact]
        public async Task ValueSource_IsPassedToTheSubscribers()
        {
            var node = new TestSource();
            var sources = new System.Collections.Concurrent.ConcurrentQueue<DispatchValueSource>();
            await _dispatcher.Subscribe(DispatchableType.NodeInstance, node.Id, (_, v) =>
            {
                sources.Enqueue(v.ValueSource);
                return Task.CompletedTask;
            });

            await _dispatcher.DispatchValue(node, 1);
            await _dispatcher.DispatchValue(node, 2, DispatchValueSource.Write);
            await _dispatcher.DispatchValue(node, 3, DispatchValueSource.User);

            await WaitUntil(() => sources.Count == 3);
            Assert.Equal(new[] { DispatchValueSource.Read, DispatchValueSource.Write, DispatchValueSource.User }, sources.ToArray());
        }

        [Fact]
        public async Task ClearRegistrations_StopsAllDeliveries()
        {
            var node = new TestSource();
            var delivered = 0;
            await _dispatcher.Subscribe(DispatchableType.NodeInstance, node.Id, (_, _) =>
            {
                Interlocked.Increment(ref delivered);
                return Task.CompletedTask;
            });

            await _dispatcher.ClearRegistrations();
            await _dispatcher.DispatchValue(node, 1);
            await Task.Delay(50);

            Assert.Equal(0, Volatile.Read(ref delivered));
            Assert.Equal(1, _dispatcher.GetValue(node.Id).Value);
        }

        [Fact]
        public async Task MqttDispatch_StoresTheRemoteValue_AsARemoteSource()
        {
            var id = Guid.NewGuid();
            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new RemoteDispatchValue
            {
                Value = new DispatchValue(id, DispatchableType.NodeInstance, 12L, Fixed, DispatchValueSource.Read)
            });

            _dispatcher.MqttDispatch($"dispatcher/NodeInstance/{id}", payload);

            await WaitUntil(() => _dispatcher.GetValue(DispatchableType.NodeInstance, id) != null);
            Assert.Equal(12L, _dispatcher.GetValue(id).Value);
            // a value from a satellite is not echoed back to the satellites
            _remote.Verify(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public void MqttDispatch_UnknownType_IsIgnored()
        {
            var id = Guid.NewGuid();

            _dispatcher.MqttDispatch($"dispatcher/NotAType/{id}", "{}");

            Assert.Null(_dispatcher.GetValue(id));
        }
    }
}
