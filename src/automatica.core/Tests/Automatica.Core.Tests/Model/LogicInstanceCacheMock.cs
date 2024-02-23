using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;

namespace Automatica.Core.Tests.Model;

public class LogicInstanceCacheMock : GuidStoreBase<RuleInstance>, ILogicInstanceCache
{
    public RuleInstance GetSingle(AutomaticaContext context, Guid id)
    {
        throw new NotImplementedException();
    }

    public IList<RuleInstance> ByFavorites()
    {
        throw new NotImplementedException();
    }

    public IList<RuleInstance> ByCategory(Guid category)
    {
        throw new NotImplementedException();
    }

    public IList<RuleInstance> ByArea(Guid category)
    {
        throw new NotImplementedException();
    }
}