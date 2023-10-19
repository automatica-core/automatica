using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var item in all)
            {
                if (item.This2AreaInstance.HasValue)
                {
                    AddToAreaCache(item, item.This2AreaInstance.Value);
                }

                if (item.This2CategoryInstance.HasValue)
                {
                    if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                    {
                        _categoryCache.Add(item.This2CategoryInstance.Value, new List<RuleInstance>());
                    }

                    _categoryCache[item.This2CategoryInstance.Value].Add(item);
                }

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
            }

            return all;
        }
        private void AddToAreaCache(RuleInstance item, Guid area)
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


            var areaItem = _areaCacheInstance.Get( area);
            if (areaItem.This2Parent.HasValue)
            {
                var parentArea = areaItem.This2Parent.Value;
                AddToAreaCache(item, parentArea);

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

            if (item.This2CategoryInstance.HasValue)
            {
                if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                {
                    _categoryCache.Add(item.This2CategoryInstance.Value, new List<RuleInstance>());
                }

                _categoryCache[item.This2CategoryInstance.Value].Add(item);
            }

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
