using System.Threading.Tasks;
using Automatica.Core.Base.IO;

namespace Automatica.Core.Base.Remote
{
    public interface IRemoteSender
    {
        Task DispatchValue(IDispatchable dispatchable, object value);
    }
}
