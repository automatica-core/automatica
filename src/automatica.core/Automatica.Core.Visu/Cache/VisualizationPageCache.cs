using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Cache.Visualization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Visu.Cache
{
    internal class VisualizationPageCache : AbstractCache<VisuPage>, IVisualizationPageCache
    {
        private readonly INodeInstanceCache _nodeInstanceCache;
        private readonly ICategoryCache _categoryCache;
        private readonly IAreaCache _areaCache;
        private readonly ILogicInstanceCache _logicInstanceCache;

        public VisualizationPageCache(IConfiguration config, INodeInstanceCache nodeInstanceCache, ICategoryCache categoryCache, IAreaCache areaCache, ILogicInstanceCache logicInstanceCache) : base(config)
        {
            _nodeInstanceCache = nodeInstanceCache;
            _categoryCache = categoryCache;
            _areaCache = areaCache;
            _logicInstanceCache = logicInstanceCache;
        }

        protected override IQueryable<VisuPage> GetAll(AutomaticaContext context)
        {
            var pages = context.VisuPages.AsNoTracking().ToList();

            var ret = new List<VisuPage>();
            foreach (var page in pages)
            {
                ret.Add(LoadPage(context, page.ObjId));
            }

            return ret.AsQueryable();
        }


        private VisuPage LoadPage(AutomaticaContext context, Guid pageId)
        {
            return context.VisuPages.AsNoTracking()
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(c => c.This2PropertyTemplateNavigation)
                .ThenInclude(c => c.Constraints)
                .ThenInclude(c => c.ConstraintData)
                .Include(a => a.VisuObjectInstances).ThenInclude(a => a.This2VisuObjectTemplateNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(c => c.This2PropertyTemplateNavigation).ThenInclude(d => d.This2PropertyTypeNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueNodeInstanceNavigation)
                .Include(a => a.This2VisuPageTypeNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueRulePageNavigation)
                .Include(a => a.VisuObjectInstances)
                .ThenInclude(b => b.PropertyInstance)
                .ThenInclude(b => b.ValueVisuPageNavigation)
                .Include(a => a.VisuObjectInstances).ThenInclude(a => a.PropertyInstance)
                .ThenInclude(a => a.ValueAreaInstanceNavigation)
                .SingleOrDefault(a => a.ObjId == pageId);
        }


        public VisualizationDataFacade AllByCategory(Guid categoryId)
        {
            return new VisualizationDataFacade()
            {
                NodeInstances = _nodeInstanceCache.ByCategory(categoryId),
                RuleInstances = _logicInstanceCache.ByCategory(categoryId)
            };
        }

        public VisualizationDataFacade AllByArea(Guid areaInstance)
        {
            return new VisualizationDataFacade()
            {
                NodeInstances = _nodeInstanceCache.ByArea(areaInstance),
                RuleInstances = _logicInstanceCache.ByArea(areaInstance)
            };
        }

        public object ByPage(Guid id)
        {
            if (_areaCache.Contains(id))
            {
                return AllByArea(id);
            }

            if (_categoryCache.Contains(id))
            {
                return AllByCategory(id);
            }

            return Get(id);
        }

        protected override Guid GetKey(VisuPage obj)
        {
            return obj.ObjId;
        }
    }
}
