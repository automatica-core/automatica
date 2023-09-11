using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public interface ILogicEngineDispatcher : IDisposable
    {
        Task<bool> Load();
        Task<bool> Reload();
        Task Unlink(Guid id);
        Task Unload();
    }
}
