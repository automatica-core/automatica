using System;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace Automatica.Core.Internals.Templates
{
    public class RuleTemplateFactory : PropertyTemplateFactory, IRuleTemplateFactory
    {
        public RuleTemplateFactory(AutomaticaContext database, IConfiguration config) : base(database, config, (template, guid) => throw new NotImplementedException())
        {
            
        }

        public CreateTemplateCode CreateRuleTemplate(Guid ui, string name, string description, string key, string group,
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

        public CreateTemplateCode CreateRuleInterfaceTemplate(Guid ui, string name, string description, Guid ruleTemplate,
            RuleInterfaceDirection direction, int maxLinks, int sortOrder)
        {
            return CreateRuleInterfaceTemplate(ui, name, description, ruleTemplate, direction, maxLinks, sortOrder,
                RuleInterfaceType.Unknown);
        }

        public CreateTemplateCode CreateRuleInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            RuleInterfaceDirection direction, int maxLinks, int sortOrder, RuleInterfaceType type)
        {
            if (direction == RuleInterfaceDirection.Param)
            {
                throw new ArgumentException(
                    $"Please use {nameof(CreateParameterRuleInterfaceTemplate)} for creating parameter interface templates");
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

        public CreateTemplateCode CreateParameterRuleInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
            int sortOrder, RuleInterfaceParameterDataType dataType, object defaultValue)
        {
            return CreateParameterRuleInterfaceTemplate(id, name, description, ruleTemplate, sortOrder, dataType,
                defaultValue, false);
        }

        public CreateTemplateCode CreateParameterRuleInterfaceTemplate(Guid id, string name, string description, Guid ruleTemplate,
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
            interfaceType.Description = description;
            interfaceType.This2RuleTemplate = ruleTemplate;
            interfaceType.This2RuleInterfaceDirection = (long)RuleInterfaceDirection.Param;
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

        public RuleInstance CreateRuleInstance(Guid templateId)
        {
            return CreateRuleInstance(GetById(templateId));
        }

        public RuleInstance CreateRuleInstance(RuleTemplate template)
        {
            return RuleInstance.CreateFromTemplate(template);
        }
    }
}
