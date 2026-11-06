using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Token returned by <see cref="IDispatcher.Subscribe"/>, disposing it removes the registration (only once).
    /// </summary>
    public sealed class DispatchSubscriptionToken : IDisposable
    {
        private Func<Task> _unregister;

        public DispatchSubscriptionToken(Func<Task> unregister)
        {
            _unregister = unregister;
        }

        public void Dispose()
        {
            var unregister = Interlocked.Exchange(ref _unregister, null);
            unregister?.Invoke();
        }
    }
}
