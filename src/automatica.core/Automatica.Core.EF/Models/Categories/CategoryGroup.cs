using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Categories
{
    public partial class CategoryGroup : TypedObject
    {
        public Guid ObjId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
