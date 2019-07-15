using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logics
{
    internal interface ILogicInstancesStore : IStore<RuleInstance, IRule>
    {
    }
}
