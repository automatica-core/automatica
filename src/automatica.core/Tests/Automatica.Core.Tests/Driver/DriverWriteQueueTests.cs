using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Core.UnitTests.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.Driver
{
    public class DriverWriteQueueTests
    {
        private class WriteDriverMock : DriverBase
        {
            public ConcurrentQueue<int> Attempts { get; } = new();
            public ConcurrentQueue<int> Written { get; } = new();
            public Func<int, int, Task> OnWrite { get; set; } = (_, _) => Task.CompletedTask;

            private readonly ConcurrentDictionary<int, int> _attemptCount = new();

            public WriteDriverMock(IDriverContext driverContext) : base(driverContext)
            {
            }

            protected override async Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
            {
                var intValue = (int)value;
                Attempts.Enqueue(intValue);
                var attempt = _attemptCount.AddOrUpdate(intValue, 1, (_, c) => c + 1);
                await OnWrite(intValue, attempt);
                Written.Enqueue(intValue);
            }

            protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
            {
                return Task.FromResult(false);
            }

            public override IDriverNode CreateDriverNode(IDriverContext ctx)
            {
                return null;
            }
        }

        private static async Task<WriteDriverMock> CreateDriver()
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = "WriteQueue"
            };
            var driver = new WriteDriverMock(new DriverContextMock(node, new DriverFactoryMock(), new NodeTemplateFactoryMock(),
                new DispatcherMock(), new NullLoggerFactory(), DateTimeHelper.ProviderInstance));
            await driver.StartInternal();
            return driver;
        }

        private static Task Write(WriteDriverMock driver, int value)
        {
            return driver.WriteValue(driver, new DispatchValue(Guid.NewGuid(), DispatchableType.NodeInstance, value, DateTime.Now, DispatchValueSource.User));
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
        public async Task Writes_AreExecutedInOrder()
        {
            var driver = await CreateDriver();
            const int count = 50;

            for (var i = 0; i < count; i++)
            {
                await Write(driver, i);
            }

            await WaitUntil(() => driver.Written.Count == count);
            Assert.Equal(Enumerable.Range(0, count), driver.Written.ToArray());
            await driver.Stop();
        }

        [Fact]
        public async Task FailedWrite_IsRetried_WhenNoNewerValueIsPending()
        {
            var driver = await CreateDriver();
            driver.OnWrite = (_, attempt) => attempt < 3 ? throw new InvalidOperationException("offline") : Task.CompletedTask;

            await Write(driver, 1);

            await WaitUntil(() => driver.Written.Count == 1);
            Assert.Equal(new[] { 1, 1, 1 }, driver.Attempts.ToArray());
            await driver.Stop();
        }

        [Fact]
        public async Task FailedWrite_IsNotRetried_WhenNewerValueIsPending()
        {
            var driver = await CreateDriver();
            var firstFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            driver.OnWrite = (value, _) =>
            {
                if (value == 1)
                {
                    firstFailed.TrySetResult();
                    throw new InvalidOperationException("offline");
                }
                return Task.CompletedTask;
            };

            await Write(driver, 1);
            await firstFailed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Write(driver, 2);

            await WaitUntil(() => driver.Written.Contains(2));
            await Task.Delay(1500); // longer than the first retry backoff

            var attempts = driver.Attempts.ToArray();
            // the old value is never written after the newer one
            Assert.Equal(2, attempts.Last());
            Assert.Equal(new[] { 2 }, driver.Written.ToArray());
            await driver.Stop();
        }

        [Fact]
        public async Task FullQueue_DropsTheOldestWrites()
        {
            var driver = await CreateDriver();
            var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            driver.OnWrite = async (value, _) =>
            {
                if (value == 0)
                {
                    firstStarted.TrySetResult();
                    await gate.Task;
                }
            };

            await Write(driver, 0);
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            for (var i = 1; i <= 150; i++)
            {
                await Write(driver, i);
            }

            gate.SetResult();

            // 0 was in progress, the queue holds the newest 100 values
            var expected = new[] { 0 }.Concat(Enumerable.Range(51, 100)).ToArray();
            await WaitUntil(() => driver.Written.Count == expected.Length);
            Assert.Equal(expected, driver.Written.ToArray());
            await driver.Stop();
        }

        [Fact]
        public async Task Stop_DropsPendingWrites()
        {
            var driver = await CreateDriver();
            var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            driver.OnWrite = async (value, _) =>
            {
                if (value == 0)
                {
                    firstStarted.TrySetResult();
                    await Task.Delay(Timeout.Infinite, CancellationToken.None).WaitAsync(TimeSpan.FromMilliseconds(300));
                }
            };

            await Write(driver, 0);
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Write(driver, 1);
            await Write(driver, 2);

            await driver.Stop();
            await Task.Delay(600);

            Assert.DoesNotContain(1, driver.Attempts);
            Assert.DoesNotContain(2, driver.Attempts);
        }
    }
}
