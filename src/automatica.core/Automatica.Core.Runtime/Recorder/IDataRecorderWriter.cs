using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Recorder
{
    public interface IDataRecorderWriter
    {
        Task Start();
        Task Stop();
        Task AddTrend(Guid nodeInstance);
        Task RemoveTrend(Guid nodeInstance);
        Task RemoveAll();
    }
}
