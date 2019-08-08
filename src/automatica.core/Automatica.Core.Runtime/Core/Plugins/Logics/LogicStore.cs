using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.Runtime.Exceptions;

namespace Automatica.Core.Runtime.Core.Plugins.Logics
{
    internal class LogicStore : StoreBase<RuleInstance, IRule>, ILogicStore
    {
        private readonly IDictionary<Guid, IRule> _ruleIdDictionary = new ConcurrentDictionary<Guid, IRule>();

        public override void Add(RuleInstance key, IRule value)
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

        public object GetDataForRuleInstance(Guid id)
        {
            if (_ruleIdDictionary.ContainsKey(id))
            {
                return _ruleIdDictionary[id].GetDataForVisu();
            }
            throw new RuleNotFoundException();
        }
    }
}
