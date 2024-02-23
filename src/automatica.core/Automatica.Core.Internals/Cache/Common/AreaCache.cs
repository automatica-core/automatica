using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class AreaCache(IConfiguration configuration) : AbstractCache<AreaInstance>(configuration), IAreaCache
    {
        private readonly IDictionary<Guid, AreaInstance> _areaInstances = new Dictionary<Guid, AreaInstance>();

        protected override IQueryable<AreaInstance> GetAll(AutomaticaContext context)
        {
            var areas = context.AreaInstances.AsNoTracking()
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.This2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.NeedsThis2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.ProvidesThis2AreayTypeNavigation);
           
            foreach (var area in areas)
            {
                _areaInstances.Add(area.ObjId, area);
            }

            return _areaInstances.Values.AsQueryable();
        }

        public override AreaInstance Get(Guid key)
        {
            Initialize();
            return _areaInstances[key];
        }


        public AreaInstance GetSingle(AutomaticaContext context, Guid guid)
        {
            return context.AreaInstances.AsNoTracking()
                .Include(a => a.InverseThis2ParentNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.This2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.NeedsThis2AreaTypeNavigation)
                .Include(a => a.This2AreaTemplateNavigation)
                .ThenInclude(a => a.ProvidesThis2AreayTypeNavigation).SingleOrDefault(a => a.ObjId == guid);
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
