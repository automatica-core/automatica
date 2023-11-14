using System;
using System.Collections.Generic;
using System.Globalization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;

namespace Automatica.Core.UnitTests.Base.Logics
{
    public class LogicTemplateFactoryMock : ILogicTemplateFactory
    {
        private readonly Dictionary<Guid, RuleTemplate> _ruleTemplates = new Dictionary<Guid, RuleTemplate>();
        private readonly Dictionary<Guid, RuleInterfaceTemplate> _ruleInterfaceTemplates = new Dictionary<Guid, RuleInterfaceTemplate>();

        public RuleInstance CreateRuleInstanceFromTemplate(RuleTemplate template)
        {
            var rule = new RuleInstance();

            foreach (var i in template.RuleInterfaceTemplate)
            {
                var intInst = new RuleInterfaceInstance();

                intInst.This2RuleInstanceNavigation = rule;
                intInst.This2RuleInterfaceTemplateNavigation = i;
                intInst.This2RuleInterfaceTemplate = i.ObjId;
                intInst.Value = i.DefaultValue;
                intInst.ObjId = i.ObjId;

                rule.RuleInterfaceInstance.Add(intInst);
            }

            rule.ObjId = template.ObjId;
            rule.Name = template.Name;
            rule.This2RuleTemplate = template.ObjId;
            rule.This2RuleTemplateNavigation = template;


            return rule;
        }

        public RuleInstance CreateRuleInstanceFromTemplate(Guid templateGuid)
        {
            return CreateRuleInstanceFromTemplate(_ruleTemplates[templateGuid]);
        }

        public CreateTemplateCode CreateLogicTemplate(Guid ui, string name, string description, string key, string group,
            double height, double width)
        {
            var interfaceType = new RuleTemplate();

            if (!_ruleTemplates.ContainsKey(ui))
            {
                _ruleTemplates.Add(ui, interfaceType);
            }

            var retValue = CreateTemplateCode.Created;

            interfaceType.ObjId = ui;
            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.Key = key;
            interfaceType.Group = group;
            interfaceType.Height = (float)height;
            interfaceType.Width = (float)width;


            _ruleTemplates[ui] = interfaceType;
            return retValue;
        }

        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description,
            Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder)
        {
            return CreateLogicInterfaceTemplate(id, name, description, ruleTemplate, direction, maxLinks, sortOrder,
                RuleInterfaceType.Unknown);
        }

        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder)
        {
            return CreateLogicInterfaceTemplate(id, name, description, key, ruleTemplate, direction, maxLinks, sortOrder,
                RuleInterfaceType.Unknown);
        }

        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type)
        {
            return CreateLogicInterfaceTemplate(id, name, description, name, ruleTemplate, direction, maxLinks, sortOrder,
                RuleInterfaceType.Unknown);
        }

        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, string key, Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type)
        {
            var interfaceType = new RuleInterfaceTemplate();

            if (!_ruleInterfaceTemplates.ContainsKey(id))
            {
                _ruleInterfaceTemplates.Add(id, interfaceType);
                _ruleTemplates[ruleTemplate].RuleInterfaceTemplate.Add(interfaceType);
            }

            interfaceType.ObjId = id;
            var retValue = CreateTemplateCode.Created;


            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.Key = key;
            interfaceType.This2RuleTemplate = ruleTemplate;
            interfaceType.This2RuleInterfaceDirection = (long)direction;
            interfaceType.MaxLinks = maxLinks;
            interfaceType.SortOrder = sortOrder;
            interfaceType.InterfaceType = type;

            _ruleInterfaceTemplates[id] = interfaceType;
            return retValue;
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid ui, string name, string description, Guid ruleTemplate,
             int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue)
        {
            return CreateParameterLogicInterfaceTemplate(ui, name, description, ruleTemplate, sortOrder, dataType,
                defaultValue, false);
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description,
            string key,
            Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue,
            bool linkable)
        {
            return CreateParameterLogicInterfaceTemplate(id, name, description, key, ruleTemplate, sortOrder, dataType,
                defaultValue, linkable, null);
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, string key,
            Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable, string meta)
        {
            var interfaceType = new RuleInterfaceTemplate();

            if (!_ruleInterfaceTemplates.ContainsKey(id))
            {
                _ruleInterfaceTemplates.Add(id, interfaceType);
                _ruleTemplates[ruleTemplate].RuleInterfaceTemplate.Add(interfaceType);
            }

            interfaceType.ObjId = id;
            var retValue = CreateTemplateCode.Created;

            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.Key = key;
            interfaceType.This2RuleTemplate = ruleTemplate;
            interfaceType.This2RuleInterfaceDirection = (long)LogicInterfaceDirection.Param;
            interfaceType.MaxLinks = 0;
            interfaceType.SortOrder = sortOrder;
            interfaceType.ParameterDataType = dataType;
            interfaceType.DefaultValue = Convert.ToString(defaultValue, CultureInfo.InvariantCulture);
            interfaceType.Meta = meta;

            _ruleInterfaceTemplates[id] = interfaceType;
            return retValue;
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, string key,
            Guid ruleTemplate, int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue)
        {
            return CreateParameterLogicInterfaceTemplate(id, name, description, key, ruleTemplate, sortOrder, dataType,
                defaultValue, false);
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable)
        {
            return CreateParameterLogicInterfaceTemplate(id, name, description, name, ruleTemplate, sortOrder, dataType,
                defaultValue, linkable);
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid objectRef, string groupd, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, Guid propertyTemplate)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset,
            Guid propertyTemplateConstraint, string propertyKey, PropertyConstraintConditionType conditionType)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid nodeTemplate, string groupd, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order, PropertyConstraint constraint,
            PropertyConstraintLevel constraintLevel, string constraintMeta="")
        {
            throw new NotImplementedException();
        }

        public void AddSettingsEntry(string key, object value, string group, PropertyTemplateType type, bool isVisible)
        {
            throw new NotImplementedException();
        }

        public Setting GetSetting(string key)
        {
            throw new NotImplementedException();
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            return CreateTemplateCode.Updated;
        }

        public RuleTemplate GetById(Guid id)
        {
            return _ruleTemplates[id];
        }

        public RuleInstance CreateLogicInstance(Guid templateId)
        {
            return CreateLogicInstance(GetById(templateId));
        }

        public RuleInstance CreateLogicInstance(RuleTemplate template)
        {
            return RuleInstance.CreateFromTemplate(template);
        }
    }
}
