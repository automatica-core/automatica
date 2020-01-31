using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicInstanceCache : AbstractCache<RuleInstance>, ILogicInstanceCache
    {
        private readonly IDictionary<Guid, IList<RuleInstance>> _categoryCache = new ConcurrentDictionary<Guid, IList<RuleInstance>>();
        private readonly IDictionary<Guid, IList<RuleInstance>> _areaCache = new ConcurrentDictionary<Guid, IList<RuleInstance>>();

        public LogicInstanceCache(IConfiguration configuration) : base(configuration)
        {
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
                    if (!_areaCache.ContainsKey(item.This2AreaInstance.Value))
                    {
                        _areaCache.Add(item.This2AreaInstance.Value, new List<RuleInstance>());
                    }

                    _areaCache[item.This2AreaInstance.Value].Add(item);
                }

                if (item.This2CategoryInstance.HasValue)
                {
                    if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                    {
                        _categoryCache.Add(item.This2CategoryInstance.Value, new List<RuleInstance>());
                    }

                    _categoryCache[item.This2CategoryInstance.Value].Add(item);
                }
            }

            return all;
        }

        public override void Clear()
        {
            base.Clear();

            _areaCache.Clear();
            _categoryCache.Clear();
        }

        protected override Guid GetKey(RuleInstance obj)
        {
            return obj.ObjId;
        }

        public IList<RuleInstance> ByCategory(Guid category)
        {
            Initialize();

            if (_categoryCache.ContainsKey(category))
            {
                return _categoryCache[category];
            }
            return new List<RuleInstance>();
        }

        public IList<RuleInstance> ByArea(Guid category)
        {
            Initialize();

            if (_areaCache.ContainsKey(category))
            {
                return _areaCache[category];
            }
            return new List<RuleInstance>();
        }
    }
}
