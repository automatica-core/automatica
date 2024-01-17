using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.Control.Base;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Logic;
using Automatica.Core.Model;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicInstanceStore : StoreBase<RuleInstance, ILogic>, ILogicInstancesStore
    {
        private readonly ILogicInstanceCache _logicInstanceCache;
        private readonly IControlCache _controlCache;
        private readonly IDictionary<Guid, ILogic> _ruleInstanceRuleMap = new ConcurrentDictionary<Guid, ILogic>();

        public LogicInstanceStore(ILogicInstanceCache logicInstanceCache, IControlCache controlCache)
        {
            _logicInstanceCache = logicInstanceCache;
            _controlCache = controlCache;
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

            if (value is IControl iControl)
            {
                _controlCache.Add(iControl.Id, iControl);
            }
       
        }

        public override void Update(RuleInstance key, ILogic value)
        {
            _ruleInstanceRuleMap[key.ObjId] = value;
      
            if (value is IControl iControl)
            {
                _controlCache.Update(iControl.Id, iControl);
            }
            base.Update(key, value);
        }

        public override void Remove(RuleInstance key)
        {
            _ruleInstanceRuleMap.Remove(key.ObjId);
            _controlCache.Remove(key.ObjId);
            
            base.Remove(key);
        }

        public override void Clear()
        {
            base.Clear();
            _ruleInstanceRuleMap.Clear();
            _controlCache.Clear();
        }
    }
}
