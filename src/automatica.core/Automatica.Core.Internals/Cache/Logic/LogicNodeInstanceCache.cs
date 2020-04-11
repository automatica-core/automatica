using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicNodeInstanceCache : AbstractCache<NodeInstance2RulePage>, ILogicNodeInstanceCache
    {
        public LogicNodeInstanceCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<NodeInstance2RulePage> GetAll(AutomaticaContext context)
        {
            return context.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation);
        }

        protected override Guid GetKey(NodeInstance2RulePage obj)
        {
            return obj.ObjId;
        }
    }
}
