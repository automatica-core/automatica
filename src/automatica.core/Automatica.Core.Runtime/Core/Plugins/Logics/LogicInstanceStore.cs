using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicInstanceStore : StoreBase<RuleInstance, IRule>, ILogicInstancesStore
    {
        private readonly IDictionary<Guid, IRule> _ruleInstanceRuleMap = new ConcurrentDictionary<Guid, IRule>();
        private readonly IDictionary<Guid, RuleInstance> _ruleInstanceMap = new ConcurrentDictionary<Guid, RuleInstance>();

        public bool ContainsRuleInstanceId(Guid ruleInstanceId)
        {
            return _ruleInstanceRuleMap.ContainsKey(ruleInstanceId);
        }

        public KeyValuePair<RuleInstance, IRule> GetByRuleInstanceId(Guid ruleInstanceId)
        {
            if (!_ruleInstanceRuleMap.ContainsKey(ruleInstanceId))
            {
                throw new KeyNotFoundException();
            }

            var rule = _ruleInstanceRuleMap[ruleInstanceId];
            var ruleInstance = _ruleInstanceMap[ruleInstanceId];

            return new KeyValuePair<RuleInstance, IRule>(ruleInstance, rule);
        }

        public override void Add(RuleInstance key, IRule value)
        {
            base.Add(key, value);
            _ruleInstanceRuleMap.Add(key.ObjId, value);
            _ruleInstanceMap.Add(key.ObjId, key);
        }

        public override void Clear()
        {
            base.Clear();
            _ruleInstanceRuleMap.Clear();
            _ruleInstanceMap.Clear();
        }
    }
}
