
using Automatica.Core.Model;

namespace Automatica.Core.EF.Models
{
    public partial class NodeDataType : TypedObject
    {
        public long Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
