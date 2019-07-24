using System.Runtime.CompilerServices;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logics
{
    public interface ILogicInstancesStore : IStore<RuleInstance, IRule>
    {
    }
}
