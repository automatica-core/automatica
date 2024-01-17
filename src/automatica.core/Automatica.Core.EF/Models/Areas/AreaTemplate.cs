using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Areas
{
    public class AreaTemplate : TypedObject
    {
        public Guid ObjId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public Guid This2AreaType { get; set; }

        public Guid ProvidesThis2AreayType { get; set; }
        public Guid NeedsThis2AreaType { get; set; }

        public string Icon { get; set; }

        public bool IsDeleteable { get; set; }


        public AreaType This2AreaTypeNavigation { get; set; }
        public AreaType ProvidesThis2AreayTypeNavigation { get; set; }
        public AreaType NeedsThis2AreaTypeNavigation { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
    }
}
