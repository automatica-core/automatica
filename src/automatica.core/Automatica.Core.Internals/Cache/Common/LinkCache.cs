using System;
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

        public LinkCache(IConfiguration configuration, ILogicInterfaceInstanceCache logicInterfaceCache, ILogicPageCache logicPageCache) : base(configuration)
        {
            _logicInterfaceCache = logicInterfaceCache;
            _logicPageCache = logicPageCache;
        }

        protected override IQueryable<Link> GetAll(AutomaticaContext context)
        {
            return context.Links
                .Include(a => a.This2NodeInstance2RulePageInputNavigation)
                .Include(a => a.This2NodeInstance2RulePageOutputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceInputNavigation)
                .Include(a => a.This2RuleInterfaceInstanceOutputNavigation)
                .AsNoTracking();
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
            base.Clear();
        }
    }
}
