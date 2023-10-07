using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;

namespace Automatica.Core.Internals.Templates
{
    internal class NodeInstanceService : INodeInstanceService
    {
        private readonly INodeTemplateCache _nodeTemplateCache;
        private readonly ILocalizationProvider _localizationProvider;

        public NodeInstanceService(INodeTemplateCache nodeTemplateCache, ILocalizationProvider localizationProvider)
        {
            _nodeTemplateCache = nodeTemplateCache;
            _localizationProvider = localizationProvider;
        }
        public NodeInstance CreateNodeInstance(string locale, NodeInstance parent, NodeTemplate nodeTemplate)
        {
            var node = NodeInstanceFactory.CreateNodeInstanceFromTemplate(nodeTemplate);

            node.Name = _localizationProvider.GetTranslation(locale, nodeTemplate.Name);
            node.Description = _localizationProvider.GetTranslation(locale, nodeTemplate.Description);
            node.This2ParentNodeInstance = parent?.ObjId;
            node.This2ParentNodeInstanceNavigation = parent;


            var childTemplates = _nodeTemplateCache.GetDefaultItemsForTemplate(nodeTemplate);

            foreach (var child in childTemplates)
            {
                var childInstance = CreateNodeInstance(locale, node, child);

            
                node.InverseThis2ParentNodeInstanceNavigation.Add(childInstance);

            }

            return node;
        }

        public NodeInstance CreateNodeInstance(string locale, NodeTemplate nodeTemplate)
        {
            return CreateNodeInstance(locale, null, nodeTemplate);
        }

        public NodeInstance CreateNodeInstance(string locale, NodeInstance parent, Guid nodeTemplate)
        {

            return CreateNodeInstance(locale, parent, _nodeTemplateCache.Get(nodeTemplate));
        }

        public NodeInstance CreateNodeInstance(string locale, Guid nodeTemplate)
        {
            return CreateNodeInstance(locale, _nodeTemplateCache.Get(nodeTemplate));
        }

        public NodeTemplate GetTemplateById(Guid id)
        {
            return _nodeTemplateCache.Get(id);
        }

        public ICollection<NodeTemplate> GetTemplatesById(params Guid[] ids)
        {
            return _nodeTemplateCache.Get(ids);
        }

        public NodeTemplate GetTemplateByKey(string key)
        {
            return _nodeTemplateCache.GetByKey(key);
        }
    }
}
