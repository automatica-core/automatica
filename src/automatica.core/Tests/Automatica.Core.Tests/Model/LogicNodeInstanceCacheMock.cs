using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using System;

namespace Automatica.Core.Tests.Model
{
    internal class LogicNodeInstanceCacheMock : GuidStoreBase<NodeInstance2RulePage>, ILogicNodeInstanceCache
    {
        public void AddOrUpdate(Guid objId, NodeInstance2RulePage instance)
        {
            if (Contains(objId))
            {
                Update(objId, instance);
            }
            else
            {
                Add(objId, instance);
            }
        }
    }
}
