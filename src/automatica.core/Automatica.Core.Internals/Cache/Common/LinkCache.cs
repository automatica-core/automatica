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

        public LinkCache(IConfiguration configuration, ILogicInterfaceInstanceCache logicInterfaceCache) : base(configuration)
        {
            _logicInterfaceCache = logicInterfaceCache;
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
