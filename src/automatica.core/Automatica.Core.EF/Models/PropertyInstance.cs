using System;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using MessagePack;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public partial class PropertyInstance
    {
        public PropertyInstance()
        {
            IsDeleted = false;
        }

        [System.ComponentModel.DataAnnotations.Key]
        public Guid ObjId { get; set; }
        public Guid This2PropertyTemplate { get; set; }
        public Guid? This2NodeInstance { get; set; }
        public Guid? This2VisuObjectInstance { get; set; }
        public string ValueString { get; set; }
        public int? ValueInt { get; set; }
        public bool? ValueBool { get; set; }
        public double? ValueDouble { get; set; }
        public long? ValueLong { get; set; }


        public Guid? ValueNodeInstance { get; set; }
        public Guid? ValueRulePage { get; set; }
        public Guid? ValueVisuPage { get; set; }
        public Guid? ValueAreaInstance { get; set; }
        public Guid? ValueCategoryInstance { get; set; }
        public Guid? ValueSlave { get; set; }

        public bool IsDeleted { get; set; }

        [JsonIgnore, IgnoreMember]
        public NodeInstance This2NodeInstanceNavigation { get; set; }

        [IgnoreMember]
        public NodeInstance ValueNodeInstanceNavigation { get; set; }

        [IgnoreMember]
        public RulePage ValueRulePageNavigation { get; set; }

        [IgnoreMember]
        public VisuPage ValueVisuPageNavigation { get; set; }

        public AreaInstance ValueAreaInstanceNavigation { get; set; }

        public CategoryInstance ValueCategoryInstanceNavigation { get; set; }

        public Slave ValueSlaveNavigation { get; set; }

        [JsonIgnore, IgnoreMember]
        public VisuObjectInstance This2VisuObjectInstanceNavigation { get; set; }

        public PropertyTemplate This2PropertyTemplateNavigation { get; set; }
        
    }
}
