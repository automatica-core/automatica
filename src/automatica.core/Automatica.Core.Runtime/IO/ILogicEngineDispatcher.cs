using System;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.IO
{
    public interface ILogicEngineDispatcher : IDisposable
    {
        Task<bool> Load();
        Task Unlink(Guid id);
        Task Unload();
    }
}
