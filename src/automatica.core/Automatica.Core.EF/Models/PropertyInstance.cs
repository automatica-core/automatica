using System;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
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
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
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

        [JsonIgnore,]
        public NodeInstance This2NodeInstanceNavigation { get; set; }

        public NodeInstance ValueNodeInstanceNavigation { get; set; }

        public RulePage ValueRulePageNavigation { get; set; }

        public VisuPage ValueVisuPageNavigation { get; set; }

        public AreaInstance ValueAreaInstanceNavigation { get; set; }

        public CategoryInstance ValueCategoryInstanceNavigation { get; set; }

        public Slave ValueSlaveNavigation { get; set; }

        [JsonIgnore]
        public VisuObjectInstance This2VisuObjectInstanceNavigation { get; set; }

        public PropertyTemplate This2PropertyTemplateNavigation { get; set; }
        
    }
}
