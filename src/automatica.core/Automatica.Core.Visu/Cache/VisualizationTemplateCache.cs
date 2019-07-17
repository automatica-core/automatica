using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache;
using Automatica.Core.Internals.Cache.Visualization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Visu.Cache
{
    internal class VisualizationTemplateCache : AbstractCache<VisuObjectTemplate>, IVisualizationTemplateCache
    {
        public VisualizationTemplateCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<VisuObjectTemplate> GetAll(AutomaticaContext dbContext)
        {
            return dbContext.VisuObjectTemplates.AsNoTracking()
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.Constraints).ThenInclude(c => c.ConstraintData);
        }

        protected override Guid GetKey(VisuObjectTemplate obj)
        {
            return obj.ObjId;
        }
    }
}
