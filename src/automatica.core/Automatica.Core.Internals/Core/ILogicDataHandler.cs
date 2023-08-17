using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using System;

namespace Automatica.Core.Internals.Core
{
    public interface ILogicDataHandler
    {
        object GetDataForRuleInstance(Guid id);
        void UpdateInstance(RuleInstance key, ILogic value);
    }
}
