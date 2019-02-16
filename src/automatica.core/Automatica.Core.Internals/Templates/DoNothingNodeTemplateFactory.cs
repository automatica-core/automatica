using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace Automatica.Core.Internals.Templates
{
    public class DoNothingNodeTemplateFactory : INodeTemplateFactory
    {
        public void AddSettingsEntry(string key, object value, string @group, PropertyTemplateType type, bool isVisible)
        {
            
        }

        public Setting GetSetting(string key)
        {
            return null;
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid objectRef, string @group, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset,
            Guid propertyTemplateConstraint, string propertyKey, PropertyConstraintConditionType conditionType)
        {
            return CreateTemplateCode.Updated;
        }

        public NodeTemplate GetNodeTemplateById(Guid id)
        {
            return null;
        }

        public NodeTemplate GetNodeTemplateByKey(string key)
        {
            return null;
        }

        public ICollection<NodeTemplate> GetNodeTemplates(params Guid[] key)
        {
            return new List<NodeTemplate>();
        }

        public NodeInstance CreateNodeInstance(NodeTemplate template)
        {
            return null;
        }

        public NodeInstance CreateNodeInstance(Guid template)
        {
            return null;
        }

        public NodeInstance CreateNodeInstanceByKey(string key)
        {
            return null;
        }

        public CreateTemplateCode CreateInterfaceType(Guid uid, string name, string description, int maxChilds, int maxInstances,
            bool isDriverInterface)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key, Guid needsInterface,
            Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
            bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode CreateNodeTemplate(Guid uid, string name, string description, string key, Guid needsInterface,
            Guid providesInterface, bool defaultCreated, bool isReadable, bool isReadableFixed, bool isWriteable,
            bool isWriteableFixed, NodeDataType dataType, int maxInstances, bool isAdapterInterface, bool deleteAble)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode ChangeNodeTemplateMetaName(Guid uid, string metaName)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            return CreateTemplateCode.Updated;
        }

        public CreateTemplateCode SetPropertyValue(Guid uid, object value)
        {
            return CreateTemplateCode.Error;
        }
    }
}
