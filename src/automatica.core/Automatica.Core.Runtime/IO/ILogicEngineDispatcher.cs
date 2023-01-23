using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.IO
{
    public interface ILogicEngineDispatcher : IDisposable
    {
        bool Load();
        Task Unlink(Guid linkId);
    }
}
