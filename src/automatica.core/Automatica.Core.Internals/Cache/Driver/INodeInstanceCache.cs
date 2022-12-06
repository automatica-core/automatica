using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Driver
{
    public interface INodeInstanceCache : IStore<NodeInstance>
    {
        IList<NodeInstance> ByFavorites();
        IList<NodeInstance> ByCategory(Guid category);
        IList<NodeInstance> ByArea(Guid category);

        NodeInstance Root { get; }

        NodeInstance GetDriverNodeInstanceFromChild(NodeInstance child);
        NodeInstance GetSingle(Guid objId, AutomaticaContext context);
        void Remove(NodeInstance existingNode);
    }
}
