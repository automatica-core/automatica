using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;

namespace Automatica.Core.Tests.Model
{
    public class NodeInstanceCacheMock : GuidStoreBase<NodeInstance>, INodeInstanceCache
    {
        public IList<NodeInstance> ByFavorites()
        {
            throw new NotImplementedException();
        }

        public IList<NodeInstance> ByCategory(Guid category)
        {
            throw new NotImplementedException();
        }

        public IList<NodeInstance> ByArea(Guid category)
        {
            throw new NotImplementedException();
        }

        public NodeInstance Root => null;
        public NodeInstance GetDriverNodeInstanceFromChild(NodeInstance child)
        {
            throw new NotImplementedException();
        }

        public NodeInstance GetSingle(Guid objId, AutomaticaContext context)
        {
            throw new NotImplementedException();
        }
    }
}
