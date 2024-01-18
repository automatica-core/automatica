using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Logic
{
    internal class LogicNodeInstanceCache(IConfiguration configuration)
        : AbstractCache<NodeInstance2RulePage>(configuration), ILogicNodeInstanceCache
    {
        protected override IQueryable<NodeInstance2RulePage> GetAll(AutomaticaContext context)
        {
            return context.NodeInstance2RulePages.Include(a => a.This2NodeInstanceNavigation).ThenInclude(a => a.This2NodeTemplateNavigation);
        }

        protected override Guid GetKey(NodeInstance2RulePage obj)
        {
            return obj.ObjId;
        }

        public void AddOrUpdate(Guid objId, NodeInstance2RulePage instance)
        {
            if (Contains(objId))
            {
                Update(objId, instance);
            }
            else
            {
                Add(objId, instance);
            }
        }
    }
}
