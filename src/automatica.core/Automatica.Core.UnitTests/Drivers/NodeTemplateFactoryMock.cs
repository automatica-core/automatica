using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace Automatica.Core.UnitTests.Drivers
{
    public class NodeTemplateFactoryMock : INodeTemplateFactory
    {

        private readonly Dictionary<Guid, NodeTemplate> _nodeTemplates = new Dictionary<Guid, NodeTemplate>();
        private readonly Dictionary<Guid, PropertyTemplate> _propertyTemplates = new Dictionary<Guid, PropertyTemplate>();
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();



        public void AddSettingsEntry(string key, object value, string @group, PropertyTemplateType type, bool isVisible)
        {
            // not needed for unit tests
        }

        public void SetSettings(string key, object value)
        {
            if (!_settings.ContainsKey(key))
            {
                _settings.Add(key, value);
            }
            else
            {
                _settings[key] = value;
            }
        }

        public Setting GetSetting(string key)
        {
            if (_settings.ContainsKey(key))
            {
                var set = new Setting();
                var value = _settings[key];

                set.Value = value;
                if (value is double dblValue)
                {
                    set.ValueDouble = dblValue;
                }
                return set;
            }
            return new Setting();
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid objectRef, string groupd, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order)
        {
            var propertyTemplate = new PropertyTemplate();

            propertyTemplate.ObjId = uid;
            propertyTemplate.Name = name;
            propertyTemplate.Description = description;
            propertyTemplate.Key = key;
            propertyTemplate.This2PropertyType = (long)propertyType;

            propertyTemplate.This2NodeTemplate = objectRef;

            propertyTemplate.Group = groupd;
            propertyTemplate.IsVisible = isVisible;
            propertyTemplate.IsReadonly = isReadonly;
            propertyTemplate.Meta = String.IsNullOrEmpty(meta) ? "" : meta;
            propertyTemplate.DefaultValue = defaultValue == null ? "" : defaultValue.ToString();
            propertyTemplate.GroupOrder = groupOrder;
            propertyTemplate.Order = order;

            propertyTemplate.This2PropertyTypeNavigation = new PropertyType();
            propertyTemplate.This2PropertyTypeNavigation.Type = (long)propertyType;

            if(!_propertyTemplates.ContainsKey(uid)) 
                _propertyTemplates.Add(uid, propertyTemplate);

            _nodeTemplates[objectRef].PropertyTemplate.Add(propertyTemplate);

            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset,
            Guid propertyTemplateConstraint, string propertyKey, PropertyConstraintConditionType conditionType)
        {
            return CreateTemplateCode.Created;
        }

        public NodeTemplate GetNodeTemplateById(Guid id)
        {
            return _nodeTemplates[id];
        }

        public ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key)
        {
            return _nodeTemplates.Values.Where(a => key.Contains(a.ObjId)).ToList();
        }

        public NodeInstance CreateNodeInstance(NodeTemplate template)
        {
            return NodeInstanceFactory.CreateNodeInstanceFromTemplate(template);
        }

        public NodeInstance CreateNodeInstance(Guid template)
        {
            return CreateNodeInstance(GetNodeTemplateById(template));
        }

        public NodeTemplate GetNodeTemplateByKey(string key)
        {
            foreach(var t in _nodeTemplates)
            {
                if(t.Value.Key == key)
                {
                    return t.Value;
                }
            }
            return null;
        }

        public NodeInstance CreateNodeInstanceByKey(string key)
        {
            return CreateNodeInstance(GetNodeTemplateByKey(key));
        }

        public CreateTemplateCode CreateBoard(Guid uid, string name, string description)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreateBoardInterface(Guid uid, string name, string description, Guid boardGuid, string meta,
            Guid intefaceType)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreateBoardInterfaceForAllBoards(string name, string description, string meta, Guid intefaceType)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreateInterfaceType(Guid uid, string name, string description, int maxChilds, int maxInstances,
            bool isDriverInterface)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key, Guid needsInterface,
            Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
            bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface)
        {
            return CreateNodeTemplate(uid, name, description, key, needsInterface, providesInterface, defaultCreated,
                isReadable, isReadableFixed, isWriteable, isWriteableFixed, dataType, maxInstances, isAdapterInterface,
                true);
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key,
            Guid needsInterface,
            Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
            bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface, bool deleteAble)
        {
            var nodeTemplate = new NodeTemplate { ObjId = uid };

            nodeTemplate.Name = name;
            nodeTemplate.Description = description;
            nodeTemplate.Key = key;
            nodeTemplate.NeedsInterface2InterfacesType = needsInterface;
            nodeTemplate.ProvidesInterface2InterfaceType = providesInterface;
            nodeTemplate.DefaultCreated = defaultCreated;
            nodeTemplate.IsReadable = isReadable;
            nodeTemplate.IsReadableFixed = isReadableFixed;
            nodeTemplate.IsWriteable = isWriteable;
            nodeTemplate.IsWriteableFixed = isWriteableFixed;
            nodeTemplate.This2NodeDataType = (long) dataType;
            nodeTemplate.MaxInstances = maxInstances;
            nodeTemplate.IsAdapterInterface = isAdapterInterface;
            nodeTemplate.IsDeleteable = deleteAble;

            if(!_nodeTemplates.ContainsKey(uid))
                _nodeTemplates.Add(uid, nodeTemplate);

            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode ChangeNodeTemplateMetaName(Guid uid, string metaName)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode SetPropertyValue(Guid uid, object value)
        {
            return CreateTemplateCode.Updated;
        }
    }
}
