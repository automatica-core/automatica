using System;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Base.Templates
{
    /// <summary>
    /// Utility class to create a <see cref="NodeInstance"/> from a <see cref="NodeTemplate"/>
    /// </summary>
    public static class NodeInstanceFactory
    {
        public static NodeInstance CreateNodeInstanceFromTemplate(NodeTemplate template)
        {
            var instance = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = template.Name,
                Description = template.Description,
                This2NodeTemplateNavigation = template,
                This2NodeTemplate = template.ObjId,
                IsWriteable = template.IsWriteable,
                IsReadable = template.IsReadable
            };

            foreach (var prop in template.PropertyTemplate)
            {
                try
                {
                    var propertyInstance = new PropertyInstance
                    {
                        ObjId = Guid.NewGuid(),
                        This2NodeInstanceNavigation = instance,
                        This2PropertyTemplateNavigation = prop,
                        This2PropertyTemplate = prop.ObjId,
                        Value = prop.DefaultValue
                    };
                    instance.PropertyInstance.Add(propertyInstance);
                }
                catch (Exception e)
                {
                    throw new FormatException($"Error creating {nameof(PropertyInstance)} for template {prop.Name} ({prop.DefaultValue})", e);
                }
            }

            return instance;
        }

        public static NodeInstance CreateNodeInstanceFromTemplate(NodeTemplate template, NodeInstance parent)
        {
            var instance = CreateNodeInstanceFromTemplate(template);

            instance.This2ParentNodeInstance = parent.ObjId;
            instance.This2ParentNodeInstanceNavigation = parent;

            return instance;
        }
    }
}
