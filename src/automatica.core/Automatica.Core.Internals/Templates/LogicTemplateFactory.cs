using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public IDictionary<Guid, RuleTemplate> LogicTemplates { get; }

        public LogicTemplateFactory(AutomaticaContext database, IConfiguration config, ILogicFactory factory) : base(database, config, (template, guid) => throw new NotImplementedException(), factory)
        {
            LogicTemplates = new ConcurrentDictionary<Guid, RuleTemplate>();
        }

        public CreateTemplateCode CreateLogicTemplate(Guid ui, string name, string description, string key, string group,
            double height, double width)
        {
            var logicTemplate = Db.RuleTemplates.SingleOrDefault(a => a.ObjId == ui);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (logicTemplate == null)
            {
                logicTemplate = new RuleTemplate
                {
                    ObjId = ui
                };
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            logicTemplate.Name = name;
            logicTemplate.Description = description;
            logicTemplate.Key = key;
            logicTemplate.Group = group;
            logicTemplate.Height = (float)height;
            logicTemplate.Width = (float)width;
            logicTemplate.This2DefaultMobileVisuTemplate = VisuMobileObjectTemplateTypeAttribute.GetFromEnum(VisuMobileObjectTemplateTypes.Label);

            if (isNewObject)
            {
                Db.RuleTemplates.Add(logicTemplate);
                Db.SaveChanges();
            }
            else
            {
                Db.RuleTemplates.Update(logicTemplate);
            }

            if (LogicTemplates.ContainsKey(logicTemplate.ObjId))
            {
                LogicTemplates[logicTemplate.ObjId] = logicTemplate;
            }
            else
            {
                LogicTemplates.Add(logicTemplate.ObjId, logicTemplate);
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

            var logicInterfaceTemplate = Db.RuleInterfaceTemplates.SingleOrDefault(a => a.ObjId == id);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (logicInterfaceTemplate == null)
            {
                logicInterfaceTemplate = new RuleInterfaceTemplate();
                logicInterfaceTemplate.ObjId = id;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            logicInterfaceTemplate.Name = name;
            logicInterfaceTemplate.Key = key;
            logicInterfaceTemplate.Description = description;
            logicInterfaceTemplate.This2RuleTemplate = ruleTemplate;
            logicInterfaceTemplate.This2RuleInterfaceDirection = (long)direction;
            logicInterfaceTemplate.MaxLinks = maxLinks;
            logicInterfaceTemplate.SortOrder = sortOrder;
            logicInterfaceTemplate.ParameterDataType = RuleInterfaceParameterDataType.NoParameter;
            logicInterfaceTemplate.InterfaceType = type;

            if (isNewObject)
            {
                Db.RuleInterfaceTemplates.Add(logicInterfaceTemplate);
                Db.SaveChanges(true);
            }
            else
            {
                Db.RuleInterfaceTemplates.Update(logicInterfaceTemplate);
            }

            if (LogicTemplates[ruleTemplate].RuleInterfaceTemplate.All(a => a.ObjId != logicInterfaceTemplate.ObjId))
            {
                LogicTemplates[ruleTemplate].RuleInterfaceTemplate.Add(logicInterfaceTemplate);
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
            var parameterLogicInterfaceTemplate = Db.RuleInterfaceTemplates.SingleOrDefault(a => a.ObjId == id);
            var retValue = CreateTemplateCode.Updated;
            bool isNewObject = false;
            if (parameterLogicInterfaceTemplate == null)
            {
                parameterLogicInterfaceTemplate = new RuleInterfaceTemplate();
                parameterLogicInterfaceTemplate.ObjId = id;
                isNewObject = true;
                retValue = CreateTemplateCode.Created;
            }

            parameterLogicInterfaceTemplate.Name = name;

            parameterLogicInterfaceTemplate.Key = name;
            parameterLogicInterfaceTemplate.Description = description;
            parameterLogicInterfaceTemplate.This2RuleTemplate = ruleTemplate;
            parameterLogicInterfaceTemplate.This2RuleInterfaceDirection = (long)LogicInterfaceDirection.Param;
            parameterLogicInterfaceTemplate.MaxLinks = 0;
            parameterLogicInterfaceTemplate.SortOrder = sortOrder;
            parameterLogicInterfaceTemplate.ParameterDataType = dataType;
            parameterLogicInterfaceTemplate.IsLinkableParameter = linkable;
            parameterLogicInterfaceTemplate.DefaultValue = Convert.ToString(defaultValue, CultureInfo.InvariantCulture);

            if (isNewObject)
            {
                Db.RuleInterfaceTemplates.Add(parameterLogicInterfaceTemplate);
                Db.SaveChanges(true);
            }
            else
            {
                Db.RuleInterfaceTemplates.Update(parameterLogicInterfaceTemplate);
            }
            if (LogicTemplates[ruleTemplate].RuleInterfaceTemplate.All(a => a.ObjId != parameterLogicInterfaceTemplate.ObjId))
            {
                LogicTemplates[ruleTemplate].RuleInterfaceTemplate.Add(parameterLogicInterfaceTemplate);
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
