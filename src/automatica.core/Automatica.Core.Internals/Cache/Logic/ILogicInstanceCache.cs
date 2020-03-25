using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Logic
{
    public interface ILogicInstanceCache : IStore<RuleInstance>
    {
        IList<RuleInstance> ByFavorites();
        IList<RuleInstance> ByCategory(Guid category);
        IList<RuleInstance> ByArea(Guid category);
    }
}
