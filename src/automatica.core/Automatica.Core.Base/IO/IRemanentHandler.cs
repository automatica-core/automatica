using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    public interface IRemanentHandler
    {
        Task<IDictionary<Guid, DispatchValue>> GetAllAsync(CancellationToken token = default);
        Task<DispatchValue> GetLastValue(Guid nodeInstanceId, CancellationToken token = default);
        Task<bool> SaveValueAsync(Guid nodeInstanceId, DispatchValue value, CancellationToken token = default);
    }
}
