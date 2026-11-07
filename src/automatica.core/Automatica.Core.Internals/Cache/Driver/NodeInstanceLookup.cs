#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Internals.Cache.Driver
{
    /// <summary>
    /// Everything a node instance is filled with (properties, templates, interfaces, slave, area, category), loaded with one query per table.
    /// Filling each node with its own queries took ~10 queries per node plus 3 per property, half a minute on a NAS with a few thousand nodes.
    /// The templates and lookup rows are shared by the node instances that use them.
    /// </summary>
    internal sealed class NodeInstanceLookup
    {
        private readonly ILookup<Guid, PropertyInstance> _properties;
        private readonly Dictionary<Guid, PropertyTemplate> _propertyTemplates;
        private readonly Dictionary<long, PropertyType> _propertyTypes;
        private readonly Dictionary<Guid, NodeTemplate> _nodeTemplates;
        private readonly Dictionary<Guid, InterfaceType> _interfaceTypes;
        private readonly Dictionary<Guid, Slave> _slaves;
        private readonly Dictionary<Guid, AreaInstance> _areas;
        private readonly Dictionary<Guid, CategoryInstance> _categories;

        private NodeInstanceLookup(AutomaticaContext context)
        {
            _properties = context.PropertyInstances.AsNoTracking().Where(a => a.This2NodeInstance != null).ToList()
                .ToLookup(a => a.This2NodeInstance!.Value);

            _propertyTypes = context.PropertyTypes.AsNoTracking().ToList().GroupBy(a => a.Type).ToDictionary(a => a.Key, a => a.First());
            var constraints = context.PropertyTemplateConstraints.AsNoTracking().ToList().ToLookup(a => a.This2PropertyTemplate);

            _propertyTemplates = context.PropertyTemplates.AsNoTracking().ToList().GroupBy(a => a.ObjId).ToDictionary(a => a.Key, a => a.First());
            foreach (var template in _propertyTemplates.Values)
            {
                template.Constraints = constraints[template.ObjId].ToList();
                if (_propertyTypes.TryGetValue(template.This2PropertyType, out var type))
                {
                    template.This2PropertyTypeNavigation = type;
                }
            }

            _interfaceTypes = context.InterfaceTypes.AsNoTracking().ToList().GroupBy(a => a.Type).ToDictionary(a => a.Key, a => a.First());
            _nodeTemplates = context.NodeTemplates.AsNoTracking().ToList().GroupBy(a => a.ObjId).ToDictionary(a => a.Key, a => a.First());
            foreach (var template in _nodeTemplates.Values)
            {
                if (_interfaceTypes.TryGetValue(template.NeedsInterface2InterfacesType, out var needs))
                {
                    template.NeedsInterface2InterfacesTypeNavigation = needs;
                }
                if (_interfaceTypes.TryGetValue(template.ProvidesInterface2InterfaceType, out var provides))
                {
                    template.ProvidesInterface2InterfaceTypeNavigation = provides;
                }
            }

            _slaves = context.Slaves.AsNoTracking().ToList().GroupBy(a => a.ObjId).ToDictionary(a => a.Key, a => a.First());
            _areas = context.AreaInstances.AsNoTracking().ToList().GroupBy(a => a.ObjId).ToDictionary(a => a.Key, a => a.First());
            _categories = context.CategoryInstances.AsNoTracking().ToList().GroupBy(a => a.ObjId).ToDictionary(a => a.Key, a => a.First());
        }

        public static NodeInstanceLookup Load(AutomaticaContext context)
        {
            return new NodeInstanceLookup(context);
        }

        /// <summary>
        /// Same result as the per node queries: a missing template or interface type throws (the cache load fails), a missing slave, area or category is null
        /// </summary>
        public void Fill(NodeInstance item)
        {
            var properties = _properties[item.ObjId].ToList();
            foreach (var property in properties)
            {
                var propertyTemplate = Required(_propertyTemplates, property.This2PropertyTemplate, "property template");
                if (propertyTemplate.This2PropertyTypeNavigation == null)
                {
                    throw new InvalidOperationException($"Property type {propertyTemplate.This2PropertyType} of property template {propertyTemplate.ObjId} not found");
                }
                property.This2PropertyTemplateNavigation = propertyTemplate;
            }
            item.PropertyInstance = properties;

            var nodeTemplate = item.This2NodeTemplate.HasValue
                ? Required(_nodeTemplates, item.This2NodeTemplate.Value, "node template")
                : throw new InvalidOperationException($"Node instance {item.ObjId} has no node template");
            if (nodeTemplate.NeedsInterface2InterfacesTypeNavigation == null || nodeTemplate.ProvidesInterface2InterfaceTypeNavigation == null)
            {
                throw new InvalidOperationException($"Interface type of node template {nodeTemplate.ObjId} not found");
            }
            item.This2NodeTemplateNavigation = nodeTemplate;

            if (item.This2Slave.HasValue)
            {
                item.This2SlaveNavigation = _slaves.GetValueOrDefault(item.This2Slave.Value);
            }

            if (item.This2AreaInstance.HasValue)
            {
                item.This2AreaInstanceNavigation = _areas.GetValueOrDefault(item.This2AreaInstance.Value);
            }

            if (item.This2CategoryInstance.HasValue)
            {
                item.This2CategoryInstanceNavigation = _categories.GetValueOrDefault(item.This2CategoryInstance.Value);
            }
        }

        private static T Required<TKey, T>(Dictionary<TKey, T> items, TKey key, string what) where TKey : notnull
        {
            if (!items.TryGetValue(key, out var item))
            {
                throw new InvalidOperationException($"The {what} {key} was not found");
            }
            return item;
        }
    }
}
