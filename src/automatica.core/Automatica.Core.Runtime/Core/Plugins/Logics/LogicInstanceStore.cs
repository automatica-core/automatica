using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins.Logics;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicInstanceStore : StoreBase<RuleInstance, IRule>, ILogicInstancesStore
    {
        private readonly IDictionary<Guid, IRule> _ruleInstanceRuleMap = new ConcurrentDictionary<Guid, IRule>();

        public override void Add(RuleInstance key, IRule value)
        {
            base.Add(key, value);
            _ruleInstanceRuleMap.Add(key.ObjId, value);
        }

        public override void Clear()
        {
            base.Clear();
            _ruleInstanceRuleMap.Clear();
        }
    }
}
