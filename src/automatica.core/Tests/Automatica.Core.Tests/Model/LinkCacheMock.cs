using System;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.Tests.Model
{
    public class LinkCacheMock : GuidStoreBase<Link>, ILinkCache
    {
        public (Link link, bool isNew) GetSingle(Guid objId, AutomaticaContext context)
        {
            return (Get(objId), false);
        }

        public bool IsRuleInterfaceMapped(Guid objId)
        {
            return false;
        }
    }
}
