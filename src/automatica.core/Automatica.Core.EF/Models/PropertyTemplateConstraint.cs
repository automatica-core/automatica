﻿using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using MessagePack;
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

        public Guid? Owner { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public long ConstraintType { get; set; }
        public long ConstraintLevel { get; set; }
        public Guid This2PropertyTemplate { get; set; }


        public List<PropertyTemplateConstraintData> ConstraintData { get; set; }

        [JsonIgnore, IgnoreMember]
        public PropertyTemplate This2PropertyTemplateNavigation { get; set; }


    }
}
