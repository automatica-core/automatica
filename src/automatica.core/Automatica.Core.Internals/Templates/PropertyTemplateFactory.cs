using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Internals.Templates
{
    public class PropertyTemplateFactory : SettingsFactory, IPropertyTemplateFactory
    {
        private readonly Action<PropertyTemplate, Guid> _propertyExpression;

        public PropertyTemplateFactory(AutomaticaContext database, IConfiguration config, Action<PropertyTemplate, Guid> propertyExpression) : base(database, config)
        {
            _propertyExpression = propertyExpression;
        }
        
        public CreateTemplateCode CreatePropertyTemplate(Guid uid, string name, string description, string key,
            PropertyTemplateType propertyType, Guid objectRef, string groupd, bool isVisible, bool isReadonly, string meta,
            object defaultValue, int groupOrder, int order)
        {
            var retValue = CreateTemplateCode.Updated;
            var propertyTemplate = Db.PropertyTemplates.SingleOrDefault(p => p.ObjId == uid);

            bool isNewObject = false;
            if (propertyTemplate == null)
            {
                isNewObject = true;
                propertyTemplate = new PropertyTemplate();
                propertyTemplate.ObjId = uid;
                retValue = CreateTemplateCode.Created;
            }

            propertyTemplate.Name = name;
            propertyTemplate.Description = description;
            propertyTemplate.Key = key;
            propertyTemplate.This2PropertyType = (long)propertyType;

            _propertyExpression(propertyTemplate, objectRef);

            propertyTemplate.Group = groupd;
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
                    var propertyInstance = new PropertyInstance();
                    propertyInstance.ObjId = Guid.NewGuid();
                    propertyInstance.This2NodeInstance = node.ObjId;
                    propertyInstance.This2PropertyTemplate = propertyTemplate.ObjId;
                    propertyInstance.This2PropertyTemplateNavigation = propertyTemplate;
                    propertyInstance.Value = propertyTemplate.DefaultValue;
                    propertyInstance.IsDeleted = false;

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

        public CreateTemplateCode CreatePropertyConstraint(Guid constraintId, string name, string descrption,
            PropertyConstraint constraintType, PropertyConstraintLevel level, Guid propertyTemplate)
        {
            var retValue = CreateTemplateCode.Updated;
            var constraint = Db.PropertyTemplateConstraints.SingleOrDefault(p => p.ObjId == constraintId);

            bool isNewObject = false;
            if (constraint == null)
            {
                isNewObject = true;
                constraint = new PropertyTemplateConstraint();
                constraint.ObjId = constraintId;
                retValue = CreateTemplateCode.Created;
            }

            constraint.Name = name;
            constraint.Description = descrption;
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
