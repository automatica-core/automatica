using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;

namespace Automatica.Core.UnitTests.Base.Model
{
    public class LogicInterfaceInstanceCacheMock : GuidStoreBase<RuleInterfaceInstance>, ILogicInterfaceInstanceCache
    {
    }
}
