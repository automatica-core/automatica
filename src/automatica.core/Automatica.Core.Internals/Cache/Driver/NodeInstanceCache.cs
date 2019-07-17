using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Helper;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Internals.Cache.Driver
{
    internal class NodeInstanceCache : AbstractCache<NodeInstance>, INodeInstanceCache
    {
        private readonly INodeInstanceStateHandler _nodeInstanceStateHandler;

        public NodeInstanceCache(IConfiguration configuration, INodeInstanceStateHandler nodeInstanceStateHandler) : base(configuration)
        {
            _nodeInstanceStateHandler = nodeInstanceStateHandler;
        }

        protected override IQueryable<NodeInstance> GetAll(AutomaticaContext context)
        {
            var rootItem = context.NodeInstances.AsNoTracking().First(a => a.This2ParentNodeInstance == null && !a.IsDeleted);
            var allItems = context.NodeInstances.AsNoTracking().Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .Include(a => a.PropertyInstance)
                .Include(a => a.This2NodeTemplateNavigation)
                .Include(a => a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                .ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a => a.PropertyInstance)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .ThenInclude(a => a.This2NodeTemplateNavigation)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                    a.This2NodeTemplateNavigation.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation).ThenInclude(a =>
                    a.This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate)
                .ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                .Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .ThenInclude(a => a.This2NodeTemplateNavigation.PropertyTemplate).ThenInclude(b => b.Constraints)
                .ThenInclude(a => a.ConstraintData)
                .Include(a => a.This2AreaInstanceNavigation)
                .Include(a => a.This2CategoryInstanceNavigation).ToList();
            rootItem.InverseThis2ParentNodeInstanceNavigation = NodeInstanceHelper.FillRecursive(allItems, rootItem.ObjId);

            
            var items = new List<NodeInstance>();
            items.Add(rootItem);

            foreach (var item in items)
            {
                GetNodeInstanceStateRec(item);
            }


            return items.AsQueryable();
        }

        private void GetNodeInstanceStateRec(NodeInstance node)
        {
            node.State = _nodeInstanceStateHandler.GetNodeInstanceState(node.ObjId);

            if (node.InverseThis2ParentNodeInstanceNavigation != null &&
                node.InverseThis2ParentNodeInstanceNavigation.Count > 0)
            {
                foreach (var child in node.InverseThis2ParentNodeInstanceNavigation)
                {
                    GetNodeInstanceStateRec(child);
                }
            }
        }

        protected override Guid GetKey(NodeInstance obj)
        {
            return obj.ObjId;
        }
    }
}
