using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache;
using Automatica.Core.Internals.Cache.Visualization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Visu.Cache
{
    internal class VisualizationInstanceCache : AbstractCache<VisuObjectInstance>, IVisualizationInstanceCache
    {
        public VisualizationInstanceCache(IConfiguration config) : base(config)
        {
        }


        protected override IQueryable<VisuObjectInstance> GetAll(AutomaticaContext context)
        {
            return context.VisuObjectInstances.AsNoTracking();
        }

        protected override Guid GetKey(VisuObjectInstance obj)
        {
            return obj.ObjId;
        }


    }
}
