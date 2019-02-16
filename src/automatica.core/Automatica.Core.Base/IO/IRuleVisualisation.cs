using System;

namespace Automatica.Core.Base.IO
{
    /// <summary>
    /// Interface to get specific rule data for the visualisation
    /// </summary>
    public interface IRuleVisualisation
    {
        object GetDataForRuleInstance(Guid id);
    }
}
