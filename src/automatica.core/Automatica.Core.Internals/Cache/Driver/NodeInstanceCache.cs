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
        private readonly ICategoryCache _categoryCacheInstance;

        private readonly IDictionary<Guid, IList<NodeInstance>> _categoryCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, IList<NodeInstance>> _areaCache = new ConcurrentDictionary<Guid, IList<NodeInstance>>();
        private readonly IDictionary<Guid, NodeInstance> _allCache = new ConcurrentDictionary<Guid, NodeInstance>();
        private readonly IDictionary<Guid, List<NodeInstance>> _allParentCache = new ConcurrentDictionary<Guid, List<NodeInstance>>();
        private readonly ConcurrentDictionary<Guid, NodeInstance> _favorites = new ConcurrentDictionary<Guid, NodeInstance>();
        private NodeInstance _root;

        public NodeInstanceCache(IConfiguration configuration, INodeInstanceStateHandler nodeInstanceStateHandler, INodeTemplateCache nodeTemplateCache, IAreaCache areaCache, ICategoryCache categoryCacheInstance) : base(configuration)
        {
            _nodeInstanceStateHandler = nodeInstanceStateHandler;
            _nodeTemplateCache = nodeTemplateCache;
            _areaCacheInstance = areaCache;
            _categoryCacheInstance = categoryCacheInstance;
        }

        protected override IQueryable<NodeInstance> GetAll(AutomaticaContext context)
        {
            var rootItem = context.NodeInstances.AsNoTracking().First(a => a.This2ParentNodeInstance == null && !a.IsDeleted);

            var allItems = context.NodeInstances.AsNoTracking()
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
                FillItem(item, context);

                if (item.This2ParentNodeInstance.HasValue)
                {
                    if (!_allParentCache.ContainsKey(item.This2ParentNodeInstance.Value))
                    {
                        _allParentCache.Add(item.This2ParentNodeInstance.Value, new List<NodeInstance>());
                    }
                    _allParentCache[item.This2ParentNodeInstance.Value].Add(item);
                }

            }

            foreach (var item in allItems)
            {
                if (_allParentCache.TryGetValue(item.ObjId, out var value))
                {
                    item.InverseThis2ParentNodeInstanceNavigation = value;
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

        private void FillItem(NodeInstance item, AutomaticaContext context)
        {
            var properties = context.PropertyInstances.AsNoTracking().Where(a => a.This2NodeInstance == item.ObjId).ToList();

            foreach (var property in properties)
            {
                var propertyTemplate = context.PropertyTemplates.AsNoTracking().First(a => a.ObjId == property.This2PropertyTemplate);
                var propertyType = context.PropertyTypes.AsNoTracking().First(a => a.Type == propertyTemplate.This2PropertyType);

                property.This2PropertyTemplateNavigation = propertyTemplate;
                propertyTemplate.This2PropertyTypeNavigation = propertyType;

                var constraints = context.PropertyTemplateConstraints.AsNoTracking().Where(a => a.This2PropertyTemplate == propertyTemplate.ObjId).ToList();
                propertyTemplate.Constraints = constraints;
            }

            item.PropertyInstance = properties;

            var nodeTemplate = context.NodeTemplates.AsNoTracking().First(a => a.ObjId == item.This2NodeTemplate);
            item.This2NodeTemplateNavigation = nodeTemplate;

            var needsInterfaces = context.InterfaceTypes.AsNoTracking().First(a => a.Type == nodeTemplate.NeedsInterface2InterfacesType);
            var providesInterface = context.InterfaceTypes.AsNoTracking().First(a => a.Type == nodeTemplate.ProvidesInterface2InterfaceType);

            nodeTemplate.NeedsInterface2InterfacesTypeNavigation = needsInterfaces;
            nodeTemplate.ProvidesInterface2InterfaceTypeNavigation = providesInterface;

            if (item.This2Slave.HasValue)
            {
                item.This2SlaveNavigation =
                    context.Slaves.AsNoTracking().FirstOrDefault(a => a.ObjId == item.This2Slave);
            }

            if (item.This2AreaInstance.HasValue)
            {
                item.This2AreaInstanceNavigation = context.AreaInstances.AsNoTracking()
                    .FirstOrDefault(a => a.ObjId == item.This2AreaInstance);
            }
            
            if (item.This2CategoryInstance.HasValue)
            {
                item.This2CategoryInstanceNavigation = context.CategoryInstances.AsNoTracking()
                    .FirstOrDefault(a => a.ObjId == item.This2CategoryInstance);
            }

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
                .FirstOrDefault(a => a.ObjId == objId);

            if (item == null)
            {
                return null;

            }

            FillItem(item, context);

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
                if (existingNode.This2ParentNodeInstance.HasValue && _allCache.TryGetValue(existingNode.This2ParentNodeInstance.Value, out var parent))
                {
                    if (parent.InverseThis2ParentNodeInstanceNavigation != null)
                    {
                        var parentExistingChild = parent.InverseThis2ParentNodeInstanceNavigation
                            .Find(a => a.ObjId == existingNode.ObjId);

                        parent.InverseThis2ParentNodeInstanceNavigation.Remove(parentExistingChild);
                    }
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

        public NodeInstance __ { get; set; }

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
