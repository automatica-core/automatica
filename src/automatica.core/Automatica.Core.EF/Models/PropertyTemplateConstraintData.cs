using System;
using Automatica.Core.Model;
using MessagePack;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public class PropertyTemplateConstraintData : TypedObject
    {
        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid? Owner { get; set; }
        public double Factor { get; set; }
        public double Offset { get; set; }
        public string PropertyKey { get; set; }

        public Guid This2PropertyTemplateConstraint { get; set; }

        public long ConditionType { get; set; }



        [JsonIgnore, IgnoreMember]
        public PropertyTemplateConstraint This2PropertyTemplateConstraintNavigation { get; set; }
    }
}
