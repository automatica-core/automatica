using System.Collections.Generic;

namespace Automatica.Push.Concurrency
{
    internal class HubSemaphoreFactory : IHubSemaphoreFactory
    {
        private readonly object _lock = new object();

        private readonly IDictionary<string, IHubSemaphore> _semaphoreDictionary = new Dictionary<string, IHubSemaphore>();

        public HubSemaphoreFactory()
        {
            
        }

        public IHubSemaphore GetSemaphore(string name)
        {
            lock (_lock)
            {
                if (!_semaphoreDictionary.ContainsKey(name))
                {
                    _semaphoreDictionary.Add(name, new HubSemaphore());
                }

                return _semaphoreDictionary[name];
            }
            
        }
    }
}
