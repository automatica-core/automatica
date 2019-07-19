using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class AreaCache : AbstractCache<AreaInstance>, IAreaCache
    {
        public AreaCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<AreaInstance> GetAll(AutomaticaContext context)
        {
            var rootItems = context.AreaInstances.AsNoTracking().Where(a => a.This2Parent == null).ToList();
            var items = new List<AreaInstance>();

            foreach (var root in rootItems)
            {
                items.Add(RecursiveLoad(root, context));
            }

            return items.AsQueryable();
        }

        private static AreaInstance RecursiveLoad(AreaInstance parent, AutomaticaContext dbContext)
        {
            var loaded = dbContext.AreaInstances.AsNoTracking()
                .Include(a => a.InverseThis2ParentNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.This2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.NeedsThis2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.ProvidesThis2AreayTypeNavigation).SingleOrDefault(a => a.ObjId == parent.ObjId);

            var newChilds = new List<AreaInstance>();
            if (loaded == null)
            {
                return null;
            }

            foreach (var child in loaded.InverseThis2ParentNavigation)
            {
                newChilds.Add(RecursiveLoad(child, dbContext));
            }

            loaded.InverseThis2ParentNavigation = newChilds;
            return loaded;
        }

        protected override Guid GetKey(AreaInstance obj)
        {
            return obj.ObjId;
        }
    }
}
