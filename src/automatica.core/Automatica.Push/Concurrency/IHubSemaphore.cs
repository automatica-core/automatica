using System;
using System.Threading.Tasks;

namespace Automatica.Push.Concurrency
{
    public interface IHubSemaphore
    {
        int Release();

        void Wait();
        bool Wait(TimeSpan timeout);
        bool Wait(int millisecondsTimeout);
        Task WaitAsync();
        Task<bool> WaitAsync(TimeSpan timeout);
        Task<bool> WaitAsync(int millisecondsTimeout);
    }
}
