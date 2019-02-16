using Automatica.Core.Base.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Automatica.Core.Tests
{
    public class AsyncExtensionTests
    {
        [Fact]
        public async Task TestWithCancellation1()
        {
            var cancel = new CancellationTokenSource();
            var t = new Task<bool>(() =>
            {
                Thread.Sleep(10 * 1000);
                return false;
            });

#pragma warning disable 4014
            Task.Run(() =>
#pragma warning restore 4014
             {
                 Thread.Sleep(1000);
                 cancel.Cancel();
             }, cancel.Token);

            try
            {
                await t.WithCancellation(cancel.Token);
            }
            catch (OperationCanceledException)
            {
                Assert.True(true);
            }

        }

        [Fact]
        public async Task TestWithCancellation2()
        {
            var cancel = new CancellationTokenSource();

            try
            {
                await AsyncTest1().WithCancellation(cancel.Token);
            }
            finally
            {
                Assert.True(true);
            }

        }

        private async Task<bool> AsyncTest1()
        {
            await Task.Delay(100);
            return false;
        }
    }
}
