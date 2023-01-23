using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using System.Collections.Generic;
using System;

namespace Automatica.Core.Runtime.Abstraction.Plugins.Logic
{
    public interface ILogicInstancesStore : IStore<RuleInstance, IRule>
    {
        bool ContainsRuleInstanceId(Guid ruleInstanceId);
        KeyValuePair<RuleInstance, IRule> GetByRuleInstanceId(Guid ruleInstanceId);
    }
}
