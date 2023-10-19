using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Helper;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Cache.Driver
{
    internal class NodeInstanceCache : AbstractCache<NodeInstance>, INodeInstanceCache
    {
        private readonly INodeInstanceStateHandler _nodeInstanceStateHandler;
        private readonly INodeTemplateCache _nodeTemplateCache;
        private readonly IAreaCache _areaCacheInstance;

        private readonly IDictionary<Guid, IList<NodeInstance>> _categoryCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, IList<NodeInstance>> _areaCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, NodeInstance> _allCache = new ConcurrentDictionary<Guid, NodeInstance>();
        private readonly ConcurrentDictionary<Guid, NodeInstance> _favorites = new ConcurrentDictionary<Guid, NodeInstance>();
        private NodeInstance _root;

        public NodeInstanceCache(IConfiguration configuration, INodeInstanceStateHandler nodeInstanceStateHandler, INodeTemplateCache nodeTemplateCache, IAreaCache areaCache) : base(configuration)
        {
            _nodeInstanceStateHandler = nodeInstanceStateHandler;
            _nodeTemplateCache = nodeTemplateCache;
            _areaCacheInstance = areaCache;
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
                    AddToAreaCache(item, item.This2AreaInstance.Value);
                }

                if (item.This2CategoryInstance.HasValue)
                {
                    if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                    {
                        _categoryCache.Add(item.This2CategoryInstance.Value, new List<NodeInstance>());
                    }

                    _categoryCache[item.This2CategoryInstance.Value].Add(item);
                }
                if (item.IsFavorite)
                {
                    if (!_favorites.ContainsKey(item.ObjId))
                    {
                        _favorites.TryAdd(item.ObjId, item);
                    }
                    else
                    {
                        _favorites[item.ObjId] = item;
                    }
                }
                else
                {
                    if (_favorites.ContainsKey(item.ObjId))
                    {
                        _favorites.TryRemove(item.ObjId, out var node);
                    }
                }
            }


            return items.AsQueryable();
        }

        private void AddToAreaCache(NodeInstance item, Guid area)
        {
            lock (_areaCacheInstance)
            {
                if (!_areaCache.ContainsKey(area))
                {
                    _areaCache.Add(area, new List<NodeInstance>());
                }

                var existing = _areaCache[area].FirstOrDefault(a => a.ObjId == item.ObjId);
                if (existing != null)
                {
                    _areaCache[area].Remove(existing);
                }

                _areaCache[area].Add(item);


                var areaItem = _areaCacheInstance.Get(area);
                if (areaItem.This2Parent.HasValue)
                {
                    var parentArea = areaItem.This2Parent.Value;
                    AddToAreaCache(item, parentArea);

                }
            }
        }

        public NodeInstance GetSingle(Guid objId, AutomaticaContext context)
        {

            var item = context.NodeInstances.AsNoTracking().Include(a => a.InverseThis2ParentNodeInstanceNavigation)
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
               .FirstOrDefault(a => a.ObjId == objId);

            if (item == null)
            {
                return null;

            }

            if (_allCache.ContainsKey(item.ObjId))
            {
                var cachedItem = _allCache[item.ObjId];
                _allCache[item.ObjId] = item;
                item.InverseThis2ParentNodeInstanceNavigation = NodeInstanceHelper.FillRecursive(_allCache.Values.ToList(), item.ObjId);
                if (item.This2ParentNodeInstance.HasValue && _allCache.ContainsKey(item.This2ParentNodeInstance.Value))
                {
                    item.This2ParentNodeInstanceNavigation = _allCache[item.This2ParentNodeInstance.Value];
                    var oldItem = _allCache[item.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation
                        .SingleOrDefault(a => a.ObjId == item.ObjId);

                    if (oldItem != null)
                    {
                        _allCache[item.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation
                            .Remove(oldItem);
                    }
                    else //seems that we moved the node to a new parent, we need to remove it from the old parent
                    {
                        var oldParent = _allCache[cachedItem.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation
                            .Single(a => a.ObjId == cachedItem.ObjId);
                ;
                        _allCache[cachedItem.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation
                            .Remove(oldParent);
                    }


                    _allCache[item.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation.Add(item);
                }

            }
            else
            {
                _allCache.Add(item.ObjId, item);

                item.InverseThis2ParentNodeInstanceNavigation = NodeInstanceHelper.FillRecursive(_allCache.Values.ToList(), item.ObjId);

                if(item.This2ParentNodeInstance.HasValue) 
                {
                    item.This2ParentNodeInstanceNavigation = _allCache[item.This2ParentNodeInstance.Value];
                    _allCache[item.This2ParentNodeInstance.Value].InverseThis2ParentNodeInstanceNavigation.Add(item);
                }
            }

            if (item.This2AreaInstance.HasValue)
            {
                AddToAreaCache(item, item.This2AreaInstance.Value);
            }

            if (item.This2CategoryInstance.HasValue)
            {
                if (!_categoryCache.ContainsKey(item.This2CategoryInstance.Value))
                {
                    _categoryCache.Add(item.This2CategoryInstance.Value, new List<NodeInstance>());
                }

                var existing = _categoryCache[item.This2CategoryInstance.Value].FirstOrDefault(a => a.ObjId == item.ObjId);
                if (existing != null)
                {
                    _categoryCache[item.This2CategoryInstance.Value].Remove(existing);
                }

                _categoryCache[item.This2CategoryInstance.Value].Add(item);
            }

            if (item.IsFavorite)
            {
                if (!_favorites.ContainsKey(item.ObjId))
                {
                    _favorites.TryAdd(item.ObjId, item);
                }
                else
                {
                    _favorites[item.ObjId] = item;
                }
            }
            else
            {
                if (_favorites.ContainsKey(item.ObjId))
                {
                    _favorites.TryRemove(item.ObjId, out var node);
                }
            }

            return item;
        }

        public void Remove(NodeInstance existingNode)
        {
            _allCache.Remove(existingNode.ObjId);
            if (existingNode.This2ParentNodeInstance.HasValue)
            {
                var parent = _allCache[existingNode.This2ParentNodeInstance.Value];

                if (parent.InverseThis2ParentNodeInstanceNavigation != null)
                {
                    var parentExistingChild = parent.InverseThis2ParentNodeInstanceNavigation
                        .Find(a => a.ObjId == existingNode.ObjId);

                    parent.InverseThis2ParentNodeInstanceNavigation.Remove(parentExistingChild);
                }
            }
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
            Initialize();

            if (_allCache.TryGetValue(key, out var value))
            {
                return value;
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
            _favorites.Clear();
        }

        public IList<NodeInstance> ByFavorites()
        {
            Initialize();

            return _favorites.Values.Where(a => a.UseInVisu).ToList();
        }

        public IList<NodeInstance> ByCategory(Guid category)
        {
            Initialize();

            if (_categoryCache.TryGetValue(category, out var byCategory))
            {
                return byCategory;
            }
            return new List<NodeInstance>();
        }

        public IList<NodeInstance> ByArea(Guid category)
        {
            Initialize();

            if (_areaCache.TryGetValue(category, out var area))
            {
                return area;
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

        public NodeInstance GetDriverNodeInstanceFromChild(NodeInstance child)
        {
            if (child == null)
            {
                return null;
            }

            if (child.This2NodeTemplate != null)
            {
                var nodeTemplate = _nodeTemplateCache.Get(child.This2NodeTemplate.Value);

                if (nodeTemplate.ProvidesInterface2InterfaceTypeNavigation.IsDriverInterface)
                {
                    return child;
                }
            }

            if (!child.This2ParentNodeInstance.HasValue)
            {
                return null;
            }
            return GetDriverNodeInstanceFromChild(Get(child.This2ParentNodeInstance.Value));
        }
    }
}
