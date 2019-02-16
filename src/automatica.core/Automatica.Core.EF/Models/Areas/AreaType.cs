using System;
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models.Areas
{
    public class AreaType : TypedObject
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
