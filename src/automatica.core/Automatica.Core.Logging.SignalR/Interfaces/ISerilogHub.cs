namespace Automatica.Core.Logging.SignalR.Interfaces
{
    public interface ISerilogHub
    {
        Task PushEventLog(string name, string message);
    }
}
