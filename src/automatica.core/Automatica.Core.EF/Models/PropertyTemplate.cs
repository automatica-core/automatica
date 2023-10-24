using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public partial class PropertyTemplate
    {
        public PropertyTemplate()
        {
            Constraints = new List<PropertyTemplateConstraint>();
        }

        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid? Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
        public long This2PropertyType { get; set; }
        public Guid? This2NodeTemplate { get; set; }
        public Guid? This2VisuObjectTemplate { get; set; }
        public string Group { get; set; }
        public bool IsVisible { get; set; }
        public bool IsReadonly { get; set; }
        public string Meta { get; set; }
        public string DefaultValue { get; set; }
        public int GroupOrder { get; set; }
        public int Order { get; set; }

        public Guid FactoryReference { get; set; }

        [JsonIgnore, IgnoreMember]
        public NodeTemplate This2NodeTemplateNavigation { get; set; }

        [JsonIgnore, IgnoreMember]
        public VisuObjectTemplate This2VisuObjectTemplateNavigation { get; set; }
        public PropertyType This2PropertyTypeNavigation { get; set; }

        public List<PropertyTemplateConstraint> Constraints { get; set; }
        
    }
}
