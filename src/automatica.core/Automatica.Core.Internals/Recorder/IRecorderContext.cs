using System;
using System.Threading.Tasks;

namespace Automatica.Core.Internals.Recorder
{
    public interface IRecorderContext
    {
        Task AddRecording(Guid nodeInstanceId);
        Task RemoveRecording(Guid nodeInstanceId);
        Task Reload();
    }
}
