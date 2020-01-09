namespace Automatica.Push.Concurrency
{
    public interface IHubSemaphoreFactory
    {
        IHubSemaphore GetSemaphore(string name);
    }
}
