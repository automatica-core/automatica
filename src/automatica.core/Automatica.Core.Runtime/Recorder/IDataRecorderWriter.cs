using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Recorder
{
    internal interface IDataRecorderWriter
    {
        Task Start();
        Task Stop();
        Task AddTrend(Guid nodeInstance);
        Task RemoveTrend(Guid nodeInstance);
        Task RemoveAll();
    }
}
