using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;

namespace Automatica.Core.UnitTests.Base.Model
{
    public class LinkCacheMock : GuidStoreBase<Link>, ILinkCache
    {
    }
}
