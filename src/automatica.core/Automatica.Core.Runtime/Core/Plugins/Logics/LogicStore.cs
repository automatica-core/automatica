using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicStore : StoreBase<RuleInstance, ILogic>, ILogicStore
    {
        private readonly IDictionary<Guid, ILogic> _ruleIdDictionary = new ConcurrentDictionary<Guid, ILogic>();

        public LogicStore()
        {
            
        }
        public override void Add(RuleInstance key, ILogic value)
        {
            if (!_ruleIdDictionary.ContainsKey(key.ObjId))
            {
                _ruleIdDictionary.Add(key.ObjId, value);
            }

            base.Add(key, value);
        }

        public override void Clear()
        {
            base.Clear();
            _ruleIdDictionary.Clear();
        }

        public object? GetDataForRuleInstance(Guid id)
        {
            if (_ruleIdDictionary.TryGetValue(id, out var value))
            {
                return value.GetDataForVisu();
            }

            return null;
        }

        public void UpdateInstance(RuleInstance key, ILogic value)
        {
            Update(key, value);
        }
    }
}
