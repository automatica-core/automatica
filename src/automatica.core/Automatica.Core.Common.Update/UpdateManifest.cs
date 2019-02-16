using System;

namespace Automatica.Core.Common.Update
{
    public class UpdateManifest
    {
        public DateTime Timestamp { get; set; }
        public Version Version { get; set; }
        public string Rid { get; set; }
        public bool PreRelease { get; set; }
    }
}
