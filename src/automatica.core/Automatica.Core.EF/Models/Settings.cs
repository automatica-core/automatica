
namespace Automatica.Core.EF.Models
{
    public partial class Setting
    {
        public long ObjId { get; set; }
        public string ValueKey { get; set; }
        public string ValueText { get; set; }
        public int? ValueInt { get; set; }
        public double? ValueDouble { get; set; }

        public string Group { get; set; }

        public long Type { get; set; }
        public bool IsVisible { get; set; }

        public int Order { get; set; }

        public bool IsReadonly { get; set; }
    }
}
