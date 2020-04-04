using System;
using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Internals.Cache.Driver
{
    public interface INodeTemplateCache : IStore<NodeTemplate>
    {
        ICollection<NodeTemplate> GetSupportedTemplates(NodeInstance targetNodeInstance, Guid neededInterfaceType);
    }
}
