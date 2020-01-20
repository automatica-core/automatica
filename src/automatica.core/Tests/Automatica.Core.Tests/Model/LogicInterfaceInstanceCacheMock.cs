using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;

namespace Automatica.Core.Tests.Model
{
    public class LogicInterfaceInstanceCacheMock : GuidStoreBase<RuleInterfaceInstance>, ILogicInterfaceInstanceCache
    {
    }
}
