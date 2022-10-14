using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Plugin.Standalone.Factories
{

    internal class RemoteNodeTemplatesFactory : INodeTemplateFactory
    {
        private readonly Dictionary<string, Setting> _settings = new Dictionary<string, Setting>();

        private readonly Dictionary<Guid, NodeTemplate> _nodeTemplates = new Dictionary<Guid, NodeTemplate>();
        private readonly Dictionary<Guid, PropertyTemplate> _propertyTemplates = new Dictionary<Guid, PropertyTemplate>();
        private readonly Dictionary<Guid, PropertyTemplateConstraint> _propertyConstraintTemplates = new Dictionary<Guid, PropertyTemplateConstraint>();
        private readonly Dictionary<Guid, PropertyTemplateConstraintData> _propertyConstraintDataTemplates = new Dictionary<Guid, PropertyTemplateConstraintData>();
        private readonly Dictionary<Guid, InterfaceType> _interfaceTypes = new Dictionary<Guid, InterfaceType>();

        public RemoteNodeTemplatesFactory()
        {

        }

        public void AddSettingsEntry(string key, object value, string group, PropertyTemplateType type, bool isVisible)
        {
            var settings = new Setting
            {
                ValueKey = key,
                Value = value,
                Type = (long) type,
                IsVisible = isVisible,
                Group = @group
            };


            _settings.Add(key, settings);
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            return CreateTemplateCode.Error;
        }

        public CreateTemplateCode ChangeNodeTemplateMetaName(Guid uid, string metaName)
        {
            if(!_nodeTemplates.ContainsKey(uid))
            {
                return CreateTemplateCode.Error;
            }
            _nodeTemplates[uid].NameMeta = metaName;

            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreateInterfaceType(Guid uid, string name, string description, int maxChilds, int maxInstances, bool isDriverInterface)
        {
            var interfaceType = new InterfaceType
            {
                Type = uid,
                Name = name,
                Description = description,
                MaxChilds = maxChilds,
                MaxInstances = maxInstances,
                IsDriverInterface = isDriverInterface
            };


            _interfaceTypes.Add(uid, interfaceType);

            return CreateTemplateCode.Created;
        }

        public NodeTemplate GetNodeTemplateById(Guid id)
        {
            return _nodeTemplates[id];
        }

        public NodeTemplate GetNodeTemplateByKey(string key)
        {
            return _nodeTemplates.Values.SingleOrDefault(a => a.Key == key);
        }

        public ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key)
        {
            return _nodeTemplates.Values.Where(a => key.Contains(a.ObjId)).ToList();
         }

        public NodeInstance CreateNodeInstance(string locale, NodeTemplate template)
        {
            return CreateNodeInstance(template);
        }

        public NodeInstance CreateNodeInstanceByKey(string key)
        {
            return CreateNodeInstance(GetNodeTemplateByKey(key));
        }

        public NodeInstance CreateNodeInstance(NodeTemplate template)
        {
            return NodeInstanceFactory.CreateNodeInstanceFromTemplate(template);
        }

        public NodeInstance CreateNodeInstance(Guid template)
        {
            return CreateNodeInstance(GetNodeTemplateById(template));
        }

        public NodeInstance CreateNodeInstance(string locale, Guid template)
        {
            return CreateNodeInstance(GetNodeTemplateById(template));
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key, Guid needsInterface, Guid providesInterface, 
            bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable, bool isWriteableFixed, Base.Templates.NodeDataType dataType, 
            int maxInstances, bool isAdapterInterface)
        {
            return CreateNodeTemplate(uid, name, description, key, needsInterface, providesInterface, defaultCreated,
               isReadable, isReadableFixed, isWriteable, isWriteableFixed, dataType, maxInstances, isAdapterInterface,
               true);
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key, Guid needsInterface, Guid providesInterface,
            bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable, bool isWriteableFixed, Base.Templates.NodeDataType dataType,
            int maxInstances, bool isAdapterInterface, bool deleteAble)
        {
            var nodeTemplate = new NodeTemplate
            {
                ObjId = uid,
                Name = name,
                Description = description,
                Key = key,
                This2DefaultMobileVisuTemplate =
                    VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label),
                NeedsInterface2InterfacesType = needsInterface,
                ProvidesInterface2InterfaceType = providesInterface,
                DefaultCreated = defaultCreated,
                IsReadable = isReadable,
                IsReadableFixed = isReadableFixed,
                IsWriteable = isWriteable,
                IsWriteableFixed = isWriteableFixed,
                This2NodeDataType = (long) dataType,
                MaxInstances = maxInstances,
                IsAdapterInterface = isAdapterInterface,
                IsDeleteable = deleteAble
            };



            _nodeTemplates.Add(uid, nodeTemplate);
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption, PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            var constraint = new PropertyTemplateConstraint
            {
                ObjId = constraintId,
                Name = name,
                Description = descrption,
                ConstraintType = (long) constraintType,
                This2PropertyTemplate = propertyTemplate,
                ConstraintLevel = (long) level
            };



            _propertyConstraintTemplates.Add(constraintId, constraint);

            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset, Guid propertyTemplateConstraint,
            string propertyKey, PropertyConstraintConditionType conditionType)
        {
            var constraint = new PropertyTemplateConstraintData
            {
                ObjId = constraintData,
                Factor = factor,
                Offset = offset,
                This2PropertyTemplateConstraint = propertyTemplateConstraint,
                PropertyKey = propertyKey,
                ConditionType = (long) conditionType
            };


            _propertyConstraintDataTemplates.Add(constraintData, constraint);

            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key, PropertyTemplateType propertyType, Guid objectRef, 
            string group, bool isVisible, bool isReadonly, string meta, object defaultValue, int groupOrder, int order)
        {
            var propertyTemplate = new PropertyTemplate
            {
                ObjId = uid,
                Name = name,
                Description = description,
                Key = key,
                This2PropertyType = (long) propertyType,
                This2NodeTemplate = objectRef,
                Group = @group,
                IsVisible = isVisible,
                IsReadonly = isReadonly,
                Meta = String.IsNullOrEmpty(meta) ? "" : meta,
                DefaultValue = defaultValue == null ? "" : defaultValue.ToString(),
                GroupOrder = groupOrder,
                Order = order
            };




            _propertyTemplates.Add(uid, propertyTemplate);

            return CreateTemplateCode.Created;
        }

        public Setting GetSetting(string key)
        {
            return _settings[key];
        }

        public CreateTemplateCode SetPropertyValue(Guid uid, object value)
        {
            return CreateTemplateCode.Created;
        }
    }
}
