using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Common;
using Xunit;

namespace Automatica.Core.Tests.Dispatcher
{
    public class DispatcherSubscriptionTests
    {
        private class TestDispatchable : IDispatchable
        {
            public DispatchableSource Source => DispatchableSource.NodeInstance;
            public DispatchableType Type => DispatchableType.NodeInstance;
            public string Name => "Test";
            public Guid Id { get; } = Guid.NewGuid();
            public bool IsRemanent => false;
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

        [Fact]
        public async Task UnRegisterByCallback_KeepsOtherSubscribers()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var first = 0;
            var second = 0;
            Action<IDispatchable, DispatchValue> firstCallback = (_, _) => Interlocked.Increment(ref first);
            Action<IDispatchable, DispatchValue> secondCallback = (_, _) => Interlocked.Increment(ref second);

            await dispatcher.RegisterDispatch(DispatchableType.NodeInstance, source.Id, firstCallback);
            await dispatcher.RegisterDispatch(DispatchableType.NodeInstance, source.Id, secondCallback);

            await dispatcher.DispatchValue(source, 1, DispatchValueSource.Read);
            await WaitUntil(() => Volatile.Read(ref first) == 1 && Volatile.Read(ref second) == 1);

            await dispatcher.UnRegisterDispatch(DispatchableType.NodeInstance, source.Id, firstCallback);

            await dispatcher.DispatchValue(source, 2, DispatchValueSource.Read);
            await WaitUntil(() => Volatile.Read(ref second) == 2);
            await Task.Delay(100);

            Assert.Equal(1, Volatile.Read(ref first));
            Assert.Equal(2, Volatile.Read(ref second));
        }

        [Fact]
        public async Task SubscriptionToken_Dispose_OnlyRemovesThisSubscription()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var first = 0;
            var second = 0;

            var token = await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref first);
                return Task.CompletedTask;
            });
            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref second);
                return Task.CompletedTask;
            });

            await dispatcher.DispatchValue(source, 1, DispatchValueSource.Read);
            await WaitUntil(() => Volatile.Read(ref first) == 1 && Volatile.Read(ref second) == 1);

            token.Dispose();
            token.Dispose(); // second dispose is a no-op

            await dispatcher.DispatchValue(source, 2, DispatchValueSource.Read);
            await WaitUntil(() => Volatile.Read(ref second) == 2);
            await Task.Delay(100);

            Assert.Equal(1, Volatile.Read(ref first));
        }

        [Fact]
        public async Task ThrowingCallback_DoesNotBlockOtherSubscribers()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var throwingCalls = 0;
            var received = 0;

            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref throwingCalls);
                throw new InvalidOperationException("test");
            });
            await dispatcher.RegisterDispatch(DispatchableType.NodeInstance, source.Id, (_, _) => throw new InvalidOperationException("test"));
            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref received);
                return Task.CompletedTask;
            });

            await dispatcher.DispatchValue(source, 1, DispatchValueSource.Read);
            await dispatcher.DispatchValue(source, 2, DispatchValueSource.Read);

            await WaitUntil(() => Volatile.Read(ref received) == 2 && Volatile.Read(ref throwingCalls) == 2);
        }

        [Fact]
        public async Task Subscription_ReceivesValuesInDispatchOrder()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();
            const int count = 200;

            var values = new ConcurrentQueue<int>();
            var random = new Random(42);

            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, async (_, value) =>
            {
                if (random.Next(10) == 0)
                {
                    await Task.Delay(1);
                }
                values.Enqueue((int)value.Value);
            });

            for (var i = 0; i < count; i++)
            {
                await dispatcher.DispatchValue(source, i, DispatchValueSource.Read);
            }

            await WaitUntil(() => values.Count == count);
            Assert.Equal(Enumerable.Range(0, count), values.ToArray());
        }

        [Fact]
        public async Task SlowSubscriber_DoesNotDelayOtherSubscribers()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var fast = 0;

            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) => gate.Task);
            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref fast);
                return Task.CompletedTask;
            });

            await dispatcher.DispatchValue(source, 1, DispatchValueSource.Read);
            await dispatcher.DispatchValue(source, 2, DispatchValueSource.Read);

            await WaitUntil(() => Volatile.Read(ref fast) == 2);
            gate.SetResult();
        }

        [Fact]
        public async Task DispatchLoop_IsStopped()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var calls = 0;
            // a value that dispatches itself again is an endless loop
            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, async (self, value) =>
            {
                Interlocked.Increment(ref calls);
                await dispatcher.DispatchValue(source, (int)value.Value + 1, DispatchValueSource.Read);
            });

            await dispatcher.DispatchValue(source, 0, DispatchValueSource.Read);

            await WaitUntil(() => Volatile.Read(ref calls) >= 10);
            await Task.Delay(500);
            var callsAfterLoop = Volatile.Read(ref calls);
            await Task.Delay(300);

            Assert.Equal(callsAfterLoop, Volatile.Read(ref calls));
            Assert.InRange(callsAfterLoop, 10, 100);
        }

        [Fact]
        public async Task UnRegisterAll_RemovesAllSubscriptionsOfTheId()
        {
            var dispatcher = new DispatcherMock();
            var source = new TestDispatchable();

            var calls = 0;
            await dispatcher.RegisterDispatch(DispatchableType.NodeInstance, source.Id, (_, _) => Interlocked.Increment(ref calls));
            await dispatcher.Subscribe(DispatchableType.NodeInstance, source.Id, (_, _) =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            });

            await dispatcher.UnRegisterDispatch(DispatchableType.NodeInstance, source.Id);
            await dispatcher.DispatchValue(source, 1, DispatchValueSource.Read);
            await Task.Delay(200);

            Assert.Equal(0, Volatile.Read(ref calls));
        }
    }
}
