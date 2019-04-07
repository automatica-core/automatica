using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Docker;
using Automatica.Core.Internals.Mqtt;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace Automatica.Core.Plugin.Dockerize.Factories
{

    internal class RemoteNodeTemplatesFactory : INodeTemplateFactory, IRemoteFactory
    {
        private Dictionary<string, Setting> _settings = new Dictionary<string, Setting>();

        private Dictionary<Guid, NodeTemplate> _nodeTemplates = new Dictionary<Guid, NodeTemplate>();
        private Dictionary<Guid, PropertyTemplate> _propertyTemplates = new Dictionary<Guid, PropertyTemplate>();
        private Dictionary<Guid, PropertyTemplateConstraint> _propertyConstraintTemplates = new Dictionary<Guid, PropertyTemplateConstraint>();
        private Dictionary<Guid, PropertyTemplateConstraintData> _propertyConstraintDataTemplates = new Dictionary<Guid, PropertyTemplateConstraintData>();
        private Dictionary<Guid, InterfaceType> _interfaceTypes = new Dictionary<Guid, InterfaceType>();

        public RemoteNodeTemplatesFactory()
        {

        }

        public async Task SubmitFactoryData(Guid factoryGuid, IMqttClient client)
        {
            var remoteDto = new RemoteNodeTemplatesFactoryDto()
            {
                InterfaceTypes = _interfaceTypes.Values,
                NodeTemplates = _nodeTemplates.Values,
                PropertyTemplateConstraintData = _propertyConstraintDataTemplates.Values,
                PropertyTemplateConstraints = _propertyConstraintTemplates.Values,
                PropertyTemplates = _propertyTemplates.Values,
                Settings = _settings.Values
            };

            var json = JsonConvert.SerializeObject(remoteDto);

            await client.PublishAsync(new MQTTnet.MqttApplicationMessage()
            {
                Payload = Encoding.UTF8.GetBytes(json),
                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                Topic = $"{MqttTopicConstants.NODETEMPLATES_TOPIC}/{factoryGuid}"
            });
        }

        public void AddSettingsEntry(string key, object value, string group, PropertyTemplateType type, bool isVisible)
        {
           var settings = new Setting
                {
                    ValueKey = key,
                    Value = value
                };
             
            settings.Type = (long)type;
            settings.IsVisible = isVisible;
            settings.Group = group;

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
            var interfaceType = new InterfaceType();
            interfaceType.Type = uid;

            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.MaxChilds = maxChilds;
            interfaceType.MaxInstances = maxInstances;
            interfaceType.IsDriverInterface = isDriverInterface;

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
            var nodeTemplate = new NodeTemplate();
            nodeTemplate.ObjId = uid;

            nodeTemplate.Name = name;
            nodeTemplate.Description = description;
            nodeTemplate.Key = key;
            nodeTemplate.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label);
            nodeTemplate.NeedsInterface2InterfacesType = needsInterface;
            nodeTemplate.ProvidesInterface2InterfaceType = providesInterface;
            nodeTemplate.DefaultCreated = defaultCreated;
            nodeTemplate.IsReadable = isReadable;
            nodeTemplate.IsReadableFixed = isReadableFixed;
            nodeTemplate.IsWriteable = isWriteable;
            nodeTemplate.IsWriteableFixed = isWriteableFixed;
            nodeTemplate.This2NodeDataType = (long)dataType;
            nodeTemplate.MaxInstances = maxInstances;
            nodeTemplate.IsAdapterInterface = isAdapterInterface;
            nodeTemplate.IsDeleteable = deleteAble;


            _nodeTemplates.Add(uid, nodeTemplate);
            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption, PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            var constraint = new PropertyTemplateConstraint();
            constraint.ObjId = constraintId;

            constraint.Name = name;
            constraint.Description = descrption;
            constraint.ConstraintType = (long)constraintType;
            constraint.This2PropertyTemplate = propertyTemplate;
            constraint.ConstraintLevel = (long)level;


            _propertyConstraintTemplates.Add(constraintId, constraint);

            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset, Guid propertyTemplateConstraint,
            string propertyKey, PropertyConstraintConditionType conditionType)
        {
            var constraint = new PropertyTemplateConstraintData();
            constraint.ObjId = constraintData;

            constraint.Factor = factor;
            constraint.Offset = offset;
            constraint.This2PropertyTemplateConstraint = propertyTemplateConstraint;
            constraint.PropertyKey = propertyKey;
            constraint.ConditionType = (long)conditionType;

            _propertyConstraintDataTemplates.Add(constraintData, constraint);

            return CreateTemplateCode.Created;
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key, PropertyTemplateType propertyType, Guid objectRef, 
            string group, bool isVisible, bool isReadonly, string meta, object defaultValue, int groupOrder, int order)
        {
            var propertyTemplate = new PropertyTemplate();
            propertyTemplate.ObjId = uid;

            propertyTemplate.Name = name;
            propertyTemplate.Description = description;
            propertyTemplate.Key = key;
            propertyTemplate.This2PropertyType = (long)propertyType;

            propertyTemplate.This2NodeTemplate = objectRef;

            propertyTemplate.Group = group;
            propertyTemplate.IsVisible = isVisible;
            propertyTemplate.IsReadonly = isReadonly;
            propertyTemplate.Meta = String.IsNullOrEmpty(meta) ? "" : meta;
            propertyTemplate.DefaultValue = defaultValue == null ? "" : defaultValue.ToString();
            propertyTemplate.GroupOrder = groupOrder;
            propertyTemplate.Order = order;

            _propertyTemplates.Add(uid, propertyTemplate);

            return CreateTemplateCode.Created;
        }

        public Setting GetSetting(string key)
        {
            return _settings[key];
        }

        public CreateTemplateCode SetPropertyValue(Guid uid, object value)
        {
            throw new NotImplementedException();
        }
    }
}
