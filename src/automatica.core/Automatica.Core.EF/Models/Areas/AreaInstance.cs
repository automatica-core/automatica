using System;
using System.Collections.Generic;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;

namespace Automatica.Core.EF.Models.Areas
{
    public class AreaInstance : TypedObject
    {
        public AreaInstance()
        {
            InverseThis2ParentNavigation = new List<AreaInstance>();
        }
        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }

        public Guid This2AreaTemplate { get; set; }

        public Guid? This2Parent { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string Icon { get; set; }

        public AreaInstance This2ParentNavigation { get; set; }

        public AreaTemplate This2AreaTemplateNavigation { get; set; }

        public Guid? This2UserGroup { get; set; }
        public bool IsFavorite { get; set; }
        public int Rating { get; set; }

        public UserGroup This2UserGroupNavigation { get; set; }


        public List<AreaInstance> InverseThis2ParentNavigation { get; set; }
    }
}
