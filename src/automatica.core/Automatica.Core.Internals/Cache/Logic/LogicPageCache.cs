using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicPageCache : AbstractCache<RulePage>, ILogicPageCache
    {
        public LogicPageCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<RulePage> GetAll(AutomaticaContext context)
        {
            return context.RulePages.AsNoTracking().Include(a => a.This2RulePageTypeNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2NodeInstance2RulePageInputNavigation)
                    .ThenInclude(a => a.This2NodeInstanceNavigation)
                    .ThenInclude(a => a.This2NodeTemplateNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2NodeInstance2RulePageOutputNavigation)
                    .ThenInclude(a => a.This2NodeInstanceNavigation)
                    .ThenInclude(a => a.This2NodeTemplateNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2RuleInterfaceInstanceInputNavigation)
                    .ThenInclude(a => a.This2RuleInstanceNavigation)
                .Include(a => a.Link)
                    .ThenInclude(a => a.This2RuleInterfaceInstanceOutputNavigation)
                    .ThenInclude(a => a.This2RuleInstanceNavigation)
                .Include(a => a.NodeInstance2RulePage).ThenInclude(b => b.This2NodeInstanceNavigation)
                .ThenInclude(x => x.PropertyInstance)
                .Include(a => a.NodeInstance2RulePage).ThenInclude(b => b.This2NodeInstanceNavigation)
                .ThenInclude(b => b.This2NodeTemplateNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2RuleTemplateNavigation)
                .ThenInclude(a => a.RuleInterfaceTemplate)
                .Include(a => a.RuleInstance).ThenInclude(a => a.RuleInterfaceInstance)
                .ThenInclude(a => a.This2RuleInterfaceTemplateNavigation)
                .ThenInclude(a => a.This2RuleInterfaceDirectionNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2AreaInstanceNavigation)
                .Include(a => a.RuleInstance).ThenInclude(a => a.This2CategoryInstanceNavigation);
        }

        protected override Guid GetKey(RulePage obj)
        {
            return obj.ObjId;
        }
    }
}
