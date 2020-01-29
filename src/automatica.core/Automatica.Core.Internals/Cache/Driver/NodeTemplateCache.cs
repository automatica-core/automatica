using System;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Driver
{
    internal class NodeTemplateCache : AbstractCache<NodeTemplate>, INodeTemplateCache
    {
        public NodeTemplateCache(IConfiguration configuration) : base(configuration)
        {
        }

        protected override IQueryable<NodeTemplate> GetAll(AutomaticaContext context)
        {
            var x = context.NodeTemplates.AsNoTracking()
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.Constraints).ThenInclude(c => c.ConstraintData);

            return x;
        }

        protected override Guid GetKey(NodeTemplate obj)
        {
            return obj.ObjId;
        }
    }
}
