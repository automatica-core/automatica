using System;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    public interface IDataBroadcast
    {
        Task DispatchValue(DispatchableType type, Guid id, object value);
    }
}
