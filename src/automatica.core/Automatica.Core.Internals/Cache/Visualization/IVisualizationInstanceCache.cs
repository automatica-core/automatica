using System.Collections.Generic;
using Automatica.Core.Base.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Model;

namespace Automatica.Core.Internals.Cache.Visualization
{
    public class VisualizationDataFacade : TypedObject
    {
        public IList<RuleInstance> RuleInstances { get; set; }
        public IList<NodeInstance> NodeInstances { get; set; }
    }

    public interface IVisualizationInstanceCache : IStore<VisuObjectInstance>
    {
        
    }
}
