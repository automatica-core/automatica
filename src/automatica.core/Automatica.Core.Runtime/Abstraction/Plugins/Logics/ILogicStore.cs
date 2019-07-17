using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Automatica.Core.Rule;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logics
{
    internal interface ILogicStore : IStore<RuleInstance, IRule>, IRuleDataHandler
    {
        
    }
}
