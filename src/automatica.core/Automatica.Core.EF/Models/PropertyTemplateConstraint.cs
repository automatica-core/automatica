using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public class PropertyTemplateConstraint : TypedObject
    {
        public PropertyTemplateConstraint()
        {
            ConstraintData = new List<PropertyTemplateConstraintData>();
        }
        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }

        public Guid? Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public long ConstraintType { get; set; }
        public long ConstraintLevel { get; set; }
        public Guid This2PropertyTemplate { get; set; }


        public List<PropertyTemplateConstraintData> ConstraintData { get; set; }

        [JsonIgnore]
        public PropertyTemplate This2PropertyTemplateNavigation { get; set; }


    }
}
