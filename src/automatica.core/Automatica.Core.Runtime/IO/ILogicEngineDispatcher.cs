using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public interface ILogicEngineDispatcher : IDisposable
    {
        Task<bool> Load();
        Task Unlink(Guid linkId);
    }
}
