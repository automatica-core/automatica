using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using System;

namespace Automatica.Core.Internals.Cache.Common
{
    public interface ILinkCache : IStore<Link>
    {
        Link GetSingle(Guid objId, AutomaticaContext context);

        bool IsRuleInterfaceMapped(Guid objId);
    }
}
