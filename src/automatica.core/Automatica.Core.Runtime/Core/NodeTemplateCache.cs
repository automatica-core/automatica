using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Localization;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Abstraction.Plugins.Driver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.Core
{
    internal class NodeTemplateCache : AbstractCache<NodeTemplate>, INodeTemplateCache
    {
        private readonly ILocalizationProvider _localizationProvider;
        private readonly IDriverFactoryStore _driverFactoryStore;

        private readonly IDictionary<Guid, IList<NodeTemplate>> _neededTemplateKeyDictionary =
            new Dictionary<Guid, IList<NodeTemplate>>();
        
        private readonly IDictionary<Guid, NodeTemplate> _templatesKeyDictionary =
            new Dictionary<Guid, NodeTemplate>();

        private readonly IDictionary<Guid, IList<NodeTemplate>> _defaultTemplateItemsKeyDictionary =
            new Dictionary<Guid, IList<NodeTemplate>>();

        private readonly IDictionary<NodeTemplate, IDriverFactory> _nodeTemplateFactoryMapping =
            new Dictionary<NodeTemplate, IDriverFactory>();

        public NodeTemplateCache(IConfiguration configuration, ILocalizationProvider localizationProvider, IDriverFactoryStore driverFactoryStore) : base(configuration)
        {
            _localizationProvider = localizationProvider;
            _driverFactoryStore = driverFactoryStore;
        }

        protected override IQueryable<NodeTemplate> GetAll(AutomaticaContext context)
        {
            var x = context.NodeTemplates.AsNoTracking()
                .Include(a => a.This2NodeDataTypeNavigation)
                .Include(a => a.NeedsInterface2InterfacesTypeNavigation)
                .Include(a => a.ProvidesInterface2InterfaceTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.This2PropertyTypeNavigation)
                .Include(a => a.PropertyTemplate).ThenInclude(b => b.Constraints).ThenInclude(c => c.ConstraintData);

            foreach (var nodeTemplate in x)
            {
                if (!_templatesKeyDictionary.ContainsKey(nodeTemplate.ObjId))
                {
                    _templatesKeyDictionary.Add(nodeTemplate.ObjId, nodeTemplate);
                }
            }
            return x;
        }

        public override void Add(Guid key, NodeTemplate value)
        {
            if (!_neededTemplateKeyDictionary.ContainsKey(value.NeedsInterface2InterfacesType))
            {
                _neededTemplateKeyDictionary.Add(value.NeedsInterface2InterfacesType, new List<NodeTemplate>());
            }

            _neededTemplateKeyDictionary[value.NeedsInterface2InterfacesType].Add(value);


            if (!_defaultTemplateItemsKeyDictionary.ContainsKey(value.NeedsInterface2InterfacesType))
            {
                _defaultTemplateItemsKeyDictionary.Add(value.NeedsInterface2InterfacesType, new List<NodeTemplate>());
            }

            if (value.DefaultCreated)
            {
                _defaultTemplateItemsKeyDictionary[value.NeedsInterface2InterfacesType].Add(value);
            }

            var factory = _driverFactoryStore.Get(value.FactoryReference);

            if (factory != null)
            {
                _nodeTemplateFactoryMapping.Add(value, factory);
            }

            base.Add(key, value);
        }

        protected override Guid GetKey(NodeTemplate obj)
        {
            return obj.ObjId;
        }

        public void InitCache()
        {
            Initialize();
        }

        public NodeTemplate GetByKey(string key)
        {
            var id = Guid.Parse(key);
            return GetByKey(id);
        }

        public NodeTemplate GetByKey(Guid key)
        {
            if (_templatesKeyDictionary.Count == 0)
            {
                InitCache();
            }

            if(_templatesKeyDictionary.ContainsKey(key))
            {
                return _templatesKeyDictionary[key];
            }

            return null;
        }

        public ICollection<NodeTemplate> GetSupportedTemplates(NodeInstance targetNodeInstance, Guid neededInterfaceType)
        {
            Initialize();

            var toAdd = new List<NodeTemplate>();

            if (!_neededTemplateKeyDictionary.ContainsKey(neededInterfaceType))
            {
                return toAdd;
            }

            var templates = _neededTemplateKeyDictionary[neededInterfaceType];
            

            if (targetNodeInstance.InverseThis2ParentNodeInstanceNavigation.Count >= targetNodeInstance
                    .This2NodeTemplateNavigation.ProvidesInterface2InterfaceTypeNavigation.MaxChilds)
            {
                return toAdd;
            }

            foreach (var template in templates)
            {
                if (!(template.IsAdapterInterface.HasValue && template.IsAdapterInterface.Value) && !_nodeTemplateFactoryMapping.ContainsKey(template))
                {
                    continue;
                }

                var existingNodes = targetNodeInstance.InverseThis2ParentNodeInstanceNavigation.Count(a =>
                    a.This2NodeTemplate == template.ObjId);
                
                if (template.MaxInstances > 0 && existingNodes >= template.MaxInstances)
                {
                    continue;
                }

                if (template.ProvidesInterface2InterfaceTypeNavigation.MaxInstances > 0)
                {
                    existingNodes = targetNodeInstance.InverseThis2ParentNodeInstanceNavigation.Count(a =>
                        a.This2NodeTemplate == template.ObjId);

                    if (existingNodes >= template.MaxInstances)
                    {
                        continue;
                    }
                }

                toAdd.Add(template);
            }

            return toAdd.GroupBy(a => a.ObjId).Select(g => g.First()).ToList();
        }

        public ICollection<NodeTemplate> GetDefaultItemsForTemplate(NodeTemplate template)
        {
            Initialize();

            if (!_defaultTemplateItemsKeyDictionary.ContainsKey(template.ProvidesInterface2InterfaceType))
            {
                return new List<NodeTemplate>();
            }

            return _defaultTemplateItemsKeyDictionary[template.ProvidesInterface2InterfaceType];
        }
    }
}
