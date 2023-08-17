using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicInstanceStore : StoreBase<RuleInstance, ILogic>, ILogicInstancesStore
    {
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IDictionary<Guid, ILogic> _ruleInstanceRuleMap = new ConcurrentDictionary<Guid, ILogic>();

        public LogicInstanceStore(ILogicInstanceCache logicInstanceCache)
        {
            _logicInstanceCache = logicInstanceCache;
        }

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
            var ruleInstance = _logicInstanceCache.Get(ruleInstanceId);

            return new KeyValuePair<RuleInstance, ILogic>(ruleInstance, rule);
        }

        public override void Add(RuleInstance key, ILogic value)
        {
            base.Add(key, value);
            _ruleInstanceRuleMap[key.ObjId] = value;
        }

        public override void Clear()
        {
            base.Clear();
            _ruleInstanceRuleMap.Clear();
        }
    }
}
