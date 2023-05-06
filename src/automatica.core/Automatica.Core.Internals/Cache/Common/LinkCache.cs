using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Common
{
    internal class LinkCache : AbstractCache<Link>, ILinkCache
    {
        private readonly ILogicInterfaceInstanceCache _logicInterfaceCache;
        private readonly ILogicPageCache _logicPageCache;

        private readonly Dictionary<Guid, List<Link>> _fromRuleInstanceCache = new();

        public LinkCache(IConfiguration configuration, ILogicInterfaceInstanceCache logicInterfaceCache, ILogicPageCache logicPageCache) : base(configuration)
        {
            _logicInterfaceCache = logicInterfaceCache;
            _logicPageCache = logicPageCache;
        }

        protected override IQueryable<Link> GetAll(AutomaticaContext context)
        {
            var all = context.Links
                .Include(a => a.This2NodeInstance2RulePageInputNavigation)
                .Include(a => a.This2NodeInstance2RulePageOutputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceInputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceOutputNavigation)
                .AsNoTracking();

            foreach (var link in all)
            {
                if (link.This2RuleInterfaceInstanceOutput.HasValue)
                {
                    if (!_fromRuleInstanceCache.ContainsKey(link.This2RuleInterfaceInstanceOutput.Value))
                    {
                        _fromRuleInstanceCache.Add(link.This2RuleInterfaceInstanceOutput.Value, new List<Link>());
                    }

                    _fromRuleInstanceCache[link.This2RuleInterfaceInstanceOutput.Value].Add(link);
                }
                if (link.This2RuleInterfaceInstanceInput.HasValue)
                {
                    if (!_fromRuleInstanceCache.ContainsKey(link.This2RuleInterfaceInstanceInput.Value))
                    {
                        _fromRuleInstanceCache.Add(link.This2RuleInterfaceInstanceInput.Value, new List<Link>());
                    }

                    _fromRuleInstanceCache[link.This2RuleInterfaceInstanceInput.Value].Add(link);
                }
            }

            return all;
        }

        public Link GetSingle(Guid objId, AutomaticaContext context)
        {
            var item = context.Links
                .Include(a => a.This2NodeInstance2RulePageInputNavigation)
                .Include(a => a.This2NodeInstance2RulePageOutputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceInputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceOutputNavigation)
                .AsNoTracking().Single(a => a.ObjId == objId);

            if (Contains(objId))
            {
                Update(objId, item);
                _logicPageCache.UpdateLink(item);
            }
            else
            {
                Add(objId, item);
                _logicPageCache.AddLink(item);
            }


            return item;
        }

        public bool IsRuleInterfaceMapped(Guid objId)
        {
            Initialize();
            return _fromRuleInstanceCache.ContainsKey(objId) && _fromRuleInstanceCache[objId].Count > 0;
        }

        public override void Remove(Guid key)
        {
            _logicPageCache.RemoveLink(Get(key));
            base.Remove(key);
        }

        protected override Guid GetKey(Link obj)
        {
            return obj.ObjId;
        }

        public override void Clear()
        {
            _logicInterfaceCache.Clear();
            _fromRuleInstanceCache.Clear();
            base.Clear();
        }
    }
}
