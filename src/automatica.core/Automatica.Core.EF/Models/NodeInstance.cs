using System;
using System.Collections.Generic;
using Automatica.Core.EF.Models.Areas;
using Automatica.Core.EF.Models.Categories;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Model.Models.User;
using MessagePack;
using Newtonsoft.Json;


namespace Automatica.Core.EF.Models
{
    public partial class NodeInstance
    {
        public NodeInstance()
        {
            InverseThis2ParentNodeInstanceNavigation = new List<NodeInstance>();
            NodeInstance2RulePage = new List<NodeInstance2RulePage>();
            PropertyInstance = new List<PropertyInstance>();

            IsDeleted = false;

            Description = "";
        }

        [System.ComponentModel.DataAnnotations.Key]
        public Guid ObjId { get; set; }

        public Guid? This2NodeTemplate { get; set; }
        public Guid? This2ParentNodeInstance { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string VisuName { get; set; }
        public bool IsReadable { get; set; }
        public bool IsWriteable { get; set; }
        public bool IsDeleted { get; set; }

        public bool UseInVisu { get; set; }
        public Guid? This2UserGroup { get; set; }
        public bool IsFavorite { get; set; }
        public int Rating { get; set; }

        public UserGroup This2UserGroupNavigation { get; set; }

        public Guid? This2AreaInstance { get; set; }

        public AreaInstance This2AreaInstanceNavigation { get; set; }
        public Guid? This2CategoryInstance { get; set; }
        public CategoryInstance This2CategoryInstanceNavigation { get; set; }

        public NodeTemplate This2NodeTemplateNavigation { get; set; }

        public string StateTextValueTrue { get; set; }
        public string StateTextValueFalse { get; set; }
        public string StateColorValueTrue { get; set; }
        public string StateColorValueFalse { get; set; }
        
        public bool Trending { get; set; }
        public int TrendingInterval { get; set; }
        public TrendingTypes TrendingType { get; set; }
        public bool TrendingToCloud { get; set; }


        [JsonIgnore, IgnoreMember]
        public NodeInstance This2ParentNodeInstanceNavigation { get; set; }
        public List<NodeInstance> InverseThis2ParentNodeInstanceNavigation { get; set; }

        [JsonIgnore, IgnoreMember]
        public List<NodeInstance2RulePage> NodeInstance2RulePage { get; set; }

        public List<PropertyInstance> PropertyInstance { get; set; }

    }
}
