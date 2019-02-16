
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public class VisuPageType : TypedObject
    {
        public long ObjId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Key { get; set; }
    }
}
