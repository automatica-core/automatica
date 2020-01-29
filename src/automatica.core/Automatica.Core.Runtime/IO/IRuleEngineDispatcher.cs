using System;

namespace Automatica.Core.Runtime.IO
{
    public interface IRuleEngineDispatcher : IDisposable
    {
        bool Load();
    }
}
