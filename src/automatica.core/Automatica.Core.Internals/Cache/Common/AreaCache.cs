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
        private readonly IDictionary<Guid, AreaInstance> _areaInstances = new Dictionary<Guid, AreaInstance>();

        public AreaCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<AreaInstance> GetAll(AutomaticaContext context)
        {
            var rootItems = context.AreaInstances.AsNoTracking().Where(a => a.This2Parent == null).ToList();
            var items = new List<AreaInstance>();



            foreach (var root in rootItems)
            {
                _areaInstances.Add(root.ObjId, root);
                items.Add(RecursiveLoad(root, context));
            }

            return items.AsQueryable();
        }

        private AreaInstance RecursiveLoad(AreaInstance parent, AutomaticaContext dbContext)
        {
            var loaded = dbContext.AreaInstances.AsNoTracking()
                .Include(a => a.InverseThis2ParentNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.This2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.NeedsThis2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.ProvidesThis2AreayTypeNavigation).SingleOrDefault(a => a.ObjId == parent.ObjId);


            var newChildren = new List<AreaInstance>();
            if (loaded == null)
            {
                return null;
            }

            if (!_areaInstances.ContainsKey(loaded.ObjId))
            {
                _areaInstances.Add(loaded.ObjId, loaded);
            }

            foreach (var child in loaded.InverseThis2ParentNavigation)
            {
                newChildren.Add(RecursiveLoad(child, dbContext));
            }

            loaded.InverseThis2ParentNavigation = newChildren;
            return loaded;
        }

        protected override Guid GetKey(AreaInstance obj)
        {
            return obj.ObjId;
        }

        public override void Clear()
        {
            _areaInstances.Clear();
            base.Clear();
        }

        public bool IsAreaExisting(Guid id)
        {
            return _areaInstances.ContainsKey(id);
        }
    }
}
