
using System;

namespace Automatica.Core.EF.Models
{
    public enum SettingReloadContext
    {
        None = -1,
        Server = 0,
        Recorders = 1,
        RemoteConnect = 2
    }

    public partial class Setting
    {
        public long ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public string ValueKey { get; set; }
        public string ValueText { get; set; }
        public int? ValueInt { get; set; }
        public double? ValueDouble { get; set; }

        public string Group { get; set; }

        public long Type { get; set; }
        public bool IsVisible { get; set; }

        public int Order { get; set; }

        public bool IsReadonly { get; set; }

        public string Meta { get; set; }

        public bool NeedsReloadOnChange { get; set; }
        public SettingReloadContext ReloadContext { get; set; }
    }
}
