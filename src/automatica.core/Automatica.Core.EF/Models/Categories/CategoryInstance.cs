using System;
using Automatica.Core.Model;
using Automatica.Core.Model.Models.User;

namespace Automatica.Core.EF.Models.Categories
{
    public partial class CategoryInstance : TypedObject
    {
        public Guid ObjId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public Guid This2CategoryGroup { get; set; }

        public int Rating { get; set; }

        public bool IsFavorite { get; set; }
        public string Icon { get; set; }

        public string Color { get; set; }

        public bool IsDeleteable { get; set; }

        public CategoryGroup This2CategoryGroupNavigation { get; set; }

        public Guid? This2UserGroup { get; set; }
        public UserGroup This2UserGroupNavigation { get; set; }

    }
}
