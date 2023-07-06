using System;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Templates
{
    public class LogicTemplateFactory : PropertyTemplateFactory, ILogicTemplateFactory
    {
        public LogicTemplateFactory(AutomaticaContext database, IConfiguration config, ILogicFactory factory) : base(database, config, (template, guid) => throw new NotImplementedException(), factory)
        {
            
        }

        public CreateTemplateCode CreateLogicTemplate(Guid ui, string name, string description, string key, string group,
            double height, double width)
        {
            var interfaceType = Db.RuleTemplates.SingleOrDefault(a => a.ObjId == ui);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (interfaceType == null)
            {
                interfaceType = new RuleTemplate();
                interfaceType.ObjId = ui;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            interfaceType.Name = name;
            interfaceType.Description = description;
            interfaceType.Key = key;
            interfaceType.Group = group;
            interfaceType.Height = (float)height;
            interfaceType.Width = (float)width;
            interfaceType.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label);

            if (isNewObject)
            {
                Db.RuleTemplates.Add(interfaceType);
                Db.SaveChanges();
            }
            else
            {
                Db.RuleTemplates.Update(interfaceType);
            }

            return retValue;
        }

        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
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
                type);
        }


        public CreateTemplateCode CreateLogicInterfaceTemplate(Guid id, string name, string description, string key,
            Guid ruleTemplate,
            LogicInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type)
        {
            if (direction == LogicInterfaceDirection.Param)
            {
                throw new ArgumentException(
                    $"Please use {nameof(CreateParameterLogicInterfaceTemplate)} for creating parameter interface templates");
            }

            var interfaceType = Db.RuleInterfaceTemplates.SingleOrDefault(a => a.ObjId == id);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (interfaceType == null)
            {
                interfaceType = new RuleInterfaceTemplate();
                interfaceType.ObjId = id;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            interfaceType.Name = name;
            interfaceType.Key = key;
            interfaceType.Description = description;
            interfaceType.This2RuleTemplate = ruleTemplate;
            interfaceType.This2RuleInterfaceDirection = (long)direction;
            interfaceType.MaxLinks = maxLinks;
            interfaceType.SortOrder = sortOrder;
            interfaceType.ParameterDataType = RuleInterfaceParameterDataType.NoParameter;
            interfaceType.InterfaceType = type;

            if (isNewObject)
            {
                Db.RuleInterfaceTemplates.Add(interfaceType);
                Db.SaveChanges(true);
            }
            else
            {
                Db.RuleInterfaceTemplates.Update(interfaceType);
            }

            return retValue;
        }


        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue)
        {
            return CreateParameterLogicInterfaceTemplate(id, name, description, ruleTemplate, sortOrder, dataType,
                defaultValue, false);
        }

        public CreateTemplateCode CreateParameterLogicInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue, bool linkable)
        {
            var interfaceType = Db.RuleInterfaceTemplates.SingleOrDefault(a => a.ObjId == id);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (interfaceType == null)
            {
                interfaceType = new RuleInterfaceTemplate();
                interfaceType.ObjId = id;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            interfaceType.Name = name;

            interfaceType.Key = name;
            interfaceType.Description = description;
            interfaceType.This2RuleTemplate = ruleTemplate;
            interfaceType.This2RuleInterfaceDirection = (long)LogicInterfaceDirection.Param;
            interfaceType.MaxLinks = 0;
            interfaceType.SortOrder = sortOrder;
            interfaceType.ParameterDataType = dataType;
            interfaceType.IsLinkableParameter = linkable;
            interfaceType.DefaultValue = Convert.ToString(defaultValue, CultureInfo.InvariantCulture);

            if (isNewObject)
            {
                Db.RuleInterfaceTemplates.Add(interfaceType);
                Db.SaveChanges(true);
            }
            else
            {
                Db.RuleInterfaceTemplates.Update(interfaceType);
            }

            return retValue;
        }

        public CreateTemplateCode ChangeDefaultVisuTemplate(Guid uid, VisuMobileObjectTemplateTypes template)
        {
            var ruleTemplate = Db.RuleTemplates.SingleOrDefault(p => p.ObjId == uid);
            if (ruleTemplate == null)
            {
                return CreateTemplateCode.Error;
            }

            ruleTemplate.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(template);

            Db.SaveChanges(true);
            return CreateTemplateCode.Updated;
        }

        public RuleTemplate GetById(Guid id)
        {
            return Db.RuleTemplates.SingleOrDefault(a => a.ObjId == id);
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
