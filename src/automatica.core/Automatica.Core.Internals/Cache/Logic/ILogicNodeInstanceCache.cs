using System;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Logic
{
    public interface ILogicNodeInstanceCache : IStore<NodeInstance2RulePage>
    {
        void AddOrUpdate(Guid existingInstanceObjId, NodeInstance2RulePage existingInstance);
    }
}
