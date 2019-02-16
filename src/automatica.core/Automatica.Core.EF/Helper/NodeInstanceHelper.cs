using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.EF.Helper
{
    public static class NodeInstanceHelper
    {
        public static List<NodeInstance> FillRecursive(IList<NodeInstance> flatObjects, Guid parentId)
        {
            var recursiveObjects = new List<NodeInstance>();
            foreach (var item in flatObjects.Where(x => x.This2ParentNodeInstance.Equals(parentId)))
            {
                item.InverseThis2ParentNodeInstanceNavigation = FillRecursive(flatObjects, item.ObjId);
                recursiveObjects.Add(item);
            }
            return recursiveObjects;
        }

        public static NodeInstance RecursiveLoad(NodeInstance parent, AutomaticaContext dbContext)
        {
            var loaded = dbContext.NodeInstances
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
                .Include(a => a.This2CategoryInstanceNavigation)
                .SingleOrDefault(a => a.ObjId == parent.ObjId && !a.IsDeleted);


            var newChilds = new List<NodeInstance>();
            if (loaded == null)
            {
                return null;
            }

            foreach (var child in loaded.InverseThis2ParentNodeInstanceNavigation)
            {
                newChilds.Add(RecursiveLoad(child, dbContext));
            }

            loaded.InverseThis2ParentNodeInstanceNavigation = newChilds;
            return loaded;


        }
    }
}
