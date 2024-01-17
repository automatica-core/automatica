using Automatica.Core.EF.Models;
using System.Threading.Tasks;

namespace Automatica.Core.Base.IO
{
    public interface IRuleInstanceVisuNotify
    {
        Task NotifyValueChanged(RuleInterfaceInstance instance, object value);
        Task NotifyValueChanged(IDispatchable instance, DispatchValue value);
    }
}
