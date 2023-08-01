using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicInstanceStore : StoreBase<RuleInstance, ILogic>, ILogicInstancesStore
    {
        private readonly IDictionary<Guid, ILogic> _ruleInstanceRuleMap = new ConcurrentDictionary<Guid, ILogic>();
        private readonly IDictionary<Guid, RuleInstance> _ruleInstanceMap = new ConcurrentDictionary<Guid, RuleInstance>();

        public bool ContainsRuleInstanceId(Guid ruleInstanceId)
        {
            return _ruleInstanceRuleMap.ContainsKey(ruleInstanceId);
        }

        public KeyValuePair<RuleInstance, ILogic> GetByRuleInstanceId(Guid ruleInstanceId)
        {
            if (!_ruleInstanceRuleMap.ContainsKey(ruleInstanceId))
            {
                throw new KeyNotFoundException();
            }

            var rule = _ruleInstanceRuleMap[ruleInstanceId];
            var ruleInstance = _ruleInstanceMap[ruleInstanceId];

            return new KeyValuePair<RuleInstance, ILogic>(ruleInstance, rule);
        }

        public override void Add(RuleInstance key, ILogic value)
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
