using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using System.Collections.Generic;
using System;
using Automatica.Core.Logic;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logic
{
    public interface ILogicInstancesStore : IStore<RuleInstance, ILogic>
    {
        bool ContainsRuleInstanceId(Guid ruleInstanceId);
        KeyValuePair<RuleInstance, ILogic> GetByRuleInstanceId(Guid ruleInstanceId);
    }
}
