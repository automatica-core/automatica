using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Control.Cache;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicInstanceCache : AbstractCache<RuleInstance>, ILogicInstanceCache
    {
        private readonly ILinkCache _linkCache;
        private readonly IAreaCache _areaCacheInstance;
        private readonly IDictionary<Guid, IList<RuleInstance>> _categoryCache = new ConcurrentDictionary<Guid, IList<RuleInstance>>();
        private readonly IDictionary<Guid, IList<RuleInstance>> _areaCache = new ConcurrentDictionary<Guid, IList<RuleInstance>>();
        private readonly IDictionary<Guid, RuleInstance> _favorites = new ConcurrentDictionary<Guid, RuleInstance>();


        private readonly IDictionary<Guid, Guid> _logicInstanceCategoryCache = new ConcurrentDictionary<Guid, Guid>();

        public LogicInstanceCache(IConfiguration configuration, ILinkCache linkCache, IAreaCache areaCacheInstance) : base(configuration)
        {
            _linkCache = linkCache;
            _areaCacheInstance = areaCacheInstance;
        }

        protected override IQueryable<RuleInstance> GetAll(AutomaticaContext context)
        {
            var all = context.RuleInstances
                .Include(a => a.RuleInterfaceInstance)
                .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                .ThenInclude(a => a.This2RuleTemplateNavigation)
                .Include(a => a.This2RuleTemplateNavigation)
                .Include(a => a.This2AreaInstanceNavigation)
                .Include(a => a.This2CategoryInstanceNavigation)
                .AsNoTracking();

            var ret = new List<RuleInstance>();
            foreach (var item in all)
            {
                foreach(var interfaces in item.RuleInterfaceInstance)
                {
                    var direction = context.RuleInterfaceDirections.Single(a =>
                        a.ObjId == interfaces.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection);
                    interfaces.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirectionNavigation = direction;
                }

                if (item.This2AreaInstance.HasValue)
                {
                    AddToAreaCache(item, item.This2AreaInstance.Value);
                }

                AddToCategoryCache(item, item.This2CategoryInstance);

                if (item.IsFavorite)
                {
                    _favorites.TryAdd(item.ObjId, item);
                }

                foreach (var interfaceInstance in item.RuleInterfaceInstance)
                {
                    if (_linkCache.IsRuleInterfaceMapped(interfaceInstance.ObjId))
                    {
                        interfaceInstance.IsLinked = true;
                    }
                }

                ret.Add(item);
            }

            return ret.AsQueryable();
        }

        private void AddToCategoryCache(RuleInstance item, Guid? category)
        {
            if (_logicInstanceCategoryCache.TryGetValue(item.ObjId, out var oldCategory))
            {
                if (_categoryCache.ContainsKey(oldCategory))
                {
                    var oldItem = _categoryCache[oldCategory].FirstOrDefault(a => a.ObjId == item.ObjId);
                    _categoryCache[oldCategory].Remove(oldItem);
                    _logicInstanceCategoryCache.Remove(oldCategory);
                }
            }

            if (category.HasValue)
            {
                var categoryValue = category.Value;
                _logicInstanceCategoryCache[item.ObjId] = categoryValue;

                if (!_categoryCache.ContainsKey(categoryValue))
                {
                    _categoryCache.Add(categoryValue, new List<RuleInstance>());
                }

                _categoryCache[categoryValue].Add(item);
            }
        }

        private void AddToAreaCache(RuleInstance item, Guid area)
        {
            lock (_areaCacheInstance)
            {
                if (!_areaCache.ContainsKey(area))
                {
                    _areaCache.Add(area, new List<RuleInstance>());
                }

                var existing = _areaCache[area].FirstOrDefault(a => a.ObjId == item.ObjId);
                if (existing != null)
                {
                    _areaCache[area].Remove(existing);
                }

                _areaCache[area].Add(item);


                var areaItem = _areaCacheInstance.Get(area);
                if (areaItem.This2Parent.HasValue)
                {
                    var parentArea = areaItem.This2Parent.Value;
                    AddToAreaCache(item, parentArea);

                }
            }
        }

        public override void Clear()
        {
            base.Clear();

            _areaCache.Clear();
            _categoryCache.Clear();
            _favorites.Clear();
        }

        protected override Guid GetKey(RuleInstance obj)
        {
            return obj.ObjId;
        }

        public RuleInstance GetSingle(AutomaticaContext context, Guid id)
        {
            var item = context.RuleInstances
                .Include(a => a.RuleInterfaceInstance)
                .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                .ThenInclude(a => a.This2RuleTemplateNavigation)
                .Include(a => a.This2RuleTemplateNavigation)
                .Include(a => a.This2AreaInstanceNavigation)
                .Include(a => a.This2CategoryInstanceNavigation)
                .AsNoTracking()
                .Single(a => a.ObjId == id);

            if (item.This2AreaInstance.HasValue)
            {
                AddToAreaCache(item, item.This2AreaInstance.Value);
            }
            foreach (var interfaces in item.RuleInterfaceInstance)
            {
                var direction = context.RuleInterfaceDirections.Single(a =>
                    a.ObjId == interfaces.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection);
                interfaces.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirectionNavigation = direction;
            }


            AddToCategoryCache(item, item.This2CategoryInstance);
            

            if (item.IsFavorite)
            {
                _favorites.TryAdd(item.ObjId, item);
            }

            foreach (var interfaceInstance in item.RuleInterfaceInstance)
            {
                if (_linkCache.IsRuleInterfaceMapped(interfaceInstance.ObjId))
                {
                    interfaceInstance.IsLinked = true;
                }
            }

            return item;
        }

        public IList<RuleInstance> ByFavorites()
        {
            Initialize();

            return _favorites.Values.ToList();
        }

        public IList<RuleInstance> ByCategory(Guid category)
        {
            Initialize();

            if (_categoryCache.TryGetValue(category, out var byCategory))
            {
                return byCategory;
            }
            return new List<RuleInstance>();
        }

        public IList<RuleInstance> ByArea(Guid category)
        {
            Initialize();

            if (_areaCache.TryGetValue(category, out var area))
            {
                return area;
            }
            return new List<RuleInstance>();
        }
    }
}
