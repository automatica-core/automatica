using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Push.Concurrency
{
    internal class HubSemaphore : IHubSemaphore
    {
        private readonly SemaphoreSlim _semaphore;

        public HubSemaphore()
        {
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public int Release()
        {
            return _semaphore.Release();
        }

        public void Wait()
        {
            _semaphore.Wait();
        }

        public bool Wait(TimeSpan timeout)
        {
            return _semaphore.Wait(timeout);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return _semaphore.Wait(millisecondsTimeout);
        }

        public Task WaitAsync()
        {
            return _semaphore.WaitAsync();
        }

        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            return _semaphore.WaitAsync(timeout);
        }

        public Task<bool> WaitAsync(int millisecondsTimeout)
        {
            return _semaphore.WaitAsync(millisecondsTimeout);
        }
    }
}
