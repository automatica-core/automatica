using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Automatica.Core.Logic;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logic
{
    internal interface ILogicStore : IStore<RuleInstance, ILogic>, ILogicDataHandler
    {
        
    }
}
