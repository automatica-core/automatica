using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Automatica.Core.EF.Helper;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Driver
{
    internal class NodeInstanceCache : AbstractCache<NodeInstance>, INodeInstanceCache
    {
        private readonly INodeInstanceStateHandler _nodeInstanceStateHandler;

        private readonly IDictionary<Guid, IList<NodeInstance>> _categoryCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, IList<NodeInstance>> _areaCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, NodeInstance> _allCache = new ConcurrentDictionary<Guid, NodeInstance>();
        private NodeInstance _root;

        public NodeInstanceCache(IConfiguration configuration, INodeInstanceStateHandler nodeInstanceStateHandler) : base(configuration)
        {
            _nodeInstanceStateHandler = nodeInstanceStateHandler;
        }

        protected override IQueryable<NodeInstance> GetAll(AutomaticaContext context)
        {
            var rootItem = context.NodeInstances.AsNoTracking().First(a => a.This2ParentNodeInstance == null && !a.IsDeleted);

            var allItems = context.NodeInstances.AsNoTracking().Include(a => a.InverseThis2ParentNodeInstanceNavigation)
                .Include(a => a.PropertyInstance)
                .ThenInclude(a => a.This2PropertyTemplateNavigation)
                .ThenInclude(a => a.This2PropertyTypeNavigation)
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
                .Include(a => a.This2SlaveNavigation)
                .Include(a => a.This2CategoryInstanceNavigation)
                .Where(a => !a.IsDeleted && a.This2ParentNodeInstance != null).ToList();

            rootItem.InverseThis2ParentNodeInstanceNavigation = NodeInstanceHelper.FillRecursive(allItems, rootItem.ObjId);

            Root = rootItem;
            rootItem.State = NodeInstanceState.Loaded;
            GetNodeInstanceStateRec(rootItem);

            var items = new List<NodeInstance>();
            items.Add(rootItem);

            foreach (var item in allItems)
            {
                _allCache.Add(item.ObjId, item);
            }

            foreach (var item in allItems)
            {
                if (item.This2AreaInstance.HasValue)
                {
                    if (!_areaCache.ContainsKey(item.This2AreaInstance.Value))
                    {
                        _areaCache.Add(item.This2AreaInstance.Value, new List<NodeInstance>());
                    }

                    _areaCache[item.This2AreaInstance.Value].Add(item);
                }

                if (item.This2CategoryInstance.HasValue)
                {
                    if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                    {
                        _categoryCache.Add(item.This2CategoryInstance.Value, new List<NodeInstance>());
                    }

                    _categoryCache[item.This2CategoryInstance.Value].Add(item);
                }
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

        public override NodeInstance Get(Guid key)
        {
            if (_allCache.ContainsKey(key))
            {
                return _allCache[key];
            }
            return null;
        }

        protected override Guid GetKey(NodeInstance obj)
        {
            return obj.ObjId;
        }

        public override void Clear()
        {
            base.Clear();

            _allCache.Clear();

            _areaCache.Clear();
            _categoryCache.Clear();
        }

        public IList<NodeInstance> ByCategory(Guid category)
        {
            Initialize();

            if (_categoryCache.ContainsKey(category))
            {
                return _categoryCache[category];
            }
            return new List<NodeInstance>();
        }

        public IList<NodeInstance> ByArea(Guid category)
        {
            Initialize();

            if (_areaCache.ContainsKey(category))
            {
                return _areaCache[category];
            }
            return new List<NodeInstance>();
        }

        public NodeInstance Root
        {
            get
            {
                Initialize();
                return _root;

            }
            private set => _root = value;
        }
    }
}
