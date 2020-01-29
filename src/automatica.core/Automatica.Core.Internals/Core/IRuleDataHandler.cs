using System;

namespace Automatica.Core.Internals.Core
{
    public interface IRuleDataHandler
    {
        object GetDataForRuleInstance(Guid id);
    }
}
