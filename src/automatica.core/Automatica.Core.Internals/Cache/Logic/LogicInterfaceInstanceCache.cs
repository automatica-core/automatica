using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicInterfaceInstanceCache : AbstractCache<RuleInterfaceInstance>, ILogicInterfaceInstanceCache
    {
        public LogicInterfaceInstanceCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<RuleInterfaceInstance> GetAll(AutomaticaContext context)
        {
            return context.RuleInterfaceInstances.Include(a => a.This2RuleInstanceNavigation).AsNoTracking();
        }

        protected override Guid GetKey(RuleInterfaceInstance obj)
        {
            return obj.ObjId;
        }
    }
}
