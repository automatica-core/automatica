﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("Automatica.Core.WebApi.Tests")]

namespace Automatica.Core.Internals.Templates
{
    public class PropertyTemplateFactory : SettingsFactory, IPropertyTemplateFactory
    {
        public Guid Owner { get; internal set; }
        public bool AllowOwnerOverride { get; internal set; }
        public ILogger Logger { get; }
        public IFactory Factory { get; private set; }
        private readonly Action<PropertyTemplate, Guid> _propertyExpression;

        public PropertyTemplateFactory(ILogger logger, AutomaticaContext database, IConfiguration config, Action<PropertyTemplate, Guid> propertyExpression) : base(database, config)
        {
            Logger = logger;
            _propertyExpression = propertyExpression;
        }

        public void SetFactory(IFactory factory)
        {
            Factory = factory;
        }
        
        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid objectRef, string group, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order)
        {

            if (Factory == null)
            {
                throw new ArgumentException($"You need to call {nameof(SetFactory)} prior using this service!");
            }

            var retValue = CreateTemplateCode.Updated;
            var propertyTemplate = Db.PropertyTemplates.SingleOrDefault(p => p.ObjId == uid);

            bool isNewObject = false;
            if (propertyTemplate == null)
            {
                isNewObject = true;
                propertyTemplate = new PropertyTemplate {ObjId = uid};
                retValue = CreateTemplateCode.Created;
            }
            if (propertyTemplate.Owner.HasValue && propertyTemplate.Owner != Owner && !AllowOwnerOverride)
            {
                throw new ArgumentException("You are not allowed to modify this template...");
            }

            propertyTemplate.Owner = Owner;

            propertyTemplate.FactoryReference = Factory.FactoryGuid;
            propertyTemplate.Name = name;
            propertyTemplate.Description = description;
            propertyTemplate.Key = key;
            propertyTemplate.This2PropertyType = (long)propertyType;

            _propertyExpression(propertyTemplate, objectRef);

            propertyTemplate.Group = group;
            propertyTemplate.IsVisible = isVisible;
            propertyTemplate.IsReadonly = isReadonly;
            propertyTemplate.Meta = String.IsNullOrEmpty(meta) ? "" : meta;
            propertyTemplate.DefaultValue = defaultValue == null ? "" : defaultValue.ToString();
            propertyTemplate.GroupOrder = groupOrder;
            propertyTemplate.Order = order;

            if (isNewObject && propertyTemplate.This2NodeTemplate.HasValue)
            {
                Db.PropertyTemplates.Add(propertyTemplate);


                var nodeList = Db.NodeInstances.Where(a => a.This2NodeTemplate == objectRef).ToList();

                propertyTemplate.This2PropertyTypeNavigation = Db.PropertyTypes.SingleOrDefault(a =>
                    a.Type == propertyTemplate.This2PropertyType);

                foreach (var node in nodeList)
                {
                    var propertyInstance = new PropertyInstance
                    {
                        ObjId = Guid.NewGuid(),
                        This2NodeInstance = node.ObjId,
                        This2PropertyTemplate = propertyTemplate.ObjId,
                        This2PropertyTemplateNavigation = propertyTemplate,
                        Value = propertyTemplate.DefaultValue,
                        IsDeleted = false
                    };

                    Db.PropertyInstances.Add(propertyInstance);
                }
            }
            else if (isNewObject && propertyTemplate.This2VisuObjectTemplate.HasValue)
            {
                Db.PropertyTemplates.Add(propertyTemplate);
            }
            else
            {
                Db.PropertyTemplates.Update(propertyTemplate);
            }

            
            return retValue;
        }

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string description,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            var retValue = CreateTemplateCode.Updated;
            var constraint = Db.PropertyTemplateConstraints.SingleOrDefault(p => p.ObjId == constraintId);

            bool isNewObject = false;
            if (constraint == null)
            {
                isNewObject = true;
                constraint = new PropertyTemplateConstraint {ObjId = constraintId};
                retValue = CreateTemplateCode.Created;
            }
            if (constraint.Owner.HasValue && constraint.Owner != Owner && !AllowOwnerOverride)
            {
                throw new ArgumentException("You are not allowed to modify this template...");
            }

            constraint.Owner = Owner;

            constraint.Name = name;
            constraint.Description = description;
            constraint.ConstraintType = (long)constraintType;
            constraint.This2PropertyTemplate = propertyTemplate;
            constraint.ConstraintLevel = (long)level;

            if (isNewObject)
            {
                Db.PropertyTemplateConstraints.Add(constraint);
            }
            else
            {
                Db.PropertyTemplateConstraints.Update(constraint);
            }
            return retValue;
        }

        public CreateTemplateCode CreatePropertyConstraintData(Guid constraintData, double factor, double offset,
            Guid propertyTemplateConstraint, string propertyKey, PropertyConstraintConditionType conditionType)
        {
            var retValue = CreateTemplateCode.Updated;
            var constraint = Db.PropertyTemplateConstraintData.SingleOrDefault(p => p.ObjId == constraintData);

            bool isNewObject = false;
            if (constraint == null)
            {
                isNewObject = true;
                constraint = new PropertyTemplateConstraintData();
                constraint.ObjId = constraintData;
                retValue = CreateTemplateCode.Created;
            }
            if (constraint.Owner.HasValue && constraint.Owner != Owner && !AllowOwnerOverride)
            {
                throw new ArgumentException("You are not allowed to modify this template...");
            }

            constraint.Owner = Owner;
            constraint.Factor = factor;
            constraint.Offset = offset;
            constraint.This2PropertyTemplateConstraint = propertyTemplateConstraint;
            constraint.PropertyKey = propertyKey;
            constraint.ConditionType = (long)conditionType;

            if (isNewObject)
            {
                Db.PropertyTemplateConstraintData.Add(constraint);
            }
            else
            {
                Db.PropertyTemplateConstraintData.Update(constraint);
            }
            return retValue;
        }
    }
}
